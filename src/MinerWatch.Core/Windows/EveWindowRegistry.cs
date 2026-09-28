using System;
using System.Collections.Generic;
using System.Linq;

namespace MinerWatch.Core.Windows;

public sealed class ClientRegistration
{
    public string ClientId { get; }
    public string CharacterName { get; }

    public ClientRegistration(string clientId, string characterName)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(characterName))
            throw new ArgumentException("Client ID and character name are required.");
        ClientId = clientId.Trim();
        CharacterName = characterName.Trim();
    }
}

// Native handles are opaque values here. Only the Windows adapter uses native APIs.
public sealed class WindowObservation
{
    public long Handle { get; }
    public int ProcessId { get; }
    public long ProcessStartedUtcTicks { get; }
    public string ProcessName { get; }
    public string Title { get; }
    public string ClassName { get; }
    public int ClientWidth { get; }
    public int ClientHeight { get; }
    public uint Dpi { get; }
    public bool IsVisible { get; }
    public bool IsMinimized { get; }
    public bool IsCloaked { get; }

    public WindowObservation(long handle, int processId, long processStartedUtcTicks,
        string processName, string title, string className, int clientWidth, int clientHeight,
        uint dpi, bool isVisible = true, bool isMinimized = false, bool isCloaked = false)
    {
        Handle = handle;
        ProcessId = processId;
        ProcessStartedUtcTicks = processStartedUtcTicks;
        ProcessName = processName ?? "";
        Title = title ?? "";
        ClassName = className ?? "";
        ClientWidth = clientWidth;
        ClientHeight = clientHeight;
        Dpi = dpi;
        IsVisible = isVisible;
        IsMinimized = isMinimized;
        IsCloaked = isCloaked;
    }

    public string? CharacterName =>
        string.Equals(ProcessName, "exefile", StringComparison.OrdinalIgnoreCase) &&
        Title.StartsWith("EVE - ", StringComparison.Ordinal) && Title.Substring(6).Trim().Length > 0
            ? Title.Substring(6).Trim() : null;

    internal bool SameSource(WindowObservation other) => Handle == other.Handle &&
        ProcessId == other.ProcessId && ProcessStartedUtcTicks == other.ProcessStartedUtcTicks &&
        ClientWidth == other.ClientWidth && ClientHeight == other.ClientHeight && Dpi == other.Dpi &&
        ClassName == other.ClassName;
}

public interface IWindowEnumerator
{
    IReadOnlyList<WindowObservation> Enumerate();
}

public enum BindingStatus { Ready, Missing, Ambiguous, Unavailable }

public sealed class ClientBinding
{
    public ClientRegistration Client { get; }
    public long Generation { get; }
    public BindingStatus Status { get; }
    public WindowObservation? Window { get; }
    public string Reason { get; }

    internal ClientBinding(ClientRegistration client, long generation, BindingStatus status,
        WindowObservation? window, string reason)
    { Client = client; Generation = generation; Status = status; Window = window; Reason = reason; }
}

// Single owner (the discovery worker). Returned snapshots cannot mutate the registry.
public sealed class EveWindowRegistry
{
    private readonly ClientRegistration[] registrations;
    private readonly Dictionary<string, ClientBinding> previous = new Dictionary<string, ClientBinding>();

    public EveWindowRegistry(IEnumerable<ClientRegistration> registrations)
    {
        this.registrations = registrations?.ToArray() ?? throw new ArgumentNullException(nameof(registrations));
        if (this.registrations.Any(x => x == null) ||
            this.registrations.Select(x => x.ClientId).Distinct(StringComparer.Ordinal).Count() != this.registrations.Length ||
            this.registrations.Select(x => x.CharacterName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != this.registrations.Length)
            throw new ArgumentException("Roster contains null or duplicate IDs/character names.");
    }

    public IReadOnlyList<ClientBinding> Refresh(IEnumerable<WindowObservation> windows)
    {
        var observations = windows?.ToArray() ?? throw new ArgumentNullException(nameof(windows));
        var result = new List<ClientBinding>();
        foreach (var client in registrations)
        {
            var matches = observations.Where(x => string.Equals(x.CharacterName, client.CharacterName,
                StringComparison.OrdinalIgnoreCase)).ToArray();
            var window = matches.Length == 1 ? matches[0] : null;
            var status = matches.Length == 0 ? BindingStatus.Missing : matches.Length > 1 ? BindingStatus.Ambiguous :
                window!.Handle == 0 || window.ProcessId <= 0 || window.ProcessStartedUtcTicks <= 0 ||
                !window.IsVisible || window.IsMinimized || window.IsCloaked || window.ClientWidth <= 0 ||
                window.ClientHeight <= 0 || window.Dpi == 0 ? BindingStatus.Unavailable : BindingStatus.Ready;
            previous.TryGetValue(client.ClientId, out var old);
            var changed = old == null || old.Status != status ||
                (old.Window == null) != (window == null) ||
                (old.Window != null && window != null && !old.Window.SameSource(window));
            var generation = old == null ? 1 : checked(old.Generation + (changed ? 1 : 0));
            var binding = new ClientBinding(client, generation, status, window, status.ToString());
            previous[client.ClientId] = binding;
            result.Add(binding);
        }
        return result.AsReadOnly();
    }
}
