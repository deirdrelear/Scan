# AGENTS.md

## Purpose

This branch contains the design foundation and first testable core for **MinerWatch**, a read-only external observer for a wall of EVE Online clients (target: about 20 Mackinaw mining clients).

The program must determine, for every client:

- mining hold fill level;
- Ice Harvester #1 active/inactive;
- Ice Harvester #2 active/inactive;
- derived operator state: GREEN / YELLOW / RED / UNKNOWN.

The intended use is an operator dashboard. MinerWatch observes pixels and reports state. It does **not** control EVE.

Before implementing or changing behavior, read:

- `docs/MINERWATCH_CONTRACT.md`
- `docs/MINERWATCH_ARCHITECTURE.md`
- `docs/adr/0001-observer-core.md`

Current implementation: `src/MinerWatch.Core` (.NET Standard 2.0),
`src/MinerWatch.Tool` and `tests/MinerWatch.Tests` (.NET 10).
Build `MinerWatch.slnx`; run the executable tests as documented in `README.md`.
The legacy `EveProj.sln` is a separate application, not a dependency of MinerWatch.
Do not describe measurement replay tests as real-image CV validation or capture benchmarks.

The contract is normative. If code and the contract disagree, stop and reconcile them before continuing.

---

## Non-negotiable safety boundary

MinerWatch must remain an **external, read-only observer**.

Never add any of the following against an EVE process/window:

- DLL injection;
- code injection of any kind;
- `OpenProcess` for VM access;
- `ReadProcessMemory`;
- `WriteProcessMemory`;
- `VirtualAllocEx`;
- `CreateRemoteThread`;
- DirectX / Present hooks;
- in-process overlays;
- `SetWindowsHookEx` into EVE threads;
- input synthesis or automation;
- `SendInput`;
- `PostMessage` / `SendMessage` used to operate the EVE UI;
- simulated mouse or keyboard actions;
- game-memory parsing.

The design goal is stronger than “no injection”: the EVE client should not need to cooperate with MinerWatch beyond being a normal top-level Windows window rendered by the desktop compositor.

Window discovery and observation through normal Windows window/compositor APIs are allowed.

`PrintWindow` is permitted only as an explicitly labelled diagnostic/fallback backend because it can cause the target window to process print messages. It must not be the preferred production capture path.

---

## Current repository: what is useful

The existing LocalScan application is a donor, not a final architecture.

Useful existing ideas/code:

- discovery of multiple EVE clients;
- character-to-window association;
- maintaining a collection of active characters;
- per-character persisted parameters;
- simple pixel colour-distance classification;
- fail-safe behavior where capture failure is treated as attention-worthy;
- WPF/MVVM infrastructure that can be reused where convenient.

Important donor files:

- `LocalScan/ViewModels/LocalScanTab/MainViewModel.cs`
- `LocalScan/ViewModels/LocalScanTab/CharacterViewModel.cs`
- `LocalScan/Models/Character.cs`
- `LocalScan/Helpers/BitmapHelper.cs`

Do not preserve an old implementation merely because it exists.

In particular, the final continuous monitoring path should not:

- capture a full-resolution bitmap for every detector;
- convert entire images to jagged `Pixel[][]` arrays;
- run capture/CV synchronously on the WPF dispatcher thread;
- use one process/window-cloner instance per EVE client.

---

## External reference: OnTopReplica

OnTopReplica-Refactor is a design reference for:

- HWND abstractions;
- window seekers;
- region abstractions;
- DWM thumbnail registration;
- source-region cropping;
- window lifecycle handling.

Reference repository:

- `giahoki/OnTopReplica-Refactor`

Useful concepts/files include:

- `ThumbnailPanel.cs`
- `ThumbnailRegion.cs`
- `WindowHandle.cs`
- `WindowSeekers/*`
- `MessagePumpProcessors/WindowKeeper.cs`

Do not fork its 1-source-window -> 1-OTR-window application model. MinerWatch must be one process managing many EVE windows.

---

## Target architecture

Keep hard boundaries between these layers:

1. **EveWindowRegistry**
   - discovers EVE top-level windows;
   - maintains stable logical client IDs;
   - tracks HWND lifecycle;
   - does not perform CV.

2. **ProfileEngine**
   - maps one UI profile to many clients;
   - resolves anchor-based ROIs;
   - validates resolution/UI-scale compatibility.

3. **CaptureEngine**
   - captures pixels without entering the EVE process;
   - first direct capture benchmark: per-window Windows.Graphics.Capture;
   - experimental candidate: DWM atlas + a proven pixel readback of MinerWatch's own atlas;
   - no production backend is selected until the benchmark is complete;
   - diagnostic fallback: PrintWindow.

4. **DetectionEngine**
   - receives only image ROIs and metadata;
   - knows nothing about HWND;
   - outputs detector measurements + confidence.

5. **StateEngine**
   - converts measurements to GREEN/YELLOW/RED/UNKNOWN;
   - applies hysteresis/debounce;
   - UNKNOWN is never GREEN.

6. **Presentation**
   - 20-client dashboard;
   - ordered attention queue;
   - optional sound / external physical semaphore.

Do not couple these layers through static globals.

---

## Expected logical state rules

Baseline policy:

- hold < 90% AND both harvesters active -> GREEN;
- hold >= 90% but not full AND both active -> YELLOW;
- one harvester inactive while not full -> YELLOW;
- hold full -> RED;
- both harvesters inactive -> RED;
- capture/detection invalid or confidence below threshold -> UNKNOWN.

Thresholds are profile/config values, not magic constants in detector code.

The state engine must use hysteresis and temporal confirmation so one valid but transient
operational reading does not flap state. Capture/measurement quality failures enter UNKNOWN
immediately. Recovery needs new frames; reprocessing a cached frame never adds confirmations.
Never reset a cached frame's capture timestamp on polling. Preserve generation and sequence.

---

## Profile principles

A UI profile is shared by all clients with the same EVE UI layout.

Prefer profile coordinates of the form:

- anchor;
- offset;
- width/height;
- reference resolution;
- UI scale;
- client-area physical pixels and reference Windows DPI;
- detector type;
- detector parameters.

Supported anchors should include at least:

- TopLeft;
- TopRight;
- BottomLeft;
- BottomRight;
- BottomCenter;
- Center.

Do not require the operator to draw three ROIs manually for every client.

Calibration should happen once per UI profile.

---

## CV principles

Start simple. Do not introduce OCR or ML unless deterministic image processing fails.

Mining hold detector should prefer:

- a small ROI;
- colour/luminance masking;
- 1D fill-boundary estimation;
- temporal smoothing.

Harvester detector should prefer:

- the activation/cycle-ring area rather than the static module icon;
- colour/brightness evidence;
- optional temporal phase/motion evidence;
- an explicit confidence value.

Every detector must be testable from saved fixture images without EVE running.

---

## Capture performance requirements

Target scale: ~20 clients.

Do not design for 60 FPS. Operational state changes are slow.

Initial target:

- hold sampling: ~1-2 Hz per client;
- harvester sampling: ~2-5 Hz per client;
- configurable/adaptive scheduling later.

Avoid transferring full 1080p/1440p frames to CPU if only three small ROIs are needed.

If using D3D textures, crop/copy only required ROIs before CPU readback whenever practical.

---

## Preferred implementation direction

For the first implementation phase:

1. extract/refactor existing EVE window discovery into a testable `EveWindowRegistry`;
2. introduce explicit capture and detector interfaces;
3. build detector fixtures from saved screenshots;
4. prove hold/harvester recognition on 3-5 stacked, non-minimized clients;
5. only then optimize the production capture backend.

Do not prematurely rewrite the whole WPF shell.

The registry, profile resolver, pixel/detector interfaces and state engine now exist.
Next: obtain real labelled image fixtures, implement calibrated detectors and benchmark
capture. Placeholder ROI positions are not measurements of the user's EVE layout.

---

## Coding expectations

- Prefer small interfaces and dependency injection over static helper classes.
- New core code should be unit-testable without a GUI.
- Use cancellation tokens for long-running loops.
- Capture/CV workers must not block the UI dispatcher.
- Dispose GDI/D3D/bitmap resources deterministically.
- Log state transitions, not every frame.
- Include client ID, detector, confidence and reason in diagnostic logs.
- Keep profile schema versioned.
- Treat UNKNOWN and stale data as first-class states.

When adding a new native API, document why it is needed and verify it does not violate the read-only boundary.

---

## Tests that must exist before calling the implementation production-ready

At minimum:

- window registry discovers N mock/real EVE windows correctly;
- HWND replacement after client restart is handled;
- one profile resolves identical ROIs for same-sized clients;
- resolution/profile mismatch becomes UNKNOWN;
- hold detector fixtures cover empty/mid/90%+/full;
- harvester detector fixtures cover ON/OFF and ambiguous frames;
- one bad frame does not flip stable state;
- stale capture becomes UNKNOWN;
- 20 simulated clients can be scheduled without UI blocking;
- capture backend failure for one client does not stop the others;
- no forbidden EVE-process access APIs exist in production code.

---

## Agent handoff rule

When uncertain between a clever optimization and a simpler design that preserves the external-observer boundary, choose the simpler design.

Do not silently weaken the contract in order to make implementation easier. Record unresolved assumptions in the architecture document or an ADR and keep them visible.
