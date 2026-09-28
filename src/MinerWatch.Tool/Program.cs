using System.Text.Json;
using MinerWatch.Core.Profiles;
using MinerWatch.Tool;

try
{
    if (args.Length == 3 && args[0] == "replay")
    {
        var profile = ProfileFile.Load(args[1]);
        using var input = File.OpenText(args[2]);
        return Replay.Run(input, Console.Out, profile.Policy);
    }
    if (args.Length == 2 && args[0] == "profile")
    {
        var profile = ProfileFile.Load(args[1]);
        var resolved = ProfileResolver.Resolve(profile, profile.ReferenceWidth, profile.ReferenceHeight, profile.ReferenceDpi, profile.UiScale);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            profile.Id, definitionValid = true, captureReady = resolved.IsValid, resolved.Reason,
            rois = resolved.Rois.Select(x => new { x.Definition.Id, x.Bounds })
        }, ProfileFile.JsonOptions));
        return resolved.IsValid ? 0 : 2;
    }
    if (args.Length == 1 && args[0] == "windows")
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 15063))
            throw new PlatformNotSupportedException("Window discovery requires Windows 10 1703 or later.");
        Console.WriteLine(JsonSerializer.Serialize(new Win32WindowEnumerator().Enumerate(), ProfileFile.JsonOptions));
        return 0;
    }
    Console.Error.WriteLine("MinerWatch development tools (no live capture/CV yet)\n" +
        "  windows                         List EVE window metadata on Windows\n" +
        "  profile <profile.json>          Validate a shared profile (exit 2 if not capture-ready)\n" +
        "  replay <profile.json> <jsonl>    Replay measurements through the state engine");
    return 1;
}
catch (Exception ex) when (ex is IOException or ArgumentException or JsonException or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
