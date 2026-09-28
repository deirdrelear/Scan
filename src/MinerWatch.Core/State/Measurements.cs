using System;

namespace MinerWatch.Core.State;

public enum HarvesterState { Unknown, Active, Inactive }
public enum OperatorState { Unknown, Green, Yellow, Red }

public readonly struct FrameStamp
{
    public long Generation { get; }
    public long Sequence { get; }
    public TimeSpan CapturedAt { get; }
    public FrameStamp(long generation, long sequence, TimeSpan capturedAt)
    { Generation = generation; Sequence = sequence; CapturedAt = capturedAt; }
}

public sealed class Measurement<T>
{
    public T Value { get; }
    public double Confidence { get; }
    public bool IsValid { get; }
    public FrameStamp Stamp { get; }
    public string? Reason { get; }
    public Measurement(T value, double confidence, bool isValid, FrameStamp stamp, string? reason = null)
    { Value = value; Confidence = confidence; IsValid = isValid; Stamp = stamp; Reason = reason; }
}

public sealed class ClientMeasurements
{
    public Measurement<double>? Hold { get; }
    public Measurement<HarvesterState>? Harvester1 { get; }
    public Measurement<HarvesterState>? Harvester2 { get; }
    public ClientMeasurements(Measurement<double>? hold, Measurement<HarvesterState>? harvester1,
        Measurement<HarvesterState>? harvester2)
    { Hold = hold; Harvester1 = harvester1; Harvester2 = harvester2; }
}

public sealed class StatePolicy
{
    public double WarningEnter { get; }
    public double WarningExit { get; }
    public double FullEnter { get; }
    public double FullExit { get; }
    public double MinimumConfidence { get; }
    public TimeSpan Freshness { get; }
    public TimeSpan MaximumSkew { get; }
    public int ConfirmationSamples { get; }

    public StatePolicy(double warningEnter = .90, double warningExit = .87,
        double fullEnter = .99, double fullExit = .97, double minimumConfidence = .80,
        int freshnessMs = 2000, int maximumSkewMs = 1000, int confirmationSamples = 2)
    {
        if (!Unit(warningEnter) || !Unit(warningExit) || !Unit(fullEnter) || !Unit(fullExit) || !Unit(minimumConfidence) ||
            warningExit >= warningEnter || warningEnter >= fullExit || fullExit >= fullEnter || minimumConfidence <= 0 ||
            freshnessMs <= 0 || maximumSkewMs < 0 || maximumSkewMs > freshnessMs || confirmationSamples < 1)
            throw new ArgumentException("Invalid thresholds, freshness, confidence or confirmation policy.");
        WarningEnter = warningEnter; WarningExit = warningExit; FullEnter = fullEnter; FullExit = fullExit;
        MinimumConfidence = minimumConfidence; Freshness = TimeSpan.FromMilliseconds(freshnessMs);
        MaximumSkew = TimeSpan.FromMilliseconds(maximumSkewMs); ConfirmationSamples = confirmationSamples;
    }

    internal static bool Unit(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 1;
}

public sealed class ClientStatus
{
    public OperatorState State { get; }
    public OperatorState? Pending { get; }
    public string Reason { get; }
    public int ConfirmationCount { get; }
    internal ClientStatus(OperatorState state, OperatorState? pending, string reason, int confirmationCount)
    { State = state; Pending = pending; Reason = reason; ConfirmationCount = confirmationCount; }
}
