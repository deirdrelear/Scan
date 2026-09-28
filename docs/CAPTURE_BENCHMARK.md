# Production capture decision protocol

Status: **Not run**. No production backend selected or implemented by the core increment.

## Setup

Record Windows edition/build, GPU/driver, monitor DPI/HDR configuration, EVE resolution,
UI scale, foreground/background frame limits, client count and observer build SHA.
Use 3–5 clients first, then the intended approximately 20. Do not change EVE settings,
focus, Z-order or input automatically. The operator performs scenario changes manually.

Compare WGC per window (shared D3D device) with DWM thumbnails composed into one
owned atlas plus an explicitly named readback method. PrintWindow is a separately
labelled diagnostic run only. Unsupported APIs produce an explicit unavailable result.

## Correctness matrix

| Scenario | Observe / acceptance |
|---|---|
| All clients visible | Pixel and client identity agree with labelled ground truth |
| Clients fully overlapping, not minimized | Every source continues to produce the correct ROIs |
| Source minimized, cloaked or closed | UNKNOWN; no cached GREEN |
| Restart and HWND/PID replacement | Epoch changes; old results discarded; new evidence required |
| Resize, DPI change, UI scale/layout mismatch | Invalidate before using old ROIs |
| Atlas visible / covered / hidden / off-screen / minimized | Record each separately; never infer behavior from another case |
| Atlas updates but one source freezes | Per-source freshness must not inherit atlas freshness |
| EVE lowers background FPS | No dependence on motion alone; measure detection delay |
| GPU device loss / session fault | UNKNOWN and bounded recovery; other clients continue |
| Monitor HDR / SDR changes | Record pixel format, colour conversion and calibration validity |

For each source independently, record capture-frame identity/time, arrival time, CPU
readback completion, detector completion, displayed state and reason. Reusing a frame
must not increment its sequence. Document any timestamp conversion to the observer's
monotonic clock. Hash equality alone neither proves nor disproves source freshness.

## Cost and liveness

Measure baseline EVE cost without the observer, then each candidate with the same
scenario: process CPU, GPU engines, working set, GPU memory, texture dimensions,
ROI bytes copied to CPU, sampling latency p50/p95/p99, dropped frames and recovery
time. Check source-client frame-rate/latency impact separately. Do not describe
small ROI readback as small full capture cost: per-window GPU frame pools still exist.

Use bounded latest-frame storage, not an unbounded queue. A timeout that only stops
awaiting a stuck backend does not cancel native work; document cancellation, resource
ownership and how a hung session is quarantined. WPF must receive small state updates,
not execute capture/CV. Selection requires both correctness and measured resource cost.

## Decision record

Attach raw measurements and reproducible settings to a new ADR. Mark each matrix row
pass/fail/unverified. Live thumbnail display by itself is insufficient evidence for
an atlas pixel pipeline. No performance figures are asserted until this protocol runs.
