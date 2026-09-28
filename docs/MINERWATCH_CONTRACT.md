# MinerWatch System Contract

Status: Draft foundation  
Branch: `feature/minerwatch-foundation`

## 1. Scope

MinerWatch is a Windows desktop application that observes multiple concurrently running EVE Online clients and reports mining-operational state for each client.

Initial target scale: approximately 20 Mackinaw clients.

For each client the system must estimate:

- mining hold fill ratio;
- Ice Harvester #1 active/inactive;
- Ice Harvester #2 active/inactive;
- overall operator state.

MinerWatch is an observer and alerting tool. It is not an input automation tool.

---

## 2. External-observer boundary

### 2.1 Required

The production implementation must remain outside the EVE process.

Allowed classes of interaction:

- enumerate top-level Windows windows/process metadata needed for discovery;
- read window title/class/geometry;
- use Windows desktop-compositor/window-capture APIs;
- analyse pixels captured into MinerWatch-owned memory;
- display dashboard, sound alerts, and optional external hardware indicators.

### 2.2 Forbidden

Production code must not:

- inject DLLs/code into EVE;
- hook DirectX/Present in EVE;
- read or write EVE virtual memory;
- allocate memory in EVE;
- create remote threads in EVE;
- install thread hooks into EVE;
- synthesize keyboard/mouse input to EVE;
- send window messages for the purpose of operating the game UI;
- parse internal game memory structures.

The project must prefer mechanisms where the EVE client does not need to actively cooperate with MinerWatch.

### 2.3 Capture-backend policy

Preferred production paths:

1. DWM-based composition/atlas;
2. Windows.Graphics.Capture per HWND.

`PrintWindow` may exist only as diagnostic/fallback functionality and must be labelled as such. It is not the default production backend.

---

## 3. Window assumptions

Initial operational assumptions:

- EVE windows are not minimized;
- windows may fully overlap one another in a stack;
- clients use a good, stable resolution;
- clients intended to share one UI profile use the same resolution and EVE UI scale;
- background render rate may be lower than foreground render rate.

The implementation must not assume that a client is visible on top of the Z-order.

---

## 4. Logical client identity

A transient HWND is not a stable client identity.

The system must maintain a logical `ClientId` for each monitored character/client and be able to re-bind that logical client when:

- EVE restarts;
- HWND changes;
- process restarts;
- discovery order changes.

Character/window title matching may be used as one identity signal.

---

## 5. Profile contract

A profile describes where and how to observe a specific EVE UI layout.

A profile must include:

- schema version;
- profile ID/name;
- reference width/height;
- expected UI scale or UI-scale identifier;
- ROI definitions;
- detector assignment per ROI;
- detector parameters;
- state-policy thresholds.

ROI definitions must support anchor-relative geometry.

Minimum anchor set:

- TopLeft;
- TopRight;
- BottomLeft;
- BottomRight;
- BottomCenter;
- Center.

A profile should normally be configured once and shared across all clients using the same UI layout.

If a client does not match profile geometry requirements, its status must become UNKNOWN instead of attempting unsafe guesswork.

---

## 6. Detector contract

Detectors consume image data only.

A detector must not receive or query an EVE HWND.

Each detector result must include:

- measurement value;
- confidence;
- timestamp;
- validity;
- optional diagnostic reason.

### 6.1 Mining hold detector

Required output:

- fill ratio in range 0.0..1.0;
- confidence;
- validity.

Preferred techniques:

- colour/luminance masking;
- horizontal fill-boundary estimation;
- smoothing across recent samples.

OCR is not required for the first implementation.

### 6.2 Harvester detector

Required output:

- ACTIVE;
- INACTIVE;
- UNKNOWN;
- confidence.

Preferred evidence:

- activation/cycle-ring colour or brightness;
- temporal phase/motion as supporting evidence;
- multiple recent samples.

The static module icon alone must not be treated as sufficient evidence of active cycling.

---

## 7. State contract

The initial policy is:

- hold < 90% AND H1 active AND H2 active -> GREEN;
- hold >= 90% AND not full AND both active -> YELLOW;
- exactly one harvester inactive while hold is not full -> YELLOW;
- hold full -> RED;
- both harvesters inactive -> RED;
- invalid/stale/ambiguous capture or detector result -> UNKNOWN.

Thresholds are configuration/profile values.

UNKNOWN must never be downgraded to GREEN.

The state engine must implement temporal confirmation/hysteresis to avoid flapping on single-frame errors.

---

## 8. Freshness contract

Every measurement has an age.

If required measurements are older than the configured freshness limit, the client becomes UNKNOWN.

A capture failure for one client must not stop monitoring of other clients.

---

## 9. Performance contract

Target: approximately 20 clients.

Initial sampling guidance:

- mining hold: 1-2 Hz/client;
- harvesters: 2-5 Hz/client.

The system must not require full-frame CPU processing for every detector evaluation.

Where the capture backend provides GPU textures, crop/copy only the required ROIs before CPU readback whenever practical.

The WPF UI thread must never be the capture or CV worker.

---

## 10. Failure semantics

Fail-safe behavior is mandatory.

Examples that must become UNKNOWN or attention-worthy:

- capture backend error;
- source window vanished;
- profile mismatch;
- ROI outside current source bounds;
- detector confidence below minimum;
- stale samples;
- unexpected source resolution.

A false GREEN is considered worse than a false UNKNOWN.

---

## 11. Presentation contract

The dashboard must make it possible to identify the few clients needing attention without cycling through all EVE windows.

Minimum per-client display:

- logical client/character name;
- overall state;
- hold percentage;
- H1 status;
- H2 status;
- last update age.

An attention queue should list RED, UNKNOWN, then YELLOW clients.

Optional later outputs:

- sound;
- tray indication;
- USB/serial/ESP32 physical semaphore.

Presentation must not directly operate EVE.

---

## 12. Testability contract

The core must be runnable against saved image fixtures without EVE installed or running.

Required test categories:

- client/window registry behavior;
- profile geometry resolution;
- hold detector;
- harvester detector;
- state engine;
- freshness handling;
- scheduler/load behavior;
- backend failure isolation.

Detector regression fixtures should be kept in a dedicated test-data location and labelled with expected outcomes.

---

## 13. Compatibility / implementation policy

The existing repository targets .NET Framework 4.8/WPF.

For the foundation phase:

- reuse the shell where useful;
- do not force an immediate UI rewrite;
- isolate new core services behind interfaces so a later migration to modern .NET is possible.

The architecture must not depend on legacy static helper classes.

---

## 14. Acceptance criteria for first useful prototype

A prototype is considered useful when, using 3-5 stacked, non-minimized EVE clients, it can continuously display for each client:

- hold percentage;
- H1 active/inactive;
- H2 active/inactive;
- overall GREEN/YELLOW/RED/UNKNOWN;

with no game-process injection, no automated input, and no manual per-client ROI placement when all clients share one profile.
