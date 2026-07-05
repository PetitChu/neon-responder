# Card Catalog — *Vampire Survivors* & *Brotato* (base game)

**Reference doc · v0.1** · *companion to [`case-study-vs-brotato-v0.1.md`](case-study-vs-brotato-v0.1.md)
(analysis lives there; this file is the raw catalog).*

**Scope:** **base game only — all paid DLC excluded.** VS excludes Legacy of the Moonspell, Tides of the
Foscari, Emergency Meeting, Ode to Castlevania, Operation Guns/Contra, Those Who Remain. Brotato excludes the
Abyssal Terrors DLC. "Base game" *includes* free content updates.

**Source & confidence note:** compiled from the games' wikis. The official **Fandom** (`vampire-survivors.fandom.com`)
and **wiki.gg** (`brotato.wiki.gg`) endpoints were **hard-blocked** (HTTP 401/402) at compile time, so data was
read from current community MediaWiki mirrors (`vampire.survivors.wiki`, `brotato.wiki.spellsandguns.com`).
The mirrors are internally consistent and appear current, but **treat exact effect numbers as illustrative and
re-verify against the live official wiki before using any value to tune.** Known gaps are flagged inline (§B0,
§B4). Effect text is condensed to one line per entry.

---

# Part A — Vampire Survivors

## A0. Counts
- **43** base weapons (§A1)
- **34** evolutions + **3** unions + **2** gift-evolutions = **39** evolved forms (§A2)
- **23** passive items (§A3)
- **22** Arcanas (§A4)

## A1. Base weapons (43)
"Start:" = the character the weapon starts with, where the wiki notes it.

| Weapon | Starting effect | Evolves / Unions into | Requirement |
|---|---|---|---|
| Whip | Attacks horizontally, passes through enemies. (Start: Antonio) | Bloody Tear | Hollow Heart |
| Magic Wand | Fires at the nearest enemy. (Start: Imelda) | Holy Wand | Empty Tome |
| Knife | Fires quickly in the faced direction. (Start: Gennaro) | Thousand Edge | Bracer |
| Axe | High damage, high Area scaling. (Start: Lama) | Death Spiral | Candelabrador |
| Cross | Aims at nearest enemy, boomerang effect. (Start: Krochi) | Heaven Sword | Clover |
| King Bible | Orbits around the character. (Start: Dommario) | Unholy Vespers | Spellbinder |
| Fire Wand | Fires at a random enemy, heavy damage. (Start: Arca) | Hellfire | Spinach |
| Garlic | Damages nearby enemies; lowers their resistances. (Start: Poe) | Soul Eater | Pummarola |
| Santa Water | Generates damaging zones. (Start: Clerici) | La Borra | Attractorb |
| Runetracer | Passes through enemies, bounces around. (Start: Pasqualina) | NO FUTURE | Armor |
| Lightning Ring | Strikes at random enemies. (Start: Porta) | Thunder Loop | Duplicator |
| Pentagram | Erases everything in sight. (Start: Christine) | Gorgeous Moon | Crown |
| Peachone | Bombards in a circling zone. (Start: Toastie) | Vandalier (union) | + Ebony Wings |
| Ebony Wings | Bombards in a circling zone. (Start: Exdash) | Vandalier (union) | + Peachone |
| Phiera Der Tuphello | Fires quickly in four fixed directions. (Start: Pugnala) | Phieraggi (union) | + Eight The Sparrow + Tirajisú |
| Eight The Sparrow | Fires quickly in four fixed directions. (Start: Pugnala) | Phieraggi (union) | + Phiera Der Tuphello + Tirajisú |
| Gatti Amari | Summons capricious projectiles; interacts with pickups. (Start: Giovanna) | Vicious Hunger | Stone Mask |
| Song of Mana | Attacks vertically, passes through enemies. (Start: Poppea) | Mannajja | Skull O'Maniac |
| Shadow Pinion | Damaging zones when moving, strikes when stopping. (Start: Concetta) | Valkyrie Turner | Wings |
| Clock Lancet | Chance to freeze enemies in time. (Start: Gallo) | Infinite Corridor | Silver Ring + Gold Ring (max) |
| Laurel | Shields from damage when active. (Start: Divano) | Crimson Shroud | Metaglio Left + Metaglio Right (max) |
| Vento Sacro | Stronger with continuous movement; can crit. (Start: Zi'Assunta) | Fuwalafuwaloo (union) | + Bloody Tear |
| Bone | Throws a bouncing projectile. (Start: Mortaccio) | Anima of Mortaccio | Chaos Malachite relic + Lv80 Mortaccio |
| Cherry Bomb | Throws a bouncing projectile; sometimes explodes. (Start: Yatta Cavallo) | Yatta Daikarin | Chaos Rosalia relic + Lv80 Yatta Cavallo |
| Carréllo | Bouncing projectile; bounces scale with Amount. (Start: Bianca Ramba) | Carozza! | Chaos Lazulia relic + Lv80 Bianca Ramba |
| Celestial Dusting | Bouncing projectile; cooldown drops when moving. (Start: O'Sole Meeo) | Profusione D'Amore | Chaos Altemanna relic + Lv80 O'Sole Meeo |
| La Robba | Generates bouncing projectiles. (Start: Sir Ambrojoe) | — | — |
| Greatest Jubilee | Chance to summon light sources. | — | — |
| Bracelet | Fires three projectiles at a random enemy. (Start: Gyorunton) | Bi-Bracelet → Tri-Bracelet | Level up (no passive) |
| Candybox | Lets you choose any unlocked base weapon. (Start: Big Trouser) | Super Candybox II Turbo (gift) | — |
| Victory Sword | Combo strike at nearest enemy; retaliates. (Start: Queen Sigma) | Sole Solution (gift) | + Torrona's Box (max) |
| Flames of Misspell | Emits cones of flames. (Start: Avatar Infernas) | Ashes of Muspell | Torrona's Box (max) |
| Pako Battiliar | May retaliate when losing health. (Start: Bat Robbert) | Mazo Familiar | Hollow Heart (max) |
| Ammo Appalate | Aims in faced direction; stockpiles when enemies out of range. (Start: Zi'Appunta) | Gunastrophe | Bracer (max) |
| Unearthly Bolt | Crits create damaging zones; Revivals boost crit chains. (Start: Big Troubler) | Spirit Disturbance | Tirajisú (max) |
| Glass Fandango | Stronger w/ movement, Orologions & vs frozen enemies. (Start: She-Moon Eeta) | Celestial Voulge | Wings (max) |
| Penshin Fatcha | Aquatic armaments that evolve endlessly (tuna chain). (Start: Para Kooleo) | Miracle of Multiplication | Evolve the tuna chain 6+ times |
| Santa Javelin | Duration affects Amount; can crit. (Start: Santa Ladonna) | Seraphic Cry | Clover (max) |
| Gaze of Gaea | May Defang enemies. (Start: Gazebo) | Embrace of Gaea | Parm Aegis |
| Magi-Stone | Fixed damage based on Weapon Level. (Start: Chula-Reh) | Kyra-Stones | Karoma's Mana |
| Phas3r | Generates thin damaging zones; high Amount scaling. (Start: Space Dude) | Photonstorm | Empty Tome (max) |
| Chaos Rune | Speed & Duration affect number of hit boxes. | Wicked Ruler | Spellbinder (max) |
| Arma Dio | Lets you choose an extra passive weapon. | — | — |

## A2. Evolved weapons

### Evolutions — weapon (maxed) + passive → result (34)
| Result | = Base weapon | + Passive | Evolved effect |
|---|---|---|---|
| Bloody Tear | Whip | Hollow Heart | Can crit and heals on damage. |
| Holy Wand | Magic Wand | Empty Tome | Fires with no cooldown. |
| Thousand Edge | Knife | Bracer | Fires a continuous stream. |
| Death Spiral | Axe | Candelabrador | Harbinger scythes sweep in a spiral. |
| Heaven Sword | Cross | Clover | Boomerang blades that can crit. |
| Unholy Vespers | King Bible | Spellbinder | Never stops orbiting. |
| Hellfire | Fire Wand | Spinach | Piercing fireballs. |
| Soul Eater | Garlic | Pummarola | Heals per kill; aura stacks. |
| La Borra | Santa Water | Attractorb | Damaging puddles trail the player. |
| NO FUTURE | Runetracer | Armor | Retaliatory explosions on low HP. |
| Thunder Loop | Lightning Ring | Duplicator | Strikes twice per hit. |
| Gorgeous Moon | Pentagram | Crown | Screen-wipe that collects XP and boosts Growth. |
| Vicious Hunger | Gatti Amari | Stone Mask | Cats turn pickups to gold, deal huge damage. |
| Mannajja | Song of Mana | Skull O'Maniac | Vertical waves that slow enemies. |
| Valkyrie Turner | Shadow Pinion | Wings | Bigger damaging zones + strikes. |
| Infinite Corridor | Clock Lancet | Silver + Gold Ring (max) | Periodically halves all enemy HP. |
| Crimson Shroud | Laurel | Metaglio Left + Right (max) | Caps incoming damage at 10, retaliates. |
| Bi-Bracelet | Bracelet | (level up) | Fires more projectiles. |
| Tri-Bracelet | Bi-Bracelet | (level up) | Fires even more projectiles. |
| Ashes of Muspell | Flames of Misspell | Torrona's Box (max) | Escalating flame cones. |
| Anima of Mortaccio | Bone | Chaos Malachite + Lv80 Mortaccio | Multiple bouncing bones. |
| Yatta Daikarin | Cherry Bomb | Chaos Rosalia + Lv80 Yatta Cavallo | Bigger, more frequent explosions. |
| Carozza! | Carréllo | Chaos Lazulia + Lv80 Bianca Ramba | Enhanced bouncing cart. |
| Profusione D'Amore | Celestial Dusting | Chaos Altemanna + Lv80 O'Sole Meeo | Enhanced healing bounce projectile. |
| Mazo Familiar | Pako Battiliar | Hollow Heart (max) | Stronger retaliation. |
| Gunastrophe | Ammo Appalate | Bracer (max) | Unloads the stockpile as a barrage. |
| Spirit Disturbance | Unearthly Bolt | Tirajisú (max) | Stronger crit zones scaling with Revivals. |
| Celestial Voulge | Glass Fandango | Wings (max) | Stronger movement/frozen-target scaling. |
| Miracle of Multiplication | Penshin Fatcha | Evolve tuna chain 6+ times | Endgame form of the tuna chain. |
| Seraphic Cry | Santa Javelin | Clover (max) | More javelins; higher crit. |
| Embrace of Gaea | Gaze of Gaea | Parm Aegis | Stronger Defang field. |
| Kyra-Stones | Magi-Stone | Karoma's Mana | Multiple fixed-damage stones. |
| Photonstorm | Phas3r | Empty Tome (max) | Dense grid of damaging beams. |
| Wicked Ruler | Chaos Rune | Spellbinder (max) | More hit boxes. |

*Tuna chain (Penshin Fatcha): Tonno Subito → Tonnado → Tonn'omoto → Tonn'oddeeo → Tonne → Unsurpassed → Miracle of Multiplication.*

### Unions — weapon (maxed) + weapon (maxed) → result (3)
| Result | = Weapon 1 | + Weapon 2 | Effect |
|---|---|---|---|
| Vandalier | Peachone | Ebony Wings | Merged twin-bird bombardment; frees a slot. |
| Phieraggi | Phiera Der Tuphello | Eight The Sparrow (+ Tirajisú) | Beam of projectiles; scales with Revivals. |
| Fuwalafuwaloo | Vento Sacro | Bloody Tear | Crits may trigger explosions. |

### Gift-evolutions (2)
| Result | From | Requirement | Effect |
|---|---|---|---|
| Super Candybox II Turbo | Candybox | Own ≥1 Candybox | Choose from a selection of advanced weapons. |
| Sole Solution | Victory Sword | + Torrona's Box (max) | Grows stronger the more enemies you defeat. |

## A3. Passive items (23)
| Passive | Effect | Feeds evolution(s) |
|---|---|---|
| Spinach | +10% Might (damage dealt). | Hellfire |
| Armor | −1 incoming damage; +10% retaliatory damage. | NO FUTURE |
| Hollow Heart | +20% max health. | Bloody Tear, Mazo Familiar |
| Pummarola | Recovers 0.2 HP/sec. | Soul Eater |
| Empty Tome | −8% weapon cooldown. | Holy Wand, Photonstorm |
| Candelabrador | +10% attack Area. | Death Spiral |
| Bracer | +10% projectile Speed. | Thousand Edge, Gunastrophe |
| Spellbinder | +10% effect Duration. | Unholy Vespers, Wicked Ruler |
| Duplicator | Weapons fire +1 projectile (Amount). | Thunder Loop |
| Wings | +10% movement speed. | Valkyrie Turner, Celestial Voulge |
| Attractorb | +pickup range (Magnet). | La Borra |
| Clover | +10% Luck. | Heaven Sword, Seraphic Cry |
| Crown | +8% experience (Growth). | Gorgeous Moon |
| Stone Mask | +10% coins (Greed). | Vicious Hunger |
| Skull O'Maniac | +10% Curse. | Mannajja |
| Tirajisú | +1 Revival (revive at 50% HP). | Spirit Disturbance; Phieraggi union |
| Torrona's Box | Cursed; +4% Might/Proj.Speed/Duration/Area (raises Curse/Greed). | Ashes of Muspell, Sole Solution |
| Silver Ring | Worn with Clock Lancet (+Duration/Area). | Infinite Corridor |
| Gold Ring | Worn with Clock Lancet (+Curse). | Infinite Corridor |
| Metaglio Left | Dark power that protects the bearer. | Crimson Shroud |
| Metaglio Right | Dark power that curses the bearer. | Crimson Shroud |
| Parm Aegis | +invulnerability time after taking damage. | Embrace of Gaea |
| Karoma's Mana | +enemy spawns; +Gold Fever/Finger duration. | Kyra-Stones |

## A4. Arcanas (22)
Global run-modifier tarots (numbered 0–XXI). Not DLC.

| # | Name | Effect |
|---|---|---|
| 0 | Game Killer | Halts XP gain; XP gems become exploding projectiles; chests hold ≥3 items. |
| I | Gemini | Listed weapons come with a counterpart. |
| II | Twilight Requiem | Listed weapon projectiles explode on expiry (scales w/ Curse). |
| III | Tragic Princess | Listed weapons' cooldown reduces while moving. |
| IV | Awake | +3 Revivals; each consumed Revival grants stat bonuses. |
| V | Chaos in the Dark Night | Projectile Speed oscillates ±50%; +1% Speed/level. |
| VI | Sarabande of Healing | Healing doubled; recovering HP damages nearby enemies equally. |
| VII | Iron Blue Will | Listed projectiles gain up to 3 bounces; may pass through enemies/walls. |
| VIII | Mad Groove | Every 2 min, pulls all stage items/pickups/lights to the player. |
| IX | Divine Bloodline | Armor boosts listed weapons & reflects damage; bonus dmg from missing HP. |
| X | Beginning | Listed weapons +1 Amount; main weapon & evolution +3 Amount. |
| XI | Waltz of Pearls | Listed weapon projectiles gain up to 3 bounces. |
| XII | Out of Bounds | Freezing enemies triggers explosions; Orologions easier to find. |
| XIII | Wicked Season | Growth/Luck/Greed/Curse doubled at intervals; +1% each per 2 levels. |
| XIV | Jail of Crystal | Listed weapon projectiles may freeze enemies. |
| XV | Disco of Gold | Coin-bags trigger Gold Fever; gold pickups restore HP. |
| XVI | Slash | Enables crits for listed weapons; doubles overall critical damage. |
| XVII | Lost & Found Painting | Duration oscillates ±50%; +1% Duration/level. |
| XVIII | Boogaloo of Illusions | Area oscillates ±25%; +1% Area/level. |
| XIX | Heart of Fire | Listed projectiles explode on impact; light sources & character explode when hit. |
| XX | Silent Old Sanctuary | +3 Reroll/Skip/Banish; each empty weapon slot gives +20% Might, −8% Cooldown. |
| XXI | Blood Astronomia | Listed weapons emit special damaging zones affected by Amount & Magnet. |

## A5. Excluded / out of scope
- **Paid DLC (excluded):** Legacy of the Moonspell, Tides of the Foscari, Emergency Meeting, Ode to
  Castlevania, Operation Guns/Contra, Those Who Remain.
- **Darkanas (12, excluded):** an advanced/Inverse-mode arcana set (numbers 0, I, III, V, VI, VIII, X, XII,
  XIII, XVIII, XIX, XXI) separate from the standard 22. Flag if you want them enumerated.
- **Not tabled (base but not draftable-as-normal):** "Counterpart Weapons" (Gemini-arcana mirrors: Cygnus,
  Flock Destroyer, Speculo-series, …) and "Special Weapons" (temporary/arcana-granted pickups: Nduja Fritta
  Tanto, Sorbetto, Game Killer, …). Flag if you want these added.

---

# Part B — Brotato

## B0. Counts & caveats
- **Weapons:** **61** across **15** classes (§B1)
- **Items:** **199** enumerated (§B2) — the mirror's intro cites **201** vanilla items, so this list is likely
  **short by ~2** items that didn't render in extraction. Flagged.
- **Characters:** **48** (§B3)
- **Weapon count caveat:** one search snippet cited "64 base weapons," but no additional base weapon names
  could be confirmed on the wiki (a bad extraction pass hallucinated fake names, which were discarded). Treat
  **61** as wiki-verified.
- **Tiers:** Legendary-class weapons are Tier IV only; others are generally Tier I–IV. Exact per-weapon minimum
  tiers (which start at II/III) were **not** individually verified.

## B1. Weapons by class (61)

Master list (each weapon once):

| Weapon | Class(es) | Tier(s) | One-line effect / signature |
|---|---|---|---|
| Cacti Club | Primitive, Heavy | I–IV | Heavy melee; spawns projectiles on hit |
| Chain Gun | Gun, Legendary | IV | Extreme fire rate + pierce, damage penalty |
| Chopper | Blade | I–IV | Scales with Max HP; heals from consumables |
| Circular Saw | Blade, Medical | I–IV | High life-steal scaling |
| Claw | Precise, Unarmed | I–IV | Fast; scales with Attack Speed |
| Crossbow | Precise, Medieval | I–IV | Pierces on crit; Range scaling |
| DEX-troyer | Explosive, Legendary | IV | Spawns bouncing lightning projectiles |
| Double Barrel Shotgun | Gun | I–IV | Multi-projectile spread, pierce penalty |
| Drill | Precise, Legendary | IV | 100% crit + material gain; Attack Speed scaling |
| Excalibur | Blade, Legendary | IV | Max HP scaling; armor penalty per weapon |
| Fireball | Elemental, Explosive | II–IV | Explodes on hit; burning |
| Fist | Unarmed | I–IV | Basic high-damage unarmed |
| Flamethrower | Elemental, Heavy | II–IV | Low-damage high-rate burning, pierces |
| Flaming Brass Knuckles | Elemental, Unarmed | I–IV | Burning melee |
| Gatling Laser | Heavy, Gun, Legendary | IV | Extreme DPS piercing |
| Ghost Axe | Ethereal | I–IV | Gains damage per kill in wave |
| Ghost Flint | Ethereal | I–IV | Gains Attack Speed per kill in wave |
| Ghost Scepter | Ethereal | I–IV | Ranged; gains Max HP per kill in wave |
| Hammer | Blunt, Heavy | II–IV | High Melee Damage; knockback |
| Hand | Support, Unarmed | I–IV | Harvesting bonus |
| Hatchet | Primitive | I–IV | Scales Melee Damage & Attack Speed |
| Icicle | Elemental, Precise | I–IV | Elemental projectile; slows |
| Jousting Lance | Medieval | I–IV | Speed-scaling; penalty when stationary |
| Knife | Precise | I–IV | High crit; scales Melee Damage |
| Laser Gun | Gun | I–IV | Piercing energy shots |
| Lightning Shiv | Elemental, Precise | I–IV | Spawns lightning projectiles |
| Medical Gun | Gun, Medical | I–IV | Healing/life-steal ranged |
| Minigun | Gun, Heavy | II–IV | Very high fire rate |
| Nuclear Launcher | Explosive, Heavy | II–IV | Large explosive AoE |
| Obliterator | Gun, Heavy | higher | Heavy high-damage shots |
| Particle Accelerator | Elemental, Heavy | higher | Elemental beam |
| Pistol | Gun | I–IV | Basic ranged |
| Plank | Elemental, Explosive | I–IV | Explosion chance on melee |
| Plasma Sledge | Elemental, Explosive | III–IV | High elemental/explosive scaling |
| Potato Thrower | Support | I–IV | Ranged; Harvesting synergy |
| Power Fist | Explosive, Unarmed | III–IV | Explosion on hit |
| Pruner | Support | I–IV | Spawns fruit garden |
| Quarterstaff | Primitive, Medieval | I–IV | Scales Level & Melee; XP bonus |
| Revolver | Gun | I–IV | High-damage slow gun |
| Rock | Blunt, Primitive | I–IV | Basic; armor scaling |
| Rocket Launcher | Explosive, Heavy | II–IV | Explosive AoE |
| Scissors | Precise, Medical | I–IV | Life steal + crit |
| Screwdriver | Tool | I–IV | Spawns landmines |
| Scythe | Ethereal, Legendary | IV | 100% life steal; self-damage |
| Sharp Tooth | Primitive, Precise | I–IV | Life-steal scaling; low-HP bonus |
| Shredder | Gun, Explosive | I–IV | Rapid spread projectiles |
| Shuriken | Precise | I–IV | Thrown; pierces |
| Slingshot | Primitive | I–IV | Cheap primitive ranged |
| SMG | Gun | I–IV | Rapid low-damage |
| Sniper Gun | Gun, Precise | II–IV | Long-range high-crit |
| Spear | Primitive | I–IV | Long-range thrust; high range |
| Spiky Shield | Blunt, Medieval | I–IV | Armor-scaling shield |
| Stick | Primitive | I–IV | Damage per additional Stick owned |
| Sword | Blade, Medieval | I–IV | Alternates thrust/sweep |
| Taser | Elemental, Support | I–IV | Chains lightning |
| Thief Dagger | Precise | I–IV | Material gain on crit kills |
| Thunder Sword | Blade, Elemental | I–IV | Spawns slowing projectiles on hit |
| Torch | Primitive, Elemental | I–IV | Burning with spread |
| Vorpal Sword | Blade, Medieval | I–IV | Chance to one-shot on hit |
| Wand | Elemental | I–IV | Elemental projectile |
| Wrench | Tool | I–IV | Spawns turrets |

### Classes & set bonuses (scale at 2 / 3 / 4 / 5 / 6 owned)
| Class | Base members | Set bonus |
|---|---|---|
| Blade | Chopper, Circular Saw, Sword, Vorpal Sword, Thunder Sword, Excalibur | +1→+5 Melee Damage & 1%→5% Life Steal |
| Blunt | Rock, Spiky Shield, Hammer | +Armor & +Max HP, −Speed (to +3/+6/−10%) |
| Elemental | Icicle, Lightning Shiv, Plank, Taser, Torch, Wand, Fireball, Flamethrower, Flaming Brass Knuckles, Particle Accelerator, Plasma Sledge, Thunder Sword | +1→+5 Elemental Damage |
| Ethereal | Ghost Axe, Ghost Flint, Ghost Scepter, Scythe | +6%→+30% Dodge, −1→−5 Armor |
| Explosive | Plank, Shredder, Fireball, Rocket Launcher, Nuclear Launcher, Plasma Sledge, Power Fist, DEX-troyer | +5%→+25% Explosion Size |
| Gun | Double Barrel Shotgun, Laser Gun, Medical Gun, Pistol, Revolver, Shredder, SMG, Minigun, Sniper Gun, Obliterator, Chain Gun | +10→+50 Range |
| Heavy | Cacti Club, Flamethrower, Hammer, Rocket Launcher, Minigun, Nuclear Launcher, Obliterator, Particle Accelerator, Gatling Laser | +5%→+25% Damage |
| Legendary | Chain Gun, DEX-troyer, Drill, Excalibur, Gatling Laser, Scythe | −20→−100 Max HP (downside) |
| Medical | Medical Gun, Scissors, Circular Saw | +1→+5 HP Regeneration |
| Medieval | Crossbow, Jousting Lance, Quarterstaff, Spiky Shield, Sword, Vorpal Sword | +Armor & +Dodge (to +3/+6%) |
| Precise | Claw, Crossbow, Icicle, Knife, Lightning Shiv, Scissors, Sharp Tooth, Shuriken, Thief Dagger, Sniper Gun, Drill | +3%→+15% Crit Chance |
| Primitive | Cacti Club, Hatchet, Quarterstaff, Rock, Sharp Tooth, Slingshot, Spear, Stick, Torch | +3→+15 Max HP |
| Support | Hand, Pruner, Taser, Potato Thrower | +5→+25 Harvesting |
| Tool | Screwdriver, Wrench | +1→+5 Engineering |
| Unarmed | Claw, Fist, Hand, Flaming Brass Knuckles, Power Fist | +3%→+15% Dodge Chance |

## B2. Items (199)
Every base-game item, with tier and effect. (See §B0: list may be ~2 short of the wiki's stated 201.)

| Item | Tier | Effect |
|---|---|---|
| Acid | II | +8 Max HP, −2% Dodge, −2 Knockback |
| Adrenaline | III | +5% Dodge, 50% chance heal 5 HP on dodge |
| Alien Baby | III | +15 Max HP, +10% Enemy health |
| Alien Eyes | II | Shoots 6 alien eyes every 3s |
| Alien Magic | III | +8 Max HP, +3 HP Regen, −8 Luck |
| Alien Tongue | I | +30% pickup range, +1 Knockback |
| Alien Worm | I | +3 Max HP, +2 HP Regen, −1 HP from consumables |
| Alloy | III | +3 all damage types, +5% Crit, −6% Dodge |
| Anvil | IV | Upgrades a random shop weapon, or +2 Armor |
| Baby Elephant | I | 25% chance to deal damage when picking materials |
| Baby Gecko | I | +10 Range, +25% auto-attract materials |
| Baby with a Beard | III | Fires bullet from corpse on death, −50 Range |
| Bag | I | +15 materials from crates, −1% Speed |
| Bait | II | +8% Damage, special enemies appear next wave |
| Ball and Chain | III | +15% Damage, +3 Armor, +5 Knockback, −3% Speed |
| Bandana | III | Projectiles pierce +1, −10% Damage |
| Banner | II | +20 Range, +10% Attack Speed, −5 Knockback |
| Barricade | III | +3 Knockback, +8 Armor standing still, −5% Speed |
| Bat | I | +2% Life Steal, −2 Harvesting |
| Bean Teacher | III | +50% XP Gain, −2% Life Steal |
| Beanie | I | +4% Speed, −6 Range |
| Big Arms | IV | +12 Melee, +6 Ranged, +3 Knockback, −3% Attack Speed/Speed |
| Black Belt | II | +25% XP, +3 Melee, +3 Knockback, −8 Luck |
| Blindfold | II | +5% Crit, +5% Dodge, −15 Range |
| Blood Donation | III | +40 Harvesting, take 1 damage/second |
| Blood Leech | II | +2% Life Steal, +2 HP Regen, −3 Harvesting |
| Bloody Hand | IV | +10% Life Steal, +2% Damage per 1% Life Steal, take 1 dmg/s |
| Boiling Water | I | +2 Elemental, −1 Max HP |
| Book | I | +2 Engineering, +1 Elemental, −1 Luck |
| Bowler Hat | III | +15 Luck, +18 Harvesting, −5% Attack Speed, −3% Crit |
| Boxing Glove | I | +1 Melee, +3 Knockback |
| Broken Mouth | I | +5 Max HP, −1 HP Regen |
| Butterfly | I | +2% Life Steal, −1 Elemental |
| Cake | I | +3 Max HP, −1% Damage |
| Campfire | II | +2 Elemental, +2 HP Regen, −2% Speed |
| Candle | III | +4 Elemental, +1 HP Regen, −10% enemies, −5% Damage |
| Candy Bag | III | +8 random stat points/wave, 10% elite spawn chance |
| Cape | IV | +5% Life Steal, +20% Dodge, −2 all damage types |
| Celery Tea | II | +5% XP/wave, +50% XP next wave, +100% enemy health next wave |
| Chameleon | III | +3% Dodge, +20% Dodge standing still, −4% Damage |
| Charcoal | I | +1 Elemental, +2 Melee, −2 Harvesting |
| Claw Tree | I | +1 Melee, +3% Crit, −1 Max HP |
| Clockwork Wasp | II | +10% structure attack speed, +5% Speed |
| Clover | III | +20 Luck, +6% Dodge, −2% Life Steal |
| Coffee | I | +10% Attack Speed, −2% Damage |
| Cog | II | +4 Engineering, +1 Knockback, −4% Damage |
| Coil | III | +5 Knockback, +1% Damage per Knockback |
| Community Support | III | +1% Attack Speed per living enemy, −2 Armor |
| Compass | II | +5% Speed, +3 Engineering, −3% Crit |
| Coupon | I | −5% item prices |
| Crown | III | +8% additional Harvesting each wave |
| Cute Monkey | I | 8% chance heal 1 HP picking materials, −1 Ranged |
| Cyberball | II | 25% chance 1 damage to enemy on enemy death |
| Cyclops Worm | II | +12% Damage, −12 Range |
| Dangerous Bunny | II | +1 free shop reroll |
| Decomposing Flesh | II | +1% Life Steal/level, −1 Max HP/level |
| Defective Steroids | I | +2 Max HP, +2 Melee, −3% Attack Speed |
| Diploma | IV | +10 Engineering, +50% XP, −3 Max HP |
| Duct Tape | I | +1 Armor, +1 Engineering, −2 Max HP |
| Dynamite | I | +15% Explosion Damage |
| Energy Bracelet | II | +4% Crit, +2 Elemental, −2 Ranged |
| Esty's Couch | IV | +5 Max HP, +2 HP Regen per −1% Speed, −20% Speed |
| Exoskeleton | IV | +3 Armor, +5% Crit, +5 Engineering, +5% Speed, −2 HP Regen, −2% Life Steal |
| Explosive Shells | IV | +60% Explosion Damage, +15% size, −15% Damage |
| Explosive Turret | IV | Spawns turret with explosive bullets |
| Extra Stomach | IV | +1 Max HP when picking consumable at full health |
| Eyes Surgery | II | Burning activates 20% faster, +1 Elemental, −10 Range |
| Fairy | III | +1 HP Regen per Tier I item, −3 per Tier IV item |
| Fertilizer | I | +8 Harvesting, −1 Melee |
| Fin | III | +10% Speed, +3% Life Steal, −8 Luck |
| Focus | IV | +30% Damage, −3% Attack Speed per weapon |
| Fresh Meat | I | +2% Life Steal, −1 HP Regen |
| Fried Rice | III | +1 HP Regen per burning enemy |
| Frozen Heart | III | +8 Elemental, +5% Crit, weapon dmg scales w/ Elemental, burning 100% slower |
| Fruit Basket | II | Enemies drop fruits more often, −3 HP Regen |
| Fuel Tank | II | +4 Elemental, −1 Melee, −1 Ranged |
| Gambling Token | II | +8% Dodge, −1 Armor |
| Garden | II | Spawns garden creating fruit every 15s |
| Gentle Alien | I | +2 Max HP, +5% Damage, +5% Enemies |
| Ghost Outfit | III | Dodge capped 70%, +10% Dodge, −5% Speed, −3 Armor |
| Giant Belt | IV | Crits deal 10% of enemy current HP as bonus damage |
| Glass Cannon | III | +25% Damage, −3 Armor |
| Glasses | I | +20 Range |
| Gnome | IV | +10 Melee, +10 Elemental, −20 Range, −20% pickup range |
| Goat Skull | I | +3 Melee, −2% Crit |
| Gobbler's Hat | IV | +70% materials dropped, −15% Speed, −10% Dodge |
| Greek Fire | IV | Burning deals +10% enemy current HP as damage |
| Grind's Magical Leaf | IV | +3 Max HP, +1 HP Regen, +1% Life Steal (all at end of wave) |
| Gummy Berserker | I | +5% Attack Speed, +25 Range, −1 Armor |
| Handcuffs | III | +8 Melee/Ranged/Elemental, Max HP capped at current value |
| Head Injury | I | +6% Damage, −8 Range |
| Heavy Bullets | IV | +5 Ranged, +10% Damage, +10 Range, −5% Attack Speed, −5% Crit |
| Hedgehog | I | +2 Melee, +1 Ranged, −1 HP Regen |
| Helmet | I | +1 Armor, −2% Speed |
| Honey | III | +3 Ranged, +10% Explosion Damage, +5% Explosion Size, −3% Speed, −3% Dodge |
| Hourglass | IV | Decreases current wave count by 1; start next wave with 1 HP |
| Hunting Trophy | III | 33% chance +1 material on crit kill |
| Ice Cube | II | Enemies take 10% more damage 3s after first Elemental hit |
| Improved Tools | III | +10% Attack Speed, +structure attack speed |
| Incendiary Turret | II | Spawns turret shooting burning flames |
| Injection | I | +7% Damage, −2 Max HP |
| Insanity | I | +6% Crit, −3% Damage |
| Jelly | I | +1 Max HP per different weapon |
| Jet Pack | IV | +15% Speed, +10% Dodge, −5 Max HP, −1 Armor |
| Landmines | I | Landmine spawns every 12s dealing area damage |
| Laser Turret | III | Spawns turret shooting piercing bullets |
| Leather Vest | II | +2 Armor, +6% Dodge, −3 Max HP |
| Lemonade | I | +1 HP from consumables |
| Lens | I | +1 Ranged, −5 Range |
| Little Frog | II | +20% pickup range, +10 Harvesting, −3% Dodge |
| Little Muscley Dude | II | +3 Melee, +5 Max HP, −15 Range |
| Lost Duck | I | +8 Luck, −1 Elemental |
| Lucky Charm | III | +30 Luck, −2 Melee, −1 Ranged |
| Lucky Coin | IV | +2 Luck per 1% Crit, −2 Armor |
| Lure | II | +2 HP Regen, 2 additional loot aliens next wave |
| Lumberjack Shirt | I | Trees die in one hit |
| Mammoth | IV | +20 Melee, +5 HP Regen, +5 Knockback, −8% Damage, −3% Speed |
| Mastery | II | +6 Melee, −3 Ranged |
| Medal | II | +3 Max HP, +3% Damage, +1 Armor, +3% Speed, −4% Crit |
| Medical Turret | II | Spawns turret shooting healing bullets |
| Medikit | IV | +10 HP Regen, +2 HP Regen/5s until wave end, −10 Luck |
| Metal Detector | II | +5% chance to double material value, +6 Luck, +2 Engineering, −5% Damage |
| Metal Plate | II | +2 Armor, −3% Damage |
| Missile | II | +10% Damage, −4% Attack Speed |
| Mouse | III | +5% Life Steal, +10% Enemies, −5 Harvesting |
| Mushroom | I | +3 HP Regen, −2 Luck |
| Mutation | I | +1 Ranged, +1 Elemental, −3 Knockback |
| Nail | III | +5 Engineering, weapon dmg scales w/ 20% Engineering, −2 Ranged |
| Night Goggles | IV | +15% Crit, +50 Range, −3 Max HP, −1 Armor |
| Octopus | IV | +12 Max HP, +5 HP Regen, +3% Life Steal, −8% Crit |
| Padding | II | +3 Max HP, +1 Max HP per 80 Materials |
| Panda | IV | +12 Max HP, +25 Luck, −5% Damage |
| Peaceful Bee | I | +4% Dodge, +4 Harvesting, −1 Melee, −1 Ranged |
| Peacock | III | +25% XP, +100% XP next wave, +50% enemy damage next wave |
| Pencil | I | +1 Engineering |
| Piggy Bank | II | +20% of your materials at start of waves |
| Pile of Books | II | Structures can crit, +5% Crit, +3 Engineering |
| Plant | I | +3 HP Regen, −1% Life Steal |
| Plastic Explosive | III | +25% Explosion Size |
| Poisonous Tonic | III | +10% Attack Speed, +5% Crit, +15 Range, −2 HP Regen |
| Pocket Factory | II | +2 Engineering, killing a tree spawns a turret |
| Potato | IV | +3 Max HP, +2 HP Regen, +1% Life Steal, +5% Damage, +5% Attack Speed, +3% Speed, +3% Dodge, +1 Armor, +5 Luck |
| Power Generator | III | +1% Damage per 1% Speed, −5% Damage |
| Propeller Hat | I | +10 Luck, −2% Damage |
| Pumpkin | II | +15% Piercing Damage, −2% Damage |
| Recycling Machine | II | +35% more materials from recycling |
| Regeneration Potion | IV | HP Regen doubled below 50% HP, +3 HP Regen |
| Reinforced Steel | II | +2 Ranged, +3 Engineering, −3% Speed |
| Retromation's Hoodie | IV | +2% Attack Speed per 1% Dodge, −80 Range |
| Ricochet | IV | Projectiles +1 bounce, −25% Damage |
| Rip and Tear | III | Enemies 20% chance to explode on death, −5% Crit |
| Riposte | II | +2 Melee, 100% chance to damage enemy when dodging their attack |
| Ritual | II | +6% Damage, +2% Life Steal, −2 Engineering |
| Robot Arm | IV | +3 Melee, +3 Engineering, −1 Max HP (all at end of wave) |
| Sad Tomato | III | +8 HP Regen, start waves with −50% HP |
| Scar | I | +20% XP, −8 Range |
| Scared Sausage | I | Attacks 25% chance to deal burning damage |
| Scope | II | +2 Ranged, +25 Range, −7% Attack Speed |
| Shackles | III | +8 HP Regen, +8 Engineering, +80 Range, Speed capped at current value |
| Shady Potion | II | +20 Luck, −2 HP Regen |
| Sharp Bullet | I | Projectiles pierce +1, −20% Piercing Dmg, −5% Damage, −3 Knockback |
| Shmoop | III | +6 Max HP, +2 HP Regen, −2 Melee, −1 Ranged |
| Sifd's Relic | IV | +3 Armor, +100% chance to instantly attract dropped material |
| Silver Bullet | III | +25% damage vs bosses and elites |
| Small Magazine | II | +2 Ranged, +10% Attack Speed, −6% Damage |
| Snail | II | −8% Enemy Speed, −3% Speed |
| Snake | I | Burning spreads to nearby enemy, −1 Max HP |
| Snowball | II | +1 Elemental each time you get an Elemental item |
| Spicy Sauce | II | +3 Max HP, consumables 50% chance to explode on pickup |
| Spider | IV | +12% Damage, +6% Attack Speed per different weapon, −3% Dodge, −5 Harvesting |
| Statue | III | +40% Attack Speed standing still, −10% Speed |
| Strange Book | III | +1 Engineering per 1 Elemental, −1 Melee, −1 Ranged |
| Stone Skin | III | +1 Max HP per 1 Armor, −6% Attack Speed |
| Sunglasses | II | +10% Crit, −1 Armor |
| Tardigrade | III | Nullifies damage of one hit each wave |
| Tentacle | II | +3% Crit, 20% chance heal 1 HP on crit kill |
| Terrified Onion | I | +4% Speed, −5 Luck |
| Toolbox | III | +6 Engineering, −8% Attack Speed |
| Torture | IV | +15 Max HP, restore 5 HP/s, cannot heal any other way |
| Toxic Sludge | I | +2 Elemental, −2% Dodge |
| Tractor | III | +40 Harvesting, −8% Damage |
| Tree | I | More trees spawn |
| Triangle of Power | III | +20% Damage, +1 Armor, −2% Damage when hit until wave end |
| Turret | I | Spawns turret shooting bullets |
| Tyler | II | Spawns little guy shooting 10 piercing lightning projectiles |
| Ugly Tooth | I | Hitting enemy removes 5% of their speed (max 20%), −3% Speed |
| Vigilante Ring | III | +3% Damage at end of wave |
| Wandering Bot | III | Spawns bot that slows nearby enemies |
| Warrior Helmet | III | +3 Armor, +5 Max HP, −5% Speed |
| Weird Food | I | +2 HP from consumables, −2% Dodge |
| Weird Ghost | I | +3 Max HP, start next wave with 1 HP |
| Wheat | III | +4 Melee, +2 Ranged, +10 Harvesting, −2 Elemental |
| Wheelbarrow | II | +16 Harvesting, −1 Armor |
| Whetstone | II | +4% Life Steal, −3 Knockback |
| White Flag | II | +5 Harvesting, −5% Enemies |
| Will-o'-Wisp | III | +1 Elemental per 30 burning enemies killed in wave |
| Wings | III | +10% Speed, +30 Range, −2 Elemental |
| Wisdom | III | +5% Damage/5s until wave end, −15% Damage |
| Wolf Helmet | IV | +10 Elemental, +20 Luck, −5 Engineering |

## B3. Characters (48)
Each carries a strong archetype-defining starting modifier.

| Character | Starting modifier |
|---|---|
| Well Rounded | +5 Max HP, +5% Speed, +8 Harvesting (balanced starter) |
| Brawler | +50% Attack Speed w/ Unarmed, starts w/ Fist, +15% Dodge; −50 Range, −50 Ranged Dmg |
| Crazy | +100 Range w/ Precise, +25% Attack Speed, starts w/ Knife; −30% Dodge, −10 Eng, −10 Ranged |
| Ranger | +50 Range, starts w/ Pistol, +50% Ranged Dmg mods, can't equip melee; −25% Max HP mods |
| Mage | +25% Elemental Dmg mods, starts w/ Snake + Scared Sausage; −100% Melee/Ranged, −50% Eng mods |
| Chunky | +25% Max HP mods, +1% Dmg per 3 Max HP, +3 HP from consumables; −100% Life Steal, −100% Speed |
| Old | −25% enemy speed, +10 Harvesting, −33% map size, −10% enemies; −10% Speed |
| Lucky | +100 Luck, +25% Luck mods, random-damage on material pickup; −60% Attack Speed, −50% XP |
| Mutant | −66% XP required to level; +50% items price |
| Generalist | +2 Melee per 1 Ranged, +1 Ranged per 2 Melee; limited to 3 melee + 3 ranged weapons |
| Loud | +30% Damage, +50% enemies; −3 Harvesting each wave |
| Multitasker | +20% Damage, up to 12 weapons; −5% Damage per weapon equipped |
| Wildling | +30% Life Steal w/ Primitive, starts w/ Stick; can't equip weapons above Tier 2 |
| Pacifist | 0.65 material/XP per living enemy at wave end, starts w/ Lumberjack Shirt; −100% Dmg, −100 Eng |
| Gladiator | +20% Attack Speed per different weapon, +5 Melee, can't equip ranged; −40% Attack Speed, −30 Luck |
| Saver | +15 Harvesting, +1% Dmg per 25 materials, starts w/ Piggy Bank; +50% item/weapon price |
| Sick | +12 Max HP, +25% Life Steal, takes 1 dmg/s; −100 HP Regen |
| Farmer | +20 Harvesting, +3% Harvesting at wave end, +1 Harvesting eating at full HP; −50% materials dropped |
| Ghost | +10 Dmg w/ Ethereal, +30% Dodge (cap 90%); −100 Armor |
| Speedy | +30% Speed, +1 Melee per 2% Speed; −100 Armor standing still, −3 Armor |
| Entrepreneur | −25% items price, +50% Harvesting mods, +25% recycling; −100% materials at wave start, −50% Dmg mods |
| Engineer | +10 Eng, +25% Eng mods, starts w/ Wrench, structures spawn close; −50% Dmg mods |
| Explorer | More trees, starts w/ Lumberjack Shirt, +10% Speed, +50% pickup, +33% map, +25% enemies; −40% Damage |
| Doctor | +200% Attack Speed w/ Medical, +5 HP Regen, +100% HP Regen mods, +5 Harvesting; −100% Attack Speed |
| Hunter | +100 Range, +1% Dmg per 10 Range, +25% Crit mods; −100% Harvesting mods, −33% Max HP mods |
| Artificer | +175% Explosion Dmg, +4% Explosion Size per 1 Elemental, +100% Dmg w/ Tool; −100% Damage |
| Arms Dealer | −95% weapons price, +30 Harvesting, +33% Dmg mods, starts w/ Dangerous Bunny; weapons destroyed entering shop |
| Streamer | +3% materials/s standing still, +40% Dmg & Attack Speed while moving, +2 Armor per structure; high-materials penalties |
| Cyborg | Starts w/ Minigun, +200% Ranged Dmg mods, converts Ranged→Eng mid-wave; −100% Melee/Elemental |
| Glutton | +50 Luck, consumables explode for 10 dmg, +1% Explosion Dmg on pickup at max HP; +25% price, −25% XP |
| Jack | +125% dmg vs bosses/elites, +200% materials; −70% enemies but +175% enemy HP, +35% enemy dmg |
| Lich | +10 HP Regen, +10% Life Steal, on heal deal Max-HP dmg to random enemy; −50% Dmg mods |
| Apprentice | On level up: +2 Melee, +1 Ranged, +1 Elemental, +1 Eng; −2 Max HP per level |
| Cryptid | +6 trees, 12 material/XP per living tree at wave end, +3 HP Regen per tree; −100% Life Steal, −100 Range |
| Fisherman | +5 Max HP, +20 Harvesting, shops always sell Bait, +2 Harvesting per Bait; Baits spawn special enemies |
| Golem | +20 Max HP, +33% Max HP & Armor mods, +40% Attack Speed / +20% Speed below 50% HP; can't heal |
| King | +50 Luck, +25% Dmg/Attack Speed per Tier IV weapon, +5 Max HP per Tier IV item; penalties for Tier I |
| Renegade | +2 projectiles, +1 pierce, +10% Dmg per Tier I item, can't equip melee; −400% Damage, −50% accuracy |
| One Armed | +200% Attack Speed, +100% Dmg mods; can only equip one weapon |
| Bull | +20 Max HP, +15 HP Regen, +10 Armor, explodes for 30 on taking damage; can't equip weapons |
| Soldier | +50% Dmg/Attack Speed standing still, +10% Speed, +200% pickup, +15 Knockback; can't attack while moving |
| Masochist | +5% Dmg on taking damage until wave end, +10 Max HP, +20 HP Regen, +8 Armor; −100% Damage |
| Knight | +2 Melee per 1 Armor, +3 Armor, can't equip ranged, only Tier 2+ weapons; −50% Attack Speed% |
| Demon | +50% materials→Max HP at wave end; buy items using Max HP instead of materials |
| Baby | +12 Harvesting, −20% items price, gain weapon slot on level up (start with 1); +130% XP required |
| Vagabond | Equipped weapons share bonuses, can't equip duplicates; −5 Armor, −5% Dodge, −50% Luck/Harvesting mods |
| Technomage | Starts w/ 2 Turrets, +5% structure attack speed per Elemental, +2 Elemental per structure; +75% XP req |
| Vampire | +2% Dmg per 1% missing HP, +1% Life Steal per 3% missing, +1 Armor per 5% missing; −60% Damage |

## B4. Excluded (Abyssal Terrors DLC) & flags
- **DLC weapons (16, excluded):** Anchor, Blunderbuss, Brick, Captain's Sword, Chainsaw, Flute, Grenade
  Launcher, Harpoon Gun, Hiking Pole, Javelin, Lute, Mace, Sickle, Spoon, Trident, War Hammer. **DLC classes
  (2):** Naval, Musical.
- **DLC items (~32, excluded):** Ashes, Axolotl, Baby Squid, Barnacle, Black Flag, Bone Dice, Cauldron, Coral,
  Corrupted Shard, Crystal, Eyepatch, Feather, Fish Hook, Goblet, Goldfish, Jerky, Knot, Kraken's Eye,
  Lantern, Lighthouse, Mirror, Pearl, Penguin, Saltwater, Scarf, Seashell, Small Fish, Spyglass, Starfish,
  Sunken Bell, Treasure Map, Whistle.
- **DLC characters (14, excluded):** Sailor, Curious, Builder, Captain, Creature, Chef, Druid, Dwarf, Gangster,
  Diver, Hiker, Buccaneer, Ogre, Romantic.
- **Base-vs-DLC flags (verify):** a few excluded items aren't obviously nautical/curse-themed — *Feather,
  Mirror, Penguin, Bone Dice, Crystal, Eyepatch, Goldfish, Knot, Jerky, Cauldron, Goblet* — confirm against
  the live wiki if any of these matter.

---

## Sources fetched
- **Vampire Survivors** (mirror `vampire.survivors.wiki`): `/w/Weapons`, `/w/Passive_Items`, `/w/Evolution`,
  `/w/Arcana`. *(Fandom endpoints returned HTTP 402 — blocked.)*
- **Brotato** (mirror `brotato.wiki.spellsandguns.com`): `/Weapon_Classes`, `/Weapons`, `/Items`,
  `/Characters`, `/Abyssal_Terrors_DLC`. *(`brotato.wiki.gg` HTTP 401, `brotato.fandom.com` HTTP 402 — blocked.)*

*Analysis of what this catalog reveals (choice architecture, presentation, synergy, meta) lives in the
companion [`case-study-vs-brotato-v0.1.md`](case-study-vs-brotato-v0.1.md).*
