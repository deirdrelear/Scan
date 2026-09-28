using System.Text.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using MinerWatch.Core.Capture;
using MinerWatch.Core.Profiles;
using MinerWatch.Core.State;
using MinerWatch.Core.Windows;
using MinerWatch.Tool;

var tests = new List<(string Name, Action Body)>();
void Test(string name, Action body) => tests.Add((name, body));
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
}
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}
TimeSpan Ms(long value) => TimeSpan.FromMilliseconds(value);
ClientMeasurements Sample(long seq, long ms, double fill = .5, HarvesterState h1 = HarvesterState.Active,
    HarvesterState h2 = HarvesterState.Active, long gen = 1, double confidence = .99, bool valid = true)
{
    var stamp = new FrameStamp(gen, seq, Ms(ms));
    return new(new(fill, confidence, valid, stamp), new(h1, confidence, valid, stamp), new(h2, confidence, valid, stamp));
}
ClientStateEngine Engine(int confirmations = 2) => new(new StatePolicy(confirmationSamples: confirmations));
ClientStateEngine Green()
{
    var e = Engine();
    Equal(OperatorState.Unknown, e.Evaluate(1, Sample(1, 0), Ms(0)).State);
    Equal(OperatorState.Green, e.Evaluate(1, Sample(2, 500), Ms(500)).State);
    return e;
}
WindowObservation Window(string name = "A", long hwnd = 1, int pid = 1, long start = 1, int width = 100, int height = 100,
    uint dpi = 96, bool minimized = false, bool visible = true, bool cloaked = false, string process = "exefile") =>
    new(hwnd, pid, start, process, "EVE - " + name, "triuiScreen", width, height, dpi, visible, minimized, cloaked);
EveWindowRegistry Registry() => new(new[] { new ClientRegistration("miner-a", "A") });
MiningProfile Profile(RoiAnchor anchor = RoiAnchor.TopLeft, int x = 0, int y = 0, bool calibrated = true) =>
    new(1, "test", 100, 100, "100", 96, calibrated, new[] {
        new RoiDefinition("hold", anchor, x, y, 10, 10, "HoldGauge"),
        new RoiDefinition("harvester1", RoiAnchor.TopLeft, 0, 0, 10, 10, "Harvester"),
        new RoiDefinition("harvester2", RoiAnchor.TopLeft, 20, 0, 10, 10, "Harvester") }, new());

foreach (var fill in new[] { 0d, .899, .90, .989, .99, 1d })
foreach (var h1 in new[] { HarvesterState.Active, HarvesterState.Inactive })
foreach (var h2 in new[] { HarvesterState.Active, HarvesterState.Inactive })
{
    var expected = fill >= .99 || h1 == HarvesterState.Inactive && h2 == HarvesterState.Inactive ? OperatorState.Red :
        fill >= .9 || h1 == HarvesterState.Inactive || h2 == HarvesterState.Inactive ? OperatorState.Yellow : OperatorState.Green;
    Test($"baseline {fill} {h1} {h2}", () => Equal(expected, Engine(1).Evaluate(1, Sample(1, 0, fill, h1, h2), Ms(0)).State));
}
Test("cached frame cannot confirm or refresh", () => {
    var e = Engine(); var s = Sample(1, 0);
    e.Evaluate(1, s, Ms(0));
    for (int i = 1; i < 2000; i += 17) Equal(OperatorState.Unknown, e.Evaluate(1, s, Ms(i)).State);
    Equal("StaleMeasurements", e.Evaluate(1, s, Ms(2000)).Reason);
});
Test("polling a stable cached frame still expires at deadline", () => {
    var e = Green();
    Equal(OperatorState.Green, e.Evaluate(1, Sample(2, 500), Ms(2499)).State);
    Equal(OperatorState.Unknown, e.Evaluate(1, Sample(2, 500), Ms(2500)).State);
});
Test("single inactive frame does not change confirmed colour", () => {
    var e = Green();
    Equal(OperatorState.Green, e.Evaluate(1, Sample(3, 1000, h1: HarvesterState.Inactive), Ms(1000)).State);
    Equal(OperatorState.Green, e.Evaluate(1, Sample(4, 1500), Ms(1500)).State);
});
Test("single threshold spike cannot latch warning", () => {
    var e = Green();
    e.Evaluate(1, Sample(3, 1000, .91), Ms(1000));
    Equal(OperatorState.Green, e.Evaluate(1, Sample(4, 1500, .89), Ms(1500)).State);
    Equal(OperatorState.Green, e.Evaluate(1, Sample(5, 2000, .89), Ms(2000)).State);
});
Test("warning hysteresis and recovery", () => {
    var e = Engine();
    e.Evaluate(1, Sample(1, 0, .92), Ms(0));
    Equal(OperatorState.Yellow, e.Evaluate(1, Sample(2, 500, .93), Ms(500)).State);
    Equal(OperatorState.Yellow, e.Evaluate(1, Sample(3, 1000, .89), Ms(1000)).State);
    Equal(OperatorState.Yellow, e.Evaluate(1, Sample(4, 1500, .87), Ms(1500)).State);
    Equal(OperatorState.Green, e.Evaluate(1, Sample(5, 2000, .86), Ms(2000)).State);
});
Test("hold band confirms while same colour caused by inactive module", () => {
    var e = Engine();
    e.Evaluate(1, Sample(1, 0, .5, HarvesterState.Inactive), Ms(0));
    e.Evaluate(1, Sample(2, 500, .5, HarvesterState.Inactive), Ms(500));
    e.Evaluate(1, Sample(3, 1000, .92, HarvesterState.Inactive), Ms(1000));
    e.Evaluate(1, Sample(4, 1500, .93, HarvesterState.Inactive), Ms(1500));
    Equal(OperatorState.Yellow, e.Evaluate(1, Sample(5, 2000, .89), Ms(2000)).State);
    Equal(OperatorState.Yellow, e.Evaluate(1, Sample(6, 2500, .89), Ms(2500)).State);
});
Test("full threshold has independent exit band", () => {
    var e = Engine(1);
    Equal(OperatorState.Red, e.Evaluate(1, Sample(1, 0, 1), Ms(0)).State);
    Equal(OperatorState.Red, e.Evaluate(1, Sample(2, 500, .98), Ms(500)).State);
    Equal(OperatorState.Yellow, e.Evaluate(1, Sample(3, 1000, .97), Ms(1000)).State);
    Equal(OperatorState.Green, e.Evaluate(1, Sample(4, 1500, .87), Ms(1500)).State);
});
Test("invalid source becomes UNKNOWN immediately and needs new confirmations", () => {
    var e = Green();
    Equal(OperatorState.Unknown, e.Invalidate("CaptureFailed").State);
    Equal(OperatorState.Unknown, e.Evaluate(1, Sample(2, 500), Ms(600)).State);
    Equal(OperatorState.Unknown, e.Evaluate(1, Sample(3, 1000), Ms(1000)).State);
    Equal(OperatorState.Green, e.Evaluate(1, Sample(4, 1500), Ms(1500)).State);
});
foreach (var confidence in new[] { .79, -1, 1.01, double.NaN, double.PositiveInfinity })
    Test("invalid confidence " + confidence, () => Equal(OperatorState.Unknown, Green().Evaluate(1, Sample(3, 1000, confidence: confidence), Ms(1000)).State));
foreach (var ratio in new[] { -.01, 1.01, double.NaN, double.NegativeInfinity })
    Test("invalid hold " + ratio, () => Equal(OperatorState.Unknown, Engine(1).Evaluate(1, Sample(1, 0, ratio), Ms(0)).State));
Test("ambiguous harvester outranks full hold", () => Equal(OperatorState.Unknown,
    Engine(1).Evaluate(1, Sample(1, 0, 1, HarvesterState.Unknown), Ms(0)).State));
Test("invalid enum cannot mean active", () => Equal(OperatorState.Unknown,
    Engine(1).Evaluate(1, Sample(1, 0, h1: (HarvesterState)99), Ms(0)).State));
Test("old binding cannot restore old green", () => {
    var e = Green();
    Equal("OldBinding", e.Evaluate(2, Sample(3, 1000), Ms(1000)).Reason);
    Equal(OperatorState.Unknown, e.Evaluate(2, Sample(1, 1500, gen: 2), Ms(1500)).State);
    Equal(OperatorState.Green, e.Evaluate(2, Sample(2, 2000, gen: 2), Ms(2000)).State);
});
Test("generation rollback is rejected", () => {
    var e = Engine(1); e.Evaluate(2, Sample(1, 1000, gen: 2), Ms(1000));
    Equal("InvalidGeneration", e.Evaluate(1, Sample(2, 1500), Ms(1500)).Reason);
});
Test("fresh sequence needs advancing capture timestamp", () => {
    var e = Green();
    Equal("OutOfOrderFrame", e.Evaluate(1, Sample(3, 500), Ms(1000)).Reason);
});
Test("cached frame cannot get a new timestamp", () => {
    var e = Green();
    Equal("OutOfOrderFrame", e.Evaluate(1, Sample(2, 1000), Ms(1000)).Reason);
});
Test("older source frame is rejected", () => Equal("OutOfOrderFrame", Green().Evaluate(1, Sample(1, 0), Ms(1000)).Reason));
Test("clock rollback and future timestamps rejected", () => {
    Equal("ClockMovedBackwards", Green().Evaluate(1, Sample(2, 500), Ms(400)).Reason);
    Equal("InvalidTimestamp", Engine().Evaluate(1, Sample(1, 1000), Ms(999)).Reason);
});
Test("missing and invalid samples fail closed", () => {
    Equal(OperatorState.Unknown, Green().Evaluate(1, null, Ms(1000)).State);
    Equal(OperatorState.Unknown, Green().Evaluate(1, Sample(3, 1000, valid: false), Ms(1000)).State);
});
Test("harvester-only advances cannot confirm hold warning", () => {
    var e = Engine(); var one = Sample(1, 0, .95); var two = Sample(2, 500, .95);
    e.Evaluate(1, one, Ms(0));
    Equal(OperatorState.Unknown, e.Evaluate(1, new(one.Hold, two.Harvester1, two.Harvester2), Ms(500)).State);
    Equal(OperatorState.Yellow, e.Evaluate(1, two, Ms(500)).State);
});
Test("excessive cross-detector age skew rejected", () => {
    var old = Sample(1, 0); var fresh = Sample(2, 1500);
    Equal("MeasurementSkew", Engine(1).Evaluate(1, new(old.Hold, fresh.Harvester1, fresh.Harvester2), Ms(1500)).Reason);
});
Test("unsafe policies rejected", () => {
    Throws<ArgumentException>(() => new StatePolicy(warningEnter: double.NaN));
    Throws<ArgumentException>(() => new StatePolicy(fullExit: .8));
    Throws<ArgumentException>(() => new StatePolicy(minimumConfidence: 0));
    Throws<ArgumentException>(() => new StatePolicy(confirmationSamples: 0));
    Throws<ArgumentException>(() => new StatePolicy(maximumSkewMs: 2001));
});

Test("20 clients preserve identity when enumeration order changes", () => {
    var roster = Enumerable.Range(1, 20).Select(i => new ClientRegistration("id-" + i, "Pilot" + i)).ToArray();
    var registry = new EveWindowRegistry(roster);
    var windows = Enumerable.Range(1, 20).Select(i => Window("Pilot" + i, i, i)).ToArray();
    var first = registry.Refresh(windows); var second = registry.Refresh(windows.Reverse());
    Equal(20, second.Count);
    for (int i = 0; i < 20; i++) {
        Equal(first[i].Client.ClientId, second[i].Client.ClientId);
        Equal(first[i].Generation, second[i].Generation);
        Equal(BindingStatus.Ready, second[i].Status);
    }
});
Test("HWND restart and PID reuse reset generation", () => {
    var r = Registry(); var a = r.Refresh(new[] { Window() })[0];
    var b = r.Refresh(new[] { Window(hwnd: 2, pid: 2) })[0];
    Equal(a.Generation + 1, b.Generation);
    Equal(a.Client.ClientId, b.Client.ClientId);
    Equal(b.Generation + 1, r.Refresh(new[] { Window(hwnd: 2, pid: 2, start: 2) })[0].Generation);
});
Test("missing clients retained and rebound", () => {
    var r = Registry(); r.Refresh(new[] { Window() });
    var missing = r.Refresh(Array.Empty<WindowObservation>())[0];
    Equal(BindingStatus.Missing, missing.Status);
    Equal(missing.Generation, r.Refresh(Array.Empty<WindowObservation>())[0].Generation);
    Equal(missing.Generation + 1, r.Refresh(new[] { Window() })[0].Generation);
});
Test("duplicate character titles never pick first window", () => {
    var r = Registry();
    var binding = r.Refresh(new[] { Window(), Window(hwnd: 2) })[0];
    Equal(BindingStatus.Ambiguous, binding.Status); Equal<WindowObservation?>(null, binding.Window);
});
Test("login and lookalike processes are not character bindings", () => {
    Equal(BindingStatus.Missing, Registry().Refresh(new[] { Window(process: "fake-exefile") })[0].Status);
    Equal<string?>(null, new WindowObservation(1, 1, 1, "exefile", "EVE", "class", 100, 100, 96).CharacterName);
});
foreach (var changed in new[] { Window(width: 101), Window(height: 101), Window(dpi: 144), Window(minimized: true), Window(visible: false), Window(cloaked: true) })
    Test($"source changes invalidate generation {changed.ClientWidth}/{changed.ClientHeight}/{changed.Dpi}/{changed.IsMinimized}/{changed.IsVisible}/{changed.IsCloaked}", () => {
        var r = Registry(); r.Refresh(new[] { Window() });
        Equal(2L, r.Refresh(new[] { changed })[0].Generation);
    });
Test("unavailable windows rejected", () => {
    foreach (var w in new[] { Window(minimized: true), Window(visible: false), Window(cloaked: true), Window(hwnd: 0), Window(start: 0), Window(dpi: 0) })
        Equal(BindingStatus.Unavailable, Registry().Refresh(new[] { w })[0].Status);
});
Test("roster rejects ambiguous identities", () => {
    Throws<ArgumentException>(() => new EveWindowRegistry(new[] { new ClientRegistration("1", "A"), new ClientRegistration("2", "a") }));
    Throws<ArgumentException>(() => new EveWindowRegistry(new[] { new ClientRegistration("1", "A"), new ClientRegistration("1", "B") }));
});
foreach (var (anchor, x, y, expectedX, expectedY) in new[] {
    (RoiAnchor.TopLeft, 1, 2, 1, 2), (RoiAnchor.TopRight, -10, 2, 90, 2),
    (RoiAnchor.BottomLeft, 1, -10, 1, 90), (RoiAnchor.BottomRight, -10, -10, 90, 90),
    (RoiAnchor.BottomCenter, -5, -10, 45, 90), (RoiAnchor.Center, -5, -5, 45, 45) })
    Test("anchor " + anchor, () => {
        var resolved = ProfileResolver.Resolve(Profile(anchor, x, y), 100, 100, 96, "100");
        Equal(true, resolved.IsValid);
        Equal(expectedX, resolved.Rois[0].Bounds.X); Equal(expectedY, resolved.Rois[0].Bounds.Y);
    });
Test("one shared profile produces identical ROIs for 20 clients", () => {
    var p = Profile(RoiAnchor.BottomRight, -10, -10);
    for (int i = 0; i < 20; i++) Equal(90, ProfileResolver.Resolve(p, 100, 100, 96, "100").Rois[0].Bounds.X);
});
Test("geometry DPI and manual UI scale must match", () => {
    foreach (var r in new[] { ProfileResolver.Resolve(Profile(), 101, 100, 96, "100"), ProfileResolver.Resolve(Profile(), 100, 101, 96, "100"),
        ProfileResolver.Resolve(Profile(), 100, 100, 144, "100"), ProfileResolver.Resolve(Profile(), 100, 100, 96, "125"),
        ProfileResolver.Resolve(Profile(), 100, 100, 96, null) }) Equal("ProfileMismatch", r.Reason);
});
Test("uncalibrated profile and outside ROI fail closed", () => {
    Equal("ProfileNotCalibrated", ProfileResolver.Resolve(Profile(calibrated: false), 100, 100, 96, "100").Reason);
    foreach (var x in new[] { -1, 91, int.MaxValue, int.MinValue }) {
        var result = ProfileResolver.Resolve(Profile(x: x), 100, 100, 96, "100");
        Equal(false, result.IsValid); Equal(0, result.Rois.Count);
    }
});
Test("ROI buffer is owned and respects padding", () => {
    var bytes = new byte[] { 1, 2, 3, 4, 0, 0, 0, 0, 5, 6, 7, 8, 0, 0, 0, 0 };
    var frame = new RoiFrame("hold", 1, 2, 8, bytes, new(1, 1, Ms(0)));
    bytes[0] = 99;
    Equal((byte)1, frame.Channel(0, 0, 0)); Equal((byte)7, frame.Channel(0, 1, 2));
    Throws<ArgumentOutOfRangeException>(() => frame.Channel(1, 0, 0));
    Throws<ArgumentException>(() => new RoiFrame("hold", int.MaxValue, 2, 8, bytes, default));
});
Test("empty or duplicate ROI batch cannot be successful", () => {
    Throws<ArgumentException>(() => CaptureBatch.Success(Array.Empty<RoiFrame>()));
    var f = new RoiFrame("hold", 1, 1, 4, new byte[4], new(1, 1, Ms(0)));
    Throws<ArgumentException>(() => CaptureBatch.Success(new[] { f, f }));
    Equal(false, CaptureBatch.Failure("black frame").IsValid);
});

var root = args.Length == 1 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
var profilePath = Path.Combine(root, "profiles/mining.example.json");
Test("example profile loads but cannot enable capture", () => {
    var profile = ProfileFile.Load(profilePath);
    Equal(false, profile.Calibrated);
    Equal(3, profile.Rois.Count);
});
Test("malformed profile fields and unknown schema rejected", () => {
    var json = File.ReadAllText(profilePath);
    foreach (var changed in new[] { json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"),
        json.Replace("\"calibrated\": false,", ""), json.Replace("\"width\": 2560", "\"widthTypo\": 2560"),
        json.Replace("\"anchor\": \"BottomRight\"", "\"anchor\": 3"), json.Replace("\"warningEnter\": 0.90", "\"warningEnter\": 2.0") }) {
        var path = Path.GetTempFileName();
        try {
            File.WriteAllText(path, changed);
            bool rejected = false;
            try { ProfileFile.Load(path); } catch (Exception ex) when (ex is JsonException or ArgumentException) { rejected = true; }
            Equal(true, rejected);
        } finally { File.Delete(path); }
    }
});
Test("replay covers recovery, isolation and expiry", () => {
    using var input = File.OpenText(Path.Combine(root, "tests/fixtures/measurements/mining-session.jsonl"));
    using var output = new StringWriter();
    Replay.Run(input, output, ProfileFile.Load(profilePath).Policy);
    var states = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("state").GetString()).ToArray();
    Equal("Unknown,Unknown,Green,Green,Yellow,Unknown,Yellow,Green,Red,Unknown,Unknown,Green,Unknown", string.Join(",", states));
});
Test("20 independent state engines isolate a client failure", () => {
    var engines = Enumerable.Range(0, 20).Select(_ => Green()).ToArray();
    engines[7].Invalidate("SourceClosed");
    for (int i = 0; i < 20; i++) Equal(i == 7 ? OperatorState.Unknown : OperatorState.Green,
        engines[i].Evaluate(1, Sample(2, 500), Ms(1000)).State);
});
Test("pixel detectors have no native handle or window argument", () => {
    var method = typeof(IRoiDetector<>).GetMethod("Analyze")!;
    Equal(typeof(RoiFrame), method.GetParameters()[0].ParameterType);
    Equal(false, typeof(RoiFrame).GetProperties().Any(x => x.PropertyType == typeof(IntPtr) || x.PropertyType == typeof(WindowObservation)));
});
Test("core has no native imports and adapter matches documented metadata API inventory", () => {
    var coreImports = typeof(ClientStateEngine).Assembly.GetTypes()
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null).ToArray();
    Equal(0, coreImports.Length);
    var native = typeof(ProfileFile).Assembly.GetTypes()
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        .Select(m => (Method: m, Import: m.GetCustomAttribute<DllImportAttribute>()))
        .Where(x => x.Import != null).ToArray();
    var expected = new[] { "EnumWindows", "GetWindowThreadProcessId", "GetWindowTextW", "GetClassNameW", "GetClientRect",
        "IsWindowVisible", "IsIconic", "GetDpiForWindow", "SetThreadDpiAwarenessContext", "DwmGetWindowAttribute" };
    Equal(string.Join(",", expected.OrderBy(x => x, StringComparer.Ordinal)),
        string.Join(",", native.Select(x => x.Method.Name).OrderBy(x => x, StringComparer.Ordinal)));
    foreach (var import in native) Equal(import.Method.Name == "DwmGetWindowAttribute" ? "dwmapi.dll" : "user32.dll", import.Import!.Value);
});

int failures = 0;
foreach (var (name, body) in tests)
{
    try { body(); Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures++; Console.Error.WriteLine("FAIL " + name + ": " + ex.Message); }
}
Console.WriteLine($"{tests.Count - failures}/{tests.Count} checks passed");
return failures == 0 ? 0 : 1;
