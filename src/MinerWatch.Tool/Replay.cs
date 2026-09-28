using System.Text.Json;
using MinerWatch.Core.State;

namespace MinerWatch.Tool;

// Replays labelled measurements, not image recognition. Input times are monotonic milliseconds.
public static class Replay
{
    public static int Run(TextReader input, TextWriter output, StatePolicy policy)
    {
        var engines = new Dictionary<string, ClientStateEngine>(StringComparer.Ordinal);
        var latest = new Dictionary<string, ClientMeasurements?>(StringComparer.Ordinal);
        int lineNumber = 0;
        string? line;
        while ((line = input.ReadLine()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var e = JsonSerializer.Deserialize<ReplayEvent>(line, ProfileFile.JsonOptions) ?? throw new InvalidDataException("Null event.");
                if (string.IsNullOrWhiteSpace(e.ClientId) || e.Generation <= 0 || e.NowMs < 0)
                    throw new InvalidDataException("Invalid client, generation or clock.");
                if (!engines.TryGetValue(e.ClientId, out var engine))
                {
                    engines[e.ClientId] = engine = new ClientStateEngine(policy);
                    latest[e.ClientId] = null;
                }
                ClientStatus status;
                if (e.Kind == "failure")
                {
                    latest[e.ClientId] = null;
                    status = engine.Evaluate(e.Generation, null, TimeSpan.FromMilliseconds(e.NowMs));
                    if (status.Reason == "MissingMeasurements") status = engine.Invalidate(e.Reason ?? "CaptureFailed");
                }
                else
                {
                    if (e.Kind == "sample")
                    {
                        if (e.Sequence == null || e.CapturedMs == null || e.Hold == null || e.H1 == null || e.H2 == null || e.Confidence == null)
                            throw new InvalidDataException("Incomplete sample.");
                        var stamp = new FrameStamp(e.Generation, e.Sequence.Value, TimeSpan.FromMilliseconds(e.CapturedMs.Value));
                        latest[e.ClientId] = new ClientMeasurements(
                            new Measurement<double>(e.Hold.Value, e.Confidence.Value, e.Valid, stamp, e.Reason),
                            new Measurement<HarvesterState>(e.H1.Value, e.Confidence.Value, e.Valid, stamp, e.Reason),
                            new Measurement<HarvesterState>(e.H2.Value, e.Confidence.Value, e.Valid, stamp, e.Reason));
                    }
                    else if (e.Kind != "tick") throw new InvalidDataException("Unknown event kind: " + e.Kind);
                    status = engine.Evaluate(e.Generation, latest[e.ClientId], TimeSpan.FromMilliseconds(e.NowMs));
                }
                output.WriteLine(JsonSerializer.Serialize(new
                {
                    e.ClientId, e.NowMs, state = status.State.ToString(),
                    pending = status.Pending?.ToString(), status.Reason, status.ConfirmationCount
                }, ProfileFile.JsonOptions));
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidDataException or OverflowException)
            {
                throw new InvalidDataException($"Replay line {lineNumber}: {ex.Message}", ex);
            }
        }
        return 0;
    }

    private sealed class ReplayEvent
    {
        public required string Kind { get; init; }
        public required string ClientId { get; init; }
        public required long Generation { get; init; }
        public required long NowMs { get; init; }
        public long? Sequence { get; init; }
        public long? CapturedMs { get; init; }
        public double? Hold { get; init; }
        public HarvesterState? H1 { get; init; }
        public HarvesterState? H2 { get; init; }
        public double? Confidence { get; init; }
        public bool Valid { get; init; } = true;
        public string? Reason { get; init; }
    }
}
