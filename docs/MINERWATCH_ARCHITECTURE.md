# MinerWatch Architecture

Status: Draft foundation  
Branch: `feature/minerwatch-foundation`

## 1. Design goals

MinerWatch should be:

- one application managing many EVE windows;
- read-only toward EVE;
- profile-driven;
- detector-testable offline;
- resilient to HWND replacement and individual capture failure;
- efficient enough for about 20 simultaneous clients;
- simple enough for a later model/agent to evolve without breaking the safety boundary.

Non-goal: controlling EVE.

---

## 2. High-level topology

```text
+-------------------------+
|       EVE clients       |
|  ~20 top-level HWNDs    |
+-----------+-------------+
            |
            | window discovery / compositor observation
            v
+-------------------------+
|    EveWindowRegistry    |
| ClientId <-> HWND       |
+-----------+-------------+
            |
            v
+-------------------------+
|      ProfileEngine      |
| resolves ROI geometry   |
+-----------+-------------+
            |
            v
+-------------------------+
|      CaptureEngine      |
| DWM atlas / WGC / diag  |
+-----------+-------------+
            |
            | ROI frames only
            v
+-------------------------+
|     DetectionEngine     |
| hold + harvester state  |
+-----------+-------------+
            |
            v
+-------------------------+
|       StateEngine       |
| G / Y / R / UNKNOWN     |
+-----------+-------------+
            |
            v
+-------------------------+
| Dashboard / Alerts      |
+-------------------------+
```

The layers should communicate via explicit DTOs/interfaces rather than static global state.

---

## 3. Components

### 3.1 EveWindowRegistry

Responsibilities:

- discover running EVE clients;
- expose logical clients as a collection;
- bind character/client identity to current HWND;
- detect disappearance/replacement;
- expose source geometry and basic metadata;
- publish lifecycle events.

Suggested data model:

```csharp
public sealed record EveClientDescriptor(
    string ClientId,
    string CharacterName,
    IntPtr Hwnd,
    int ProcessId,
    string WindowTitle,
    int Width,
    int Height,
    DateTimeOffset LastSeen);
```

Notes:

- `ClientId` should survive HWND replacement.
- process enumeration logic from existing LocalScan is a good donor.
- registry should be independently testable behind a window-enumerator abstraction.

---

### 3.2 ProfileEngine

Responsibilities:

- load/version profile documents;
- validate source geometry compatibility;
- resolve anchor-based ROIs to absolute pixel rectangles;
- provide detector configuration;
- provide thresholds/hysteresis settings.

Suggested profile model:

```json
{
  "schemaVersion": 1,
  "id": "eve-mining-2560x1440-ui100",
  "reference": {
    "width": 2560,
    "height": 1440,
    "uiScale": "100"
  },
  "rois": {
    "hold": {
      "anchor": "BottomRight",
      "offsetX": -320,
      "offsetY": -180,
      "width": 230,
      "height": 30,
      "detector": "HoldGauge"
    },
    "harvester1": {
      "anchor": "BottomCenter",
      "offsetX": -95,
      "offsetY": -115,
      "width": 72,
      "height": 72,
      "detector": "Harvester"
    },
    "harvester2": {
      "anchor": "BottomCenter",
      "offsetX": -15,
      "offsetY": -115,
      "width": 72,
      "height": 72,
      "detector": "Harvester"
    }
  }
}
```

Manual calibration should be performed once per UI profile, not once per client.

Future extension:

- optional template-based automatic anchor discovery;
- multiple profile variants for resolution/UI scale.

---

### 3.3 CaptureEngine

Contract:

```csharp
public interface ICaptureBackend : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);

    Task<CaptureBatch> CaptureAsync(
        EveClientDescriptor client,
        IReadOnlyList<ResolvedRoi> rois,
        CancellationToken cancellationToken);
}
```

A `CaptureBatch` should contain only requested ROIs plus timestamps/diagnostics.

#### Backend A: DwmAtlasBackend (preferred experiment)

Concept:

- MinerWatch owns one hidden/off-screen/top-level atlas HWND;
- register multiple DWM thumbnail relationships from EVE HWNDs;
- each relationship uses `rcSource` to select only required source region;
- DWM composes all required ROIs into the atlas;
- capture/read the MinerWatch-owned atlas;
- split atlas cells back into logical ROI frames.

Potential advantages:

- one process;
- no injection;
- no direct game-memory access;
- source windows may overlap;
- can reduce capture fan-out.

Key experiment:

- verify live updates for fully overlapped, non-minimized EVE windows;
- verify background-render behavior;
- verify no unintended black/stale frames.

#### Backend B: WgcMultiWindowBackend

Concept:

- one `Windows.Graphics.Capture` session per source HWND;
- shared D3D11 device;
- crop/copy only requested ROIs;
- CPU readback only for small regions.

Advantages:

- modern API;
- direct window capture semantics;
- clean per-client failure isolation.

Tradeoff:

- 20 simultaneous capture sessions need empirical cost measurement.

#### Backend C: PrintWindowDiagnosticBackend

Reuse/refactor the current `BitmapHelper` approach only for:

- calibration;
- detector development;
- fallback diagnostics;
- proof-of-concept.

Not preferred for production because it can cause target-window print handling and captures full bitmaps inefficiently.

---

### 3.4 DetectionEngine

Suggested contracts:

```csharp
public interface IRoiDetector
{
    string DetectorId { get; }
    DetectorResult Analyze(in RoiFrame frame, DetectorContext context);
}
```

```csharp
public sealed record DetectorResult(
    bool IsValid,
    double Confidence,
    object Measurement,
    string? Reason,
    DateTimeOffset Timestamp);
```

Prefer typed detector result subtypes in final code.

#### HoldGaugeDetector

Pipeline candidate:

1. crop already provided by CaptureEngine;
2. convert to a robust colour representation if needed;
3. threshold expected active-fill colour/luminance;
4. compute horizontal occupancy/projection;
5. estimate fill boundary;
6. apply temporal smoothing outside or inside detector;
7. return `FillRatio` + confidence.

Do not OCR numeric text in v1.

#### HarvesterDetector

Pipeline candidate:

1. use an annulus/ring mask around the module;
2. measure activation colour/brightness;
3. optionally compare ring phase across recent frames;
4. fuse evidence;
5. return ACTIVE / INACTIVE / UNKNOWN + confidence.

The detector should remain valid at low background FPS; motion is supporting evidence, not the sole criterion.

---

### 3.5 Scheduler

Do not run one busy loop per detector.

Suggested centralized scheduler:

```text
GREEN client:
  hold       every 500-1000 ms
  harvesters every 250-500 ms

YELLOW:
  temporarily increase relevant detector frequency

RED:
  slower refresh is acceptable after confirmation

UNKNOWN:
  retry capture at a bounded diagnostic cadence
```

Initial implementation can use fixed intervals before adaptive scheduling is added.

Use:

- background tasks;
- cancellation tokens;
- bounded concurrency;
- per-client failure isolation.

---

### 3.6 StateEngine

Input model:

```csharp
public sealed record ClientMeasurements(
    TimedMeasurement<double> HoldFill,
    TimedMeasurement<HarvesterState> H1,
    TimedMeasurement<HarvesterState> H2);
```

Output:

```csharp
public enum OperatorState
{
    Green,
    Yellow,
    Red,
    Unknown
}
```

State evaluation order should prioritize uncertainty/freshness before normal rules.

Example:

```text
if required data invalid/stale -> UNKNOWN
else if hold >= fullThreshold -> RED
else if H1 == OFF && H2 == OFF -> RED
else if hold >= warningThreshold -> YELLOW
else if exactly one harvester OFF -> YELLOW
else -> GREEN
```

Transitions require configurable confirmation counts/times.

Example:

- GREEN -> YELLOW hold threshold: >= 0.90 for N samples;
- YELLOW -> GREEN: <= 0.87 for N samples.

This prevents threshold flapping.

---

### 3.7 Presentation

Initial WPF dashboard can be evolved from the current repository.

Suggested main view:

```text
01  GREEN    42%   H1 ON   H2 ON
02  GREEN    61%   H1 ON   H2 ON
03  YELLOW   94%   H1 ON   H2 ON
04  RED     100%   H1 ON   H2 ON
05  UNKNOWN   --   H1 ?    H2 ?
...
```

Secondary attention list:

1. RED;
2. UNKNOWN;
3. YELLOW.

Avoid forcing the operator to switch through all EVE windows.

Future:

- system tray summary;
- sound;
- serial/USB/ESP32 3-light semaphore.

---

## 4. Data flow

```text
WindowRegistry
   |
   | EveClientDescriptor
   v
ProfileEngine
   |
   | ResolvedRoi[]
   v
CaptureEngine
   |
   | RoiFrame[]
   v
DetectionEngine
   |
   | DetectorResult[]
   v
MeasurementStore
   |
   v
StateEngine
   |
   | ClientStatus
   v
Dashboard
```

No reverse command path to EVE exists.

---

## 5. Reuse plan from existing Scan code

### Keep/refactor

- EVE process discovery logic from `MainViewModel.OnScanProcess`;
- character collection concept;
- persisted per-character/profile settings concept;
- basic colour-distance functions as detector primitives;
- alarm/sound plumbing if still useful;
- WPF MVVM shell selectively.

### Replace

- `DispatcherTimer` scanning on UI thread;
- full-window `Bitmap` capture for every evaluation;
- `Pixel[][]` conversion;
- static monolithic `BitmapHelper`;
- XML as the main new profile format;
- tightly coupled character/viewmodel/capture logic.

---

## 6. Suggested project structure

A minimal evolution without an immediate framework migration:

```text
LocalScan/
  MinerWatch/
    Abstractions/
      ICaptureBackend.cs
      IWindowEnumerator.cs
      IRoiDetector.cs

    Windows/
      EveWindowRegistry.cs
      Win32WindowEnumerator.cs

    Profiles/
      MiningUiProfile.cs
      RoiDefinition.cs
      ProfileLoader.cs
      ProfileResolver.cs

    Capture/
      PrintWindowDiagnosticBackend.cs
      DwmAtlasBackend.cs
      WgcMultiWindowBackend.cs

    Detection/
      HoldGaugeDetector.cs
      HarvesterDetector.cs

    State/
      ClientMeasurementStore.cs
      ClientStateEngine.cs
      OperatorState.cs

    Scheduling/
      MonitorScheduler.cs

    Presentation/
      MinerWatchViewModel.cs
```

Tests:

```text
MinerWatch.Tests/
  Profiles/
  Detection/
  State/
  Registry/
  Scheduling/
  Fixtures/
```

The exact solution/project split can be changed by Astra later; dependency boundaries matter more than folder names.

---

## 7. First implementation sequence

### Phase 0 - foundation

- add contracts/docs;
- extract interfaces;
- no behavioral rewrite yet.

### Phase 1 - registry

- refactor EVE discovery;
- stable logical identity;
- mockable tests.

### Phase 2 - offline detector development

- capture labelled screenshots from existing Scan diagnostic path;
- build hold fixtures;
- build harvester ON/OFF fixtures;
- implement deterministic detectors;
- regression tests.

### Phase 3 - stacked-client PoC

Using 3-5 non-minimized, overlapping EVE clients:

- detect all clients;
- use one profile;
- continuously show hold/H1/H2/state;
- measure latency and CPU use.

This phase may temporarily use `PrintWindowDiagnosticBackend` to isolate CV from capture-backend work.

### Phase 4 - production capture

Prototype both:

- DWM atlas;
- WGC multi-window.

Benchmark:

- correctness while fully overlapped;
- stale/black frames;
- GPU usage;
- CPU usage;
- memory;
- source-client impact;
- recovery after window restart.

Select production default based on measurements.

### Phase 5 - operator UX

- 20-client grid;
- attention queue;
- state-transition logging;
- optional physical semaphore.

---

## 8. Open questions / ADR candidates

A later implementation pass should explicitly decide and record:

1. DWM atlas vs WGC as production default after benchmark.
2. Exact EVE window process/title/class matching on current client version.
3. Profile format: JSON with System.Text.Json/Newtonsoft/other depending target framework.
4. Whether to remain on .NET Framework 4.8 or migrate core/UI to modern .NET.
5. Exact hold bar visual signature and how it varies by EVE UI colour theme.
6. Exact harvester activation-ring signature at background frame rates.
7. Whether EVE clients use identical UI scale and module-rack placement in the intended deployment.
8. Whether profile calibration should be manual-first or template-assisted in v1.

---

## 9. Architecture invariant

The most important invariant is:

```text
EVE-facing side: window/compositor observation only
                     |
                     v
               captured pixels
                     |
               trust boundary
                     |
                     v
          CV / state / presentation
```

If a future implementation proposal crosses that boundary to make recognition easier, it must be rejected or explicitly reconsidered at the contract level before code is written.
