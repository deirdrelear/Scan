using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinerWatch.Core.Profiles;
using MinerWatch.Core.State;
using MinerWatch.Core.Windows;

namespace MinerWatch.Core.Capture;

// Owned, top-down BGRA8 ROI pixels. No HWND, GPU surface or borrowed native buffer reaches CV.
public sealed class RoiFrame
{
    private readonly byte[] pixels;
    public string RoiId { get; }
    public int Width { get; }
    public int Height { get; }
    public int Stride { get; }
    public FrameStamp Stamp { get; }
    public RoiFrame(string roiId, int width, int height, int stride, byte[] bgra, FrameStamp stamp)
    {
        if (string.IsNullOrWhiteSpace(roiId) || width <= 0 || height <= 0 || stride < (long)width * 4 ||
            bgra == null || (long)stride * height != bgra.LongLength) throw new ArgumentException("Invalid BGRA ROI.");
        RoiId = roiId; Width = width; Height = height; Stride = stride; Stamp = stamp;
        pixels = (byte[])bgra.Clone();
    }

    public byte Channel(int x, int y, int channel)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || channel < 0 || channel > 3)
            throw new ArgumentOutOfRangeException();
        return pixels[y * Stride + x * 4 + channel];
    }
}

public sealed class CaptureBatch
{
    public string? FailureReason { get; }
    public IReadOnlyList<RoiFrame> Frames { get; }
    public bool IsValid => FailureReason == null;
    private CaptureBatch(string? failureReason, RoiFrame[] frames)
    { FailureReason = failureReason; Frames = Array.AsReadOnly(frames); }

    public static CaptureBatch Success(IEnumerable<RoiFrame> frames)
    {
        var copy = frames?.ToArray() ?? throw new ArgumentNullException(nameof(frames));
        if (copy.Length == 0 || copy.Any(x => x == null) || copy.Select(x => x.RoiId).Distinct().Count() != copy.Length)
            throw new ArgumentException("A capture batch must have unique, non-null ROI frames.");
        return new CaptureBatch(null, copy);
    }

    public static CaptureBatch Failure(string reason) => new CaptureBatch(
        string.IsNullOrWhiteSpace(reason) ? "CaptureFailed" : reason, new RoiFrame[0]);
}

public interface ICaptureBackend : IDisposable
{
    string BackendId { get; }
    // Implementations must honor cancellation. No fallback may silently change the source of pixels.
    Task<CaptureBatch> CaptureAsync(ClientBinding binding, IReadOnlyList<ResolvedRoi> rois, CancellationToken cancellationToken);
}

public interface IRoiDetector<T>
{
    string DetectorId { get; }
    Measurement<T> Analyze(RoiFrame frame, IReadOnlyDictionary<string, double> parameters);
}
