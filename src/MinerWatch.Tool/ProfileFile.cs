using System.Text.Json;
using System.Text.Json.Serialization;
using MinerWatch.Core.Profiles;
using MinerWatch.Core.State;

namespace MinerWatch.Tool;

public static class ProfileFile
{
    internal static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static MiningProfile Load(string path)
    {
        using var stream = File.OpenRead(path);
        var dto = JsonSerializer.Deserialize<ProfileDto>(stream, JsonOptions) ?? throw new InvalidDataException("Empty profile.");
        if (dto.Reference == null || dto.Rois == null || dto.Policy == null || dto.Rois.Any(x => x == null))
            throw new InvalidDataException("Profile sections cannot be null.");
        var p = dto.Policy;
        return new MiningProfile(dto.SchemaVersion, dto.Id, dto.Reference.Width, dto.Reference.Height,
            dto.Reference.UiScale, dto.Reference.Dpi, dto.Calibrated,
            dto.Rois.Select(x => new RoiDefinition(x.Id, x.Anchor, x.OffsetX, x.OffsetY, x.Width, x.Height, x.Detector, x.Parameters)),
            new StatePolicy(p.WarningEnter, p.WarningExit, p.FullEnter, p.FullExit, p.MinimumConfidence,
                p.FreshnessMs, p.MaximumSkewMs, p.ConfirmationSamples));
    }

    private sealed class ProfileDto
    {
        public required int SchemaVersion { get; init; }
        public required string Id { get; init; }
        public required bool Calibrated { get; init; }
        public required ReferenceDto Reference { get; init; }
        public required RoiDto[] Rois { get; init; }
        public required PolicyDto Policy { get; init; }
    }
    private sealed class ReferenceDto
    {
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required string UiScale { get; init; }
        public required uint Dpi { get; init; }
    }
    private sealed class RoiDto
    {
        public required string Id { get; init; }
        public required RoiAnchor Anchor { get; init; }
        public required int OffsetX { get; init; }
        public required int OffsetY { get; init; }
        public required int Width { get; init; }
        public required int Height { get; init; }
        public required string Detector { get; init; }
        public required Dictionary<string, double> Parameters { get; init; }
    }
    private sealed class PolicyDto
    {
        public required double WarningEnter { get; init; }
        public required double WarningExit { get; init; }
        public required double FullEnter { get; init; }
        public required double FullExit { get; init; }
        public required double MinimumConfidence { get; init; }
        public required int FreshnessMs { get; init; }
        public required int MaximumSkewMs { get; init; }
        public required int ConfirmationSamples { get; init; }
    }
}
