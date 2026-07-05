# Feel & Level pass — pre-brief (notes only, no plan yet)

**Status:** NOTES. Captured 2026-07-04 from Sebastien after the M2 plan was written ("the feel
is not super right right now — likely easy to fix"). This becomes its own brainstorm → spec →
plan workflow **after the engine-base series (Plans 4–5 / M3–M4)**, and it is **gated on
Sebastien delivering a level layout/structure design first** — do not spec or plan this from
these notes alone.

## Scope items (Sebastien's list, mapped to today's code)

1. **A new level purpose-built for the gameplay.** Sebastien designs the layout/structure;
   the spec is written against that document. Implementation follows the established path
   (`Level` + `LevelConfigurationAsset` + `SpawnerService` + swarm block — `neon-recipes`
   Recipe 3; no legacy scene rebuilds).

2. **Better/improved QTE + feedback UI.** The M2 finish-challenge prompt is deliberately
   minimal — text like `KICK 1/2` + "FINISH!" in `UIHUDFinishPrompt`. Wants real verb glyphs,
   progress presentation, and juice. (Overlaps M4's HUD-polish scope — decide at M4 planning
   time what lands there vs. here.)

3. **Swap placeholder sprites.** Chaff proxies and the ambient material both still use
   `HitEffect.png` (`SwarmRenderRig._chaffSprite`, `SwarmAmbient.mat` Base Map). Real art pass.

4. **Chaff lane spawning/holding must go.** Today `SwarmSpawnSystem` floods from the two belt
   ends into 3 fixed lanes and `SwarmSteeringSystem` holds each agent to its lane-Y — they
   spawn in lines, stay in lines. Sebastien dislikes it and it will not fit the new level.

5. **Smart grouping once lanes are gone.** With lane-hold removed, chaff need real crowd
   behavior (separation/flocking — the "seek+separation" the spec always wanted; M1 shipped
   lane-Y + per-entity jitter as a documented deviation, and the M1 gate already flagged the
   blob). This is Layer-1 internal: the bridge isolates it, nothing above the swarm moves.

6. **Lanes move to the AMBIENT layer.** Ambient agents should align on lanes that follow the
   walkways/streets of the new level — i.e. lane geometry becomes level data (an evolution of
   the `SwarmDensityBlock` belt rect into level-authored paths), consumed by ambient placement
   instead of chaff steering.

## Inputs required before spec-writing

- Sebastien's level layout/structure design doc (to come — the trigger for this workflow).
- M2–M4 gate records (feel flags accumulate there; fold them in).
- Re-read `SwarmSpawnSystem` / `SwarmSteeringSystem` / `SwarmRenderRig` as they stand then —
  M3/M4 may have touched them.

## Slotting

Default: its own workflow after Plan 5 (M4). M4's existing "chaff separation feel pass" and
HUD-polish items overlap items 2/5 — at M4 planning time, either pull those forward into M4
or explicitly defer them here, but don't do them twice.
