# Real EVE image fixtures — not collected yet

No EVE recognition claim can be made from the measurement replay or generated shapes.
Keep this directory empty of pretend EVE fixtures. Add only supplied/captured, labelled
frames suitable for redistribution in this repository.

For one shared UI profile collect:

| Area | Required examples |
|---|---|
| Mining hold | Empty, middle, below/above 90%, near-full, full, after unloading |
| Harvester 1 and 2 | Active at several cycle phases, inactive, deactivating, ambiguous |
| UI presence | Correct mining-hold tab, wrong inventory tab, inventory closed, obscured bar |
| Quality failures | Black/blank frame, clipped ROI, wrong geometry, tooltip covering ring |
| Background behavior | Same client foreground and fully covered, low background FPS, frozen source |

Supply full client-area PNGs for geometry calibration and small lossless ROI crops for
regression. Include a short timestamped sequence of ring phases, not just one active
icon. The detector must distinguish positive inactive evidence from absence of a ring.

Record alongside each sequence: client-area resolution, EVE UI scale, Windows DPI,
Windows build, EVE UI theme, capture backend, source-frame timestamps, visible/covered
status and expected hold/H1/H2 values. Annotate uncertain labels as UNKNOWN.
Real character names need not appear in fixture filenames.

Choose parameters using a calibration subset; evaluate false-GREEN and UNKNOWN rates
on separate held-out frames. Synthetic fixtures can later test pixel arithmetic but
must be labelled synthetic and cannot replace this acceptance set.
