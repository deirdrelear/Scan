# ADR 0001 — Separate core, source provenance, capture remains unproven

Date: 2026-09-28
Status: Accepted for the first implementation increment

## Findings in the donor

`LocalScan.ViewModels.LocalScanTab.MainViewModel.OnScanProcess` matches process names
by substring, groups by title and selects the first process. It cannot distinguish
an ambiguous title from a reliable identity. It uses a DispatcherTimer for scanning.
`BitmapHelper` captures outer-window geometry through PrintWindow and silently falls
back to GetWindowDC/BitBlt. Returned cropped/full Bitmaps are not consistently disposed.
`Common.Helpers.BringToFront` contains a focus-changing API. None of these helpers
belongs in the new observer's dependency graph. The legacy executable remains separate;
this increment does not certify or rewrite it.

## Decisions

### 1. Preserve a bridge to the existing UI

Create `MinerWatch.Core` targeting .NET Standard 2.0, with no native imports, WPF,
System.Drawing, external CV packages or JSON dependency. .NET Framework 4.8 can
consume this target. Use .NET 10 for the development CLI and executable tests.
No migration of the existing WPF projects is required. JSON belongs to the host;
validated immutable domain objects belong to the core.

Reference: [Microsoft .NET Standard compatibility](https://learn.microsoft.com/en-us/dotnet/standard/net-standard).
[.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
informs the .NET 10 development-host choice. This is not a decision to migrate the final UI.

### 2. A title is a binding hint, not a durable identifier

The registry accepts a configured roster of stable ClientId/character-name pairs.
It only accepts an exact `exefile` process name and the provisional `EVE - <name>`
title grammar. Duplicate matches become Ambiguous, not first-match-wins.
Missing clients stay in the roster. Persisting the roster in the eventual host is
still required; the core does not invent a stable ID from HWND or discovery order.
Character renames require explicit roster updates. Current title/class conventions
must be checked using the Windows discovery command.

Each binding carries a generation that changes on process/HWND/geometry/DPI changes
or availability transitions. Process start time distinguishes PID reuse. A failed
process metadata query is never treated as a successful binding. When a profile,
detector configuration or capture session changes, the future coordinator must also
create a new observation epoch (or a fresh engine) and discard in-flight old results.
Registry generations are runtime values, not persisted across observer restarts.

### 3. Define coordinates before doing capture

Profiles use client-area physical pixels. The anchor is a reference point; offset
locates the ROI's top-left corner. Right/bottom anchors are the exclusive outer edge;
center uses integer floor. Coordinates are not scaled automatically. UI scale is
operator-confirmed metadata; Windows DPI does not reveal EVE's UI scale.
Each backend must explicitly map its texture origin to client coordinates. WGC
ContentSize, DWM source coordinates, window borders and DPI virtualization must not
be mixed. Clipping an out-of-bounds ROI is prohibited; invalidate the profile instead.

### 4. Freshness is the age of the source frame

Measurements carry generation, frame sequence and capture time from one monotonic
clock domain. UTC is useful for human logs, not for expiry. Polling or classifying
the same frame again must preserve its identity/time. Out-of-order data, future
timestamps, low confidence, excessive detector age skew and expired data are UNKNOWN.
Confirmation requires advances in **all three required streams**; fast harvester
updates cannot repeatedly confirm an old hold measurement.

Quality failures enter UNKNOWN immediately. Valid but changing operational states
need N confirmations and hold thresholds have separate enter/exit values. Recovery
from UNKNOWN needs new evidence. A previously confirmed colour may remain visible
while a valid operational transition is pending; the pending state is exposed.
UNKNOWN is not delayed by this debounce rule. The full threshold of 0.99 is an
example policy value, not a measured EVE bar-quantization result.

Capture-delivery freshness is **not proof that EVE itself rendered a new scene**.
Unchanged ROI pixels can be legitimate. Do not invalidate solely on image hash,
or stamp cached pixels with the time of a poll. Source-freeze detection needs a
separately validated liveness signal and Windows experiments.

### 5. DWM Atlas is an experiment, not a guaranteed pixel backend

DWM thumbnails establish live visual relationships; the documented API does not
return a CPU bitmap. The proposed path still needs a tested readback mechanism for
the owned destination window. Hidden/off-screen/minimized/covered atlas behavior
and whether capture includes the composed thumbnails are **unresolved**.
An animating atlas overlay would not prove that any individual source is fresh.
Do not promote this path based only on seeing a live thumbnail on screen.

WGC is the first direct capture baseline to measure. This is an experiment order,
not a declaration of a production winner. CreateForWindow has Windows 10 1903 as its
documented minimum; the old machine may have an earlier Windows 10 LTSC build.
Determine the actual build and API support on the deployment machine. Do not
silently fall back to PrintWindow. No promise of invisibility to the game or to
Windows follows from being an external observer.

References:

- [DWM thumbnail overview](https://learn.microsoft.com/en-us/windows/win32/dwm/thumbnail-ovw)
- [DwmRegisterThumbnail](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmregisterthumbnail)
- [WGC frame acquisition, QPC time and ContentSize](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture)
- [CreateForWindow requirements](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow)

### 6. No fabricated EVE detectors

No labelled EVE mining screenshots were supplied with this checkout. The current
fixtures are explicitly **measurement replay**, not detector-validation images.
Interfaces, owned BGRA ROI data and parameter dictionaries are present; actual
hold/harvester algorithms remain pending. Empty/black ROIs must not mean empty hold
or inactive modules without positive UI-presence evidence. The placeholder profile
is not calibrated and cannot resolve capture ROIs until calibrated.

## Native API inventory (development discovery adapter only)

| API | Purpose |
|---|---|
| EnumWindows | Enumerate top-level windows |
| GetWindowThreadProcessId | Associate window and process metadata |
| GetWindowTextW, GetClassNameW | Read title and class |
| GetClientRect | Read client-area geometry |
| IsWindowVisible, IsIconic | Read availability flags |
| GetDpiForWindow | Detect geometry/DPI changes |
| DwmGetWindowAttribute (DWMWA_CLOAKED) | Read cloaking state |
| SetThreadDpiAwarenessContext | Set and restore only the observer thread's coordinate context |

`System.Diagnostics.Process` reads process name/start time and disposes its handle.
These are metadata queries, not virtual-memory access. No screenshot or input API
is invoked by the `windows` command. There are no production capture native calls yet.

## Remaining implementation work

- Real-image fixtures, positive UI-presence checks and calibrated detectors.
- Capture provenance/liveness experiments, bounded GPU readback and session recovery.
- Per-client latest-frame slots, bounded scheduler, cancellation/timeout behavior.
- Coordinator joining discovery, profile version, capture session epoch and state.
- Host persistence, WPF presentation and transition logs.

Tests with 20 independent registries/state engines do not benchmark 20 live captures
and do not validate scheduler failure isolation. Those acceptance gates remain open.
