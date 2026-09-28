# MinerWatch review follow-ups

Date: 2026-09-28
Applies after commit: `c16cc4b29154691b7bed74c248a66e6419f2052a`

These are review findings to process before or alongside the next implementation increment.
They are not a replacement for the normative contract.

## 1. Recovery must require a genuinely new source frame

Current `ClientStateEngine` correctly enters UNKNOWN immediately for low confidence,
invalid measurements, unknown harvester state, excessive skew, etc. However, a bad
measurement can be rejected before its `FrameStamp` is recorded in the source-frame
watermark.

Risk:

1. frame N is captured;
2. detector result for frame N is invalid / low-confidence -> UNKNOWN;
3. the same frame N is classified again and now returns a valid result;
4. frame N can participate in recovery confirmation even though no new source frame arrived.

This violates the intended invariant:

> Recovery needs new frames; reprocessing a cached frame never adds confirmations.

Required change:

- advance/record a per-stream source-frame watermark for every structurally valid source
  frame that reaches state evaluation, even when the detector result is unusable;
- quality failure still enters UNKNOWN immediately;
- recovery confirmation must require a strictly newer source frame than the frame that
  caused/participated in the failure;
- add explicit regression tests for low-confidence, invalid measurement, unknown harvester
  and measurement-skew cases using the same frame again after failure.

Do not solve this by changing timestamps on a cached frame.

## 2. Profiles must not be limited to exactly three ROIs

The current `MiningProfile` requires exactly:

- `hold`
- `harvester1`
- `harvester2`

Those are the three required semantic outputs, but real CV may also need support ROIs:

- mining-hold tab/header presence;
- inventory state/panel anchor;
- module-rack anchor;
- UI-presence marker;
- calibration/template landmarks;
- other support evidence discovered from real fixtures.

Required direction:

- keep the three required semantic outputs;
- allow additional profile ROIs;
- separate, where practical, ROI identity from detector identity and semantic output role;
- do not force a schema break merely to add UI-presence/anchor regions;
- keep fail-closed behavior if a required support ROI is missing or invalid.

The exact model may be refined after real screenshots are inspected, but the core profile
container should no longer hard-code `Rois.Count == 3`.

Add tests showing:

- the three required semantic regions are mandatory;
- extra support ROIs are accepted;
- duplicate IDs are rejected;
- unsupported required-role mappings fail closed.

## 3. Process metadata boundary is currently accepted, but must stay query-only

Current Windows discovery uses `System.Diagnostics.Process` to read process name and
process start time. This may cause Windows to open a process handle with metadata/query
rights. This is acceptable for now and is useful for PID-reuse protection.

Do not expand this into VM/process-memory access.

Still forbidden:

- `PROCESS_VM_READ` / `PROCESS_VM_WRITE`;
- `ReadProcessMemory`;
- `WriteProcessMemory`;
- remote allocation/threads;
- injection/hooking;
- any input/control path to EVE.

If a future design can remove process-handle queries without losing reliable identity,
record that as a separate ADR. Do not weaken identity just to claim "HWND only".

## 4. Real screenshots are the next evidence source

If real EVE screenshots/ROI sequences are added under `tests/fixtures/images/`, inspect
the actual image content before implementing detectors.

Rules:

- do not infer detector thresholds from filenames alone;
- do not claim an image was analysed unless the environment actually opened/decoded it;
- use full client-area screenshots for geometry calibration;
- use lossless ROI crops and short sequences for detector regression;
- preserve labels/metadata for expected hold/H1/H2 state;
- keep character names optional/redacted;
- split calibration and held-out evaluation fixtures;
- report ambiguous examples as UNKNOWN rather than forcing a label.

Recommended manifest per fixture/sequence:

- file or sequence ID;
- expected hold ratio/range;
- expected H1/H2 state;
- client-area resolution;
- EVE UI scale;
- Windows DPI;
- UI theme;
- foreground/fully-covered state;
- capture backend;
- notes about tooltips/occlusion/wrong tab/ambiguity.

A simple `manifest.json` or `manifest.jsonl` beside the images is preferred.

## 5. Next implementation priority

Before dashboard work, prefer this order:

1. fix source-frame watermark semantics;
2. relax/extend profile ROI model;
3. ingest and inspect real EVE fixtures;
4. implement deterministic hold and harvester detectors with positive UI-presence checks;
5. create the 3-5 stacked-client capture PoC;
6. benchmark WGC vs DWM Atlas according to `docs/CAPTURE_BENCHMARK.md`;
7. only then spend significant effort on operator UI.

Do not treat the current measurement replay as CV validation.
