using System;
using System.Linq;

namespace MinerWatch.Core.State;

// One instance per logical client; call from one worker. All times share one monotonic clock.
public sealed class ClientStateEngine
{
    private readonly StatePolicy policy;
    private long generation;
    private TimeSpan lastNow;
    private FrameStamp[]? observed;
    private FrameStamp[]? counted;
    private OperatorState stable = OperatorState.Unknown;
    private OperatorState? pending;
    private int confirmations;
    private bool warning;
    private bool full;
    private bool pendingWarning;
    private bool pendingFull;

    public ClientStateEngine(StatePolicy policy) => this.policy = policy ?? throw new ArgumentNullException(nameof(policy));

    public ClientStatus Evaluate(long bindingGeneration, ClientMeasurements? measurements, TimeSpan now)
    {
        if (bindingGeneration <= 0 || bindingGeneration < generation) return Unknown("InvalidGeneration");
        if (now < TimeSpan.Zero || now < lastNow) return Unknown("ClockMovedBackwards");
        lastNow = now;
        if (bindingGeneration != generation)
        {
            generation = bindingGeneration;
            observed = null;
            counted = null;
            Unknown("BindingChanged");
        }
        if (measurements?.Hold == null || measurements.Harvester1 == null || measurements.Harvester2 == null)
            return Unknown("MissingMeasurements");
        var hold = measurements.Hold;
        var h1 = measurements.Harvester1;
        var h2 = measurements.Harvester2;
        var error = Validate(hold, now) ?? Validate(h1, now) ?? Validate(h2, now);
        if (error != null) return Unknown(error);
        if (!StatePolicy.Unit(hold.Value)) return Unknown("InvalidHoldRatio");
        if (!Known(h1.Value) || !Known(h2.Value)) return Unknown("HarvesterUnknown");
        var stamps = new[] { hold.Stamp, h1.Stamp, h2.Stamp };
        if (stamps.Max(x => x.CapturedAt) - stamps.Min(x => x.CapturedAt) > policy.MaximumSkew)
            return Unknown("MeasurementSkew");
        if (observed != null)
        {
            for (int i = 0; i < stamps.Length; i++)
            {
                if (stamps[i].Sequence < observed[i].Sequence ||
                    (stamps[i].Sequence == observed[i].Sequence && stamps[i].CapturedAt != observed[i].CapturedAt) ||
                    (stamps[i].Sequence > observed[i].Sequence && stamps[i].CapturedAt <= observed[i].CapturedAt))
                    return Unknown("OutOfOrderFrame");
            }
        }
        observed = stamps;
        var nextFull = full ? hold.Value > policy.FullExit : hold.Value >= policy.FullEnter;
        var nextWarning = warning ? hold.Value > policy.WarningExit : hold.Value >= policy.WarningEnter;
        var off = (h1.Value == HarvesterState.Inactive ? 1 : 0) + (h2.Value == HarvesterState.Inactive ? 1 : 0);
        var desired = nextFull || off == 2 ? OperatorState.Red : nextWarning || off == 1 ? OperatorState.Yellow : OperatorState.Green;
        var reason = nextFull ? "HoldFull" : off == 2 ? "BothHarvestersInactive" : off == 1 ? "OneHarvesterInactive" :
            nextWarning ? "HoldWarning" : "Mining";

        if (desired == stable && nextFull == full && nextWarning == warning)
        {
            pending = null;
            confirmations = 0;
            counted = stamps;
            return new ClientStatus(stable, null, reason, 0);
        }
        // Confirm the hold band even if another condition already produces the same operator colour.
        if (pending != desired || pendingFull != nextFull || pendingWarning != nextWarning)
        {
            pending = desired; pendingFull = nextFull; pendingWarning = nextWarning; confirmations = 0;
        }
        // All required streams must advance before another confirmation counts.
        // Polling, replaying a cached frame or faster harvester-only updates cannot confirm an old hold sample.
        if (counted == null || stamps.Where((s, i) => s.Sequence <= counted[i].Sequence).Any() == false)
        {
            confirmations++;
            counted = stamps;
        }
        if (confirmations >= policy.ConfirmationSamples)
        {
            full = nextFull;
            warning = nextWarning;
            stable = desired;
            pending = null;
            confirmations = 0;
            return new ClientStatus(stable, null, reason, policy.ConfirmationSamples);
        }
        return new ClientStatus(stable, pending == stable ? null : pending, "Confirming:" + reason, confirmations);
    }

    // Call immediately on capture/profile/window failure. Previously valid measurements must not mask it.
    public ClientStatus Invalidate(string reason) => Unknown(string.IsNullOrWhiteSpace(reason) ? "InvalidSource" : reason);

    private ClientStatus Unknown(string reason)
    {
        stable = OperatorState.Unknown;
        pending = null;
        confirmations = 0;
        warning = false;
        full = false;
        return new ClientStatus(stable, null, reason, 0);
    }

    private string? Validate<T>(Measurement<T> measurement, TimeSpan now)
    {
        if (!measurement.IsValid) return measurement.Reason ?? "InvalidMeasurement";
        if (!StatePolicy.Unit(measurement.Confidence) || measurement.Confidence < policy.MinimumConfidence) return "LowConfidence";
        if (measurement.Stamp.Generation != generation) return "OldBinding";
        if (measurement.Stamp.Sequence <= 0) return "InvalidSequence";
        if (measurement.Stamp.CapturedAt < TimeSpan.Zero || measurement.Stamp.CapturedAt > now) return "InvalidTimestamp";
        if (now - measurement.Stamp.CapturedAt >= policy.Freshness) return "StaleMeasurements";
        return null;
    }

    private static bool Known(HarvesterState value) => value == HarvesterState.Active || value == HarvesterState.Inactive;
}
