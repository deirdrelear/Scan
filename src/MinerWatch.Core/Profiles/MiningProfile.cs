using System;
using System.Collections.Generic;
using System.Linq;
using MinerWatch.Core.State;

namespace MinerWatch.Core.Profiles;

public enum RoiAnchor { TopLeft, TopRight, BottomLeft, BottomRight, BottomCenter, Center }

public readonly struct PixelRect
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    public PixelRect(int x, int y, int width, int height)
    { X = x; Y = y; Width = width; Height = height; }
}

public sealed class RoiDefinition
{
    public string Id { get; }
    public RoiAnchor Anchor { get; }
    public int OffsetX { get; }
    public int OffsetY { get; }
    public int Width { get; }
    public int Height { get; }
    public string Detector { get; }
    public IReadOnlyDictionary<string, double> Parameters { get; }

    public RoiDefinition(string id, RoiAnchor anchor, int offsetX, int offsetY, int width, int height,
        string detector, IDictionary<string, double>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(detector) || width <= 0 || height <= 0 ||
            !Enum.IsDefined(typeof(RoiAnchor), anchor)) throw new ArgumentException("Invalid ROI definition.");
        var copy = parameters == null ? new Dictionary<string, double>() : new Dictionary<string, double>(parameters);
        if (copy.Any(p => string.IsNullOrWhiteSpace(p.Key) || double.IsNaN(p.Value) || double.IsInfinity(p.Value)))
            throw new ArgumentException("Detector parameters must be named, finite values.");
        Id = id; Anchor = anchor; OffsetX = offsetX; OffsetY = offsetY;
        Width = width; Height = height; Detector = detector;
        Parameters = new System.Collections.ObjectModel.ReadOnlyDictionary<string, double>(copy);
    }
}

public sealed class MiningProfile
{
    public int SchemaVersion { get; }
    public string Id { get; }
    public int ReferenceWidth { get; }
    public int ReferenceHeight { get; }
    public string UiScale { get; }
    public uint ReferenceDpi { get; }
    public bool Calibrated { get; }
    public IReadOnlyList<RoiDefinition> Rois { get; }
    public StatePolicy Policy { get; }

    public MiningProfile(int schemaVersion, string id, int referenceWidth, int referenceHeight,
        string uiScale, uint referenceDpi, bool calibrated, IEnumerable<RoiDefinition> rois, StatePolicy policy)
    {
        if (schemaVersion != 1) throw new ArgumentException("Unsupported profile schema version.");
        if (string.IsNullOrWhiteSpace(id) || referenceWidth <= 0 || referenceHeight <= 0 ||
            string.IsNullOrWhiteSpace(uiScale) || referenceDpi == 0) throw new ArgumentException("Invalid profile metadata.");
        var list = rois?.ToArray() ?? throw new ArgumentNullException(nameof(rois));
        if (list.Length != 3 || list.Any(x => x == null) ||
            !list.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal)
                .SequenceEqual(new[] { "harvester1", "harvester2", "hold" }))
            throw new ArgumentException("Exactly hold, harvester1, harvester2 ROIs are required.");
        if (list.Any(x => x.Detector != (x.Id == "hold" ? "HoldGauge" : "Harvester")))
            throw new ArgumentException("Unsupported detector assignment.");
        SchemaVersion = schemaVersion; Id = id; ReferenceWidth = referenceWidth; ReferenceHeight = referenceHeight;
        UiScale = uiScale; ReferenceDpi = referenceDpi; Calibrated = calibrated; Rois = Array.AsReadOnly(list);
        Policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }
}

public sealed class ResolvedRoi
{
    public RoiDefinition Definition { get; }
    public PixelRect Bounds { get; }
    internal ResolvedRoi(RoiDefinition definition, PixelRect bounds) { Definition = definition; Bounds = bounds; }
}

public sealed class ProfileResolution
{
    public bool IsValid => Reason == null;
    public string? Reason { get; }
    public IReadOnlyList<ResolvedRoi> Rois { get; }
    internal ProfileResolution(string? reason, IList<ResolvedRoi> rois)
    { Reason = reason; Rois = new List<ResolvedRoi>(rois).AsReadOnly(); }
}

public static class ProfileResolver
{
    // Source coordinates are CLIENT-AREA PHYSICAL PIXELS, not WPF DIPs or outer-window coordinates.
    public static ProfileResolution Resolve(MiningProfile profile, int width, int height, uint dpi, string? confirmedUiScale)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        ProfileResolution Fail(string reason) => new ProfileResolution(reason, new List<ResolvedRoi>());
        if (!profile.Calibrated) return Fail("ProfileNotCalibrated");
        if (width != profile.ReferenceWidth || height != profile.ReferenceHeight || dpi != profile.ReferenceDpi ||
            !string.Equals(confirmedUiScale, profile.UiScale, StringComparison.Ordinal)) return Fail("ProfileMismatch");
        var result = new List<ResolvedRoi>();
        foreach (var roi in profile.Rois)
        {
            long x = roi.Anchor == RoiAnchor.TopRight || roi.Anchor == RoiAnchor.BottomRight ? width :
                roi.Anchor == RoiAnchor.BottomCenter || roi.Anchor == RoiAnchor.Center ? width / 2 : 0;
            long y = roi.Anchor == RoiAnchor.BottomLeft || roi.Anchor == RoiAnchor.BottomRight || roi.Anchor == RoiAnchor.BottomCenter ? height :
                roi.Anchor == RoiAnchor.Center ? height / 2 : 0;
            x += roi.OffsetX; y += roi.OffsetY;
            if (x < 0 || y < 0 || x + roi.Width > width || y + roi.Height > height) return Fail("RoiOutOfBounds:" + roi.Id);
            result.Add(new ResolvedRoi(roi, new PixelRect((int)x, (int)y, roi.Width, roi.Height)));
        }
        return new ProfileResolution(null, result);
    }
}
