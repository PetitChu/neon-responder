---
name: neon-combat
description: "Neon Responder combat — the engine-agnostic verb model (punch/kick/grab/throw/jump/weapon), the custom UnitStateMachine, and MonoBehaviour-now vs DOTS-target status. Use when working on combat, unit states, or the Act-verb design in Neon Responder."
---

# Neon Responder — Combat

Side-scroll beat-'em-up. PC / Steam, Unity 6, single-player, **NOT VR** — do not apply Quest/VR framing. Namespace throughout: `BrainlessLabs.Neon`. Combat code lives under `Assets/_neon/Scripts/Units/`.

## Engine-status banner

**Combat DESIGN is engine-agnostic.** Keep it that way — describe verbs and rules in terms of player intent, never in terms of MonoBehaviour vs ECS.

| Layer | Status |
| --- | --- |
| MonoBehaviour FSM brawler (`UnitStateMachine` + `Player*`/`Enemy*`/`Unit*` states) | **`[BUILT]`** — this is the combat that runs today. |
| DOTS/ECS **combat** logic | **`[IN-MIGRATION → DOTS]`** — a design-level target, **not implemented**. No ECS combat systems exist. |
| DOTS entity/spawn layer (`EntitiesService`, `SpawnerService`, `Level`) | Exists and is merged, but carries **NO combat logic** — it is a registry + spawner only (replaced the old static `EnemyManager`). |

When asked to "add DOTS combat," treat it as greenfield against the engine-agnostic design; do not claim any ECS combat exists to extend. Route MonoBehaviour-vs-ECS integration questions to the parallel DOTS track.

## The verb model / "Act" dispatcher

GDD v0.4 recasts combat as a single contextual **"Act" button** that dispatches to one of a set of verbs by context + a priority table. The **individual verb-states already exist as MonoBehaviour states `[BUILT]`**; the single "Act" button + dispatch priority table is **`[GDD-TARGET · not built]`**.

Verbs (each maps to a real state class today): **punch**, **kick**, **grab**, **throw-enemy**, **jump-attack** (jump-punch / jump-kick), **weapon-attack**, **weapon-throw** *(pickup exists; a dedicated throw-weapon state does not yet)*, **pickup**.

Today, dispatch is **not** unified: `PlayerIdle` / `PlayerMove` each contain a hand-ordered `if`-cascade over separate input keys (Punch / Kick / Grab / Jump / Defend). The "Act" work is to collapse those cascades into one contextual button with an explicit priority table. GDD pointer: Notion "Neon_Responder_GDD" v0.4 → the "Act" contextual verb dispatcher (which replaced v0.3's Takedown QTE).

## The custom `UnitStateMachine`

Files: `Scripts/StateMachine/UnitState.cs`, `Scripts/StateMachine/UnitStateMachine.cs`.

- **Distinct from the app-lifecycle UnityHFSM** (different assembly). This is a hand-rolled combat FSM; do not conflate the two.
- `UnitStateMachine : UnitActions` (MonoBehaviour). Holds one current `UnitState _unitState`. `Start()` seeds `new PlayerIdle()` if `isPlayer`, else `new EnemyIdle()` if `isEnemy`.
- `SetState(UnitState)` is the only transition primitive: calls `Exit()` on the old state, assigns the new state's `unit = this`, stamps `stateStartTime = Time.time`, then calls `Enter()`. States trigger their own transitions by calling `unit.UnitStateMachine.SetState(new SomeState())`.
- The machine pumps `Update` / `LateUpdate` / `FixedUpdate` into the current state each frame. Optional `showStateInGame` renders the current state name over the unit for debugging.
- `UnitState` (abstract base): fields `unit` (a `UnitActions`) and `stateStartTime`; virtuals `Enter/Exit/Update/LateUpdate/FixedUpdate`; `virtual bool canGrab => true` (states set it `false` to make a unit un-grabbable, e.g. while grabbing, knocked down, standing up).
- Common patterns: play an animation in `Enter()`; end a timed state via `Time.time - stateStartTime > unit.GetAnimDuration(animName)`; poll input in `Update()`; do movement/physics in `FixedUpdate()`.

## Player states catalog

Directory: `Scripts/Units/PlayerStates/`. Every class below is a real file there.

| State class | What it does | GDD verb role |
| --- | --- | --- |
| `PlayerIdle` | Standing. Polls all inputs and dispatches to every other action (the current de-facto "Act" cascade). Falls through to `PlayerMove` on input vector. | dispatcher (today) |
| `PlayerMove` | Ground movement; depth-scaled Y, wall detection; same input cascade as Idle. | move |
| `PlayerAttack(ATTACKTYPE)` | Combo engine for **punch/kick**. Matches `unit.attackList` against `settings.comboData`, honours `comboResetTime`, chains follow-ups, can hand off to `PlayerWeaponAttack`. | punch / kick |
| `PlayerJump` | Jump arc (`JumpSequence`); can branch to jump-attacks mid-air; lands into `PlayerLand`. | jump |
| `PlayerJumpAttack(AttackData)` | Air attack (jump-punch/jump-kick data from settings); checks hit; lands into `PlayerLand`. | jump-attack |
| `PlayerLand` | Recovery after landing; footstep SFX; → `PlayerIdle`. | (jump recovery) |
| `PlayerTryGrab` | Grab attempt. Picks up nearby pickup (→ `PlayerGrabItem`) or, if hitbox catches a grabbable enemy, → `PlayerGrabEnemy`. `canGrab => false`. | grab / pickup |
| `PlayerGrabEnemy(GameObject)` | Holds a grabbed enemy; punch/kick → `PlayerGrabAttack`, grab → `PlayerThrowEnemy`, timeout → Idle. Releases enemy on exit. | grab (hold) |
| `PlayerGrabAttack(AttackData)` | Attack executed on a held enemy (grab-punch / grab-kick). | grab-attack |
| `PlayerThrowEnemy(GameObject)` | Throws held enemy: repositions/turns it and puts it into `UnitKnockDown` using throw distance/height. | throw-enemy |
| `PlayerGrabItem(GameObject)` | Pickup animation; fires `Item.OnPickUpItem`; → Idle. | pickup |
| `PlayerGroundPunch` | Punch on a downed nearby enemy (`settings.groundPunch`). | punch (ground) |
| `PlayerGroundKick` | Kick on a downed nearby enemy (`settings.groundKick`). | kick (ground) |
| `PlayerWeaponAttack` | Attack with equipped weapon; uses `weapon.attackData`; handles `timesToUse` depletion & weapon destruction. | weapon-attack |
| `PlayerInActive` | Controls fully disabled (idle anim, no input). Used to lock the player out. | (control lock) |

Shared `Unit*` states (also in `PlayerStates/`, used by **both** players and enemies — `Enter/Update` branch on `unit.isPlayer`):

| State class | What it does |
| --- | --- |
| `UnitHit` | Hit-reaction stagger; may drop weapon (`loseWeaponWhenHit`); → Idle (player or enemy) when anim ends unless dead. |
| `UnitKnockDown(AttackData, xForce, yForce)` | Airborne knockback with bounces; optional fall damage on `GRABTHROW`; can hit other enemies while flying; → `UnitKnockDownGrounded`. `canGrab => false`. |
| `UnitKnockDownGrounded` | Lying on the floor; → `UnitDeath(false)` if no HP, else → `UnitStandUp` after `knockDownFloorTime`. `canGrab => false`. |
| `UnitStandUp` | Get-up animation; → Idle (player or enemy). `canGrab => false`. |
| `UnitDeath(bool showDeathAnim)` | Death: settle on floor, stop; player death calls `Entities.DisableAllEnemyAI()`; enemy flickers & self-destructs. |
| `UnitDefend` | Block. Player holds Defend key (optional turn-in-place); enemy blocks for `defendDuration`. `Hit()` shows block FX/SFX. |
| `UnitDropWeapon` | Deliberately drop the equipped weapon at mid-animation; → `PlayerIdle`. |

## Enemy states catalog

Directory: `Scripts/Units/EnemyStates/`. Every class below is a real file there.

| State class | What it does |
| --- | --- |
| `EnemyIdle` | Default resting state; acquires/keeps a player `target` (`findClosestPlayer`), turns to it if spotted. The FSM's "home" state and the only state `EnemyBehaviour` issues decisions from. |
| `EnemyWait(float)` | Passive wait for a duration, facing target; → `EnemyIdle`. |
| `EnemyMoveTo(Vector2)` | Walk to a world point with wall-detection; → `EnemyIdle` on arrival/wall. |
| `EnemyMoveToTargetAndAttack(AttackData)` | Approach the target to attack range, pause, then → `EnemyAttack`. Core aggression state. |
| `EnemyKeepDistance(xMin,xMax,yMin,yMax)` | Pick a randomized standoff point relative to target and delegate to `EnemyMoveTo` (spacing/reposition behaviour). |
| `EnemyAttack(AttackData)` | Execute one attack (skips if target dead); check hit; → `EnemyIdle`. |
| `EnemyGrabbed(GameObject player, Vector2 grabPos)` | Held by the player: snap to grab position, face away from grabber, wait out `grabDuration`. `canGrab => false`. |

**Targeting & AI gate — `Scripts/Units/EnemyBehaviour.cs`** (MonoBehaviour, separate from the FSM): after `delayBeforeStart`, watches for `targetInSight()` to set `targetSpotted`, then on `decisionInterval` runs `DoSomething()`. **`DoSomething()` is hard-gated by `public bool AI_Active` — if `AI_Active` is false, the enemy makes no decisions and just stands in `EnemyIdle`.** Decisions only fire when the unit is currently in `EnemyIdle`. It weights attacking by `_entities.GetEnemyAttackerCount()` (≈75% attack when no one is on the player, ≈25% when ≤2 are), else repositions via `EnemyKeepDistance`. Random attacks come from `settings.enemyAttackList`.

> **Spawn/AI gap:** spawned enemies whose `AI_Active` is never turned on stay inert. See **neon-troubleshooting** for the spawn-gate issue (SpawnerService/EntitiesService not enabling `AI_Active`).

## Supporting types

All in `Scripts/Units/` unless noted.

- **`HealthSystem`** — HP, `SubstractHealth`/`AddHealth`, `isDead`, hit-flash/shake FX, health bars; fires static `onHealthChange` / `onUnitDeath` events. Enemy list ownership moved to `EntitiesService` (not here).
- **`UnitActions`** — MonoBehaviour base for `UnitStateMachine`; shared combat helpers used by states: `CheckForHit`/`GetObjectsHit`/`HitBoxActive`, `NearbyEnemyDown`, `MoveToVector`/`StopMoving`/`JumpSequence`, `GetAnimDuration`, `findClosestPlayer`/`targetInSight`, turn helpers, and DI accessors (`InputService`, `AudioService`, `Entities`) injected via **VContainer** (`[Inject] IInputService/IAudioService/IEntitiesService`).
- **`UnitDefinitionAsset`** — `ScriptableObject` (menu `Neon/Units/Unit Definition`) describing a unit type for the spawner: id, display name, `UNITTYPE`, prefab, portrait, max health. Data only.
- **`UnitSettings`** — big per-unit config MonoBehaviour: movement/jump, `comboData` + `comboResetTime`, per-verb `AttackData` (jump/grab/ground punch+kick, grabThrow), `enemyAttackList`, knockdown/throw/defend/grab tuning, FOV, name/portrait.
- **`WeaponAttachment`** — equips/drops/destroys a `WeaponPickup` on a unit; sprite parenting, sorting, and `LoseCurrentWeapon` on hit/knockdown.
- **`AttackData`** — `[Serializable]` attack descriptor: `damage`, `animationState`, `sfx`, `ATTACKTYPE` (`NONE/PUNCH/KICK/GROUNDPOUND/GRAB/GRABPUNCH/GRABKICK/GRABTHROW/WEAPON`), `knockdown`, `inflictor`. `Combo` = named `List<AttackData>` sequence (only PUNCH/KICK usable in combos).

## Momentum / Finish-Ready

**`[GDD-TARGET · not built]`** — these do not exist in code today; do not describe them as implemented.

Design intent (GDD v0.4 core loop): *auto-engage → **Finish-Ready** → contextual "Act" verb → **Momentum** up → build fires harder / earns faster → more Finish-Ready.* Finish-Ready is a per-enemy "ready to be finished" flag surfaced to the player; Momentum is a multiplier tier (Cool → Warm → Hot → Overdrive) driven by finishing hits. GDD pointer: Notion "Neon_Responder_GDD" v0.4 → Finish-Ready state and the Momentum multiplier. Build these against the engine-agnostic design, not as extensions of any existing ECS system.
