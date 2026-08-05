# Game Design Notes

Living document capturing design decisions made so far. This reflects a mix of what's actually
implemented in code today and what's been decided but not yet built — each section says which.

## Class Structure

Three base paths, each branching into specializations. `BaseClass` and `AdvClass` are defined in
`Chaos.DarkAges/Definitions/Enums.cs`.

```
BaseClass: Unassigned, Lancer, WeaponMaster, Sorcerer, Mystic, MartialArtist, Recruit, Strider, Magus, Diacht
AdvClass:  None, Assassin, Trickster, Archer, Bard, Summoner
```

**Recruit path** — melee specializations, set directly via `BaseClass` with no `AdvClass`:
- Lancer (was "Guardian" — renamed mid-development)
- WeaponMaster
- MartialArtist

**Strider path** — `BaseClass.Strider` stays constant, specialization is carried by `AdvClass`:
- Archer
- Assassin
- Trickster

**Magus path** — caster cluster. Currently only Bard is wired end-to-end
(`BaseClass.Magus` + `AdvClass.Bard`). Sorcerer and Mystic exist as their own direct `BaseClass`
values (same "no AdvClass" pattern as the Recruit path) rather than being grouped under Magus +
AdvClass — this is a leftover inconsistency from iterative renames, not an intentional design
choice, and should be reconciled before calling the 9-specialization structure "final."
`AdvClass.Summoner` exists in the enum but has no class-selector wiring yet.

Class assignment happens via the Class Selector NPC (`Chaos/Scripting/DialogScripts/SetClassScript.cs`),
which currently allows free reclassing at any time (no lock-in) — a deliberate temporary choice while
the class system is still being finalized.

## Floor Progression

**10 floors total** (revised from an earlier 20-floor draft — corrected during the floor-tracker design
pass; see `FLOOR_TRACKER_DESIGN.md`). Target endgame character level is **255**, which is also a hard
wire-protocol ceiling, not just a design target: `Chaos.Networking/Entities/Server/AttributesArgs.cs`
sends `Level` as a `byte`, so nothing above 255 can display correctly on the client regardless of what
the server-side `int` `Level` holds.

- Each floor has a level cap. **Floor 1 cap is 19**, hardcoded as of an earlier session
  (`DefaultLevelUpScript.FloorLevelCap`, enforced in `DefaultExperienceDistributionScript.GiveExp`) —
  that number was set against the old 20-floor plan and has **not been re-derived for 10 floors**.
  Simply halving the floor count doesn't obviously imply a new per-floor curve (evenly spacing 255
  across 10 floors gives ~25-26/floor, which doesn't match a floor-1 value of 19 either) — this needs an
  explicit decision, not a guess. Caps for floors 2–10 are also still undecided.
- Chamber rewards per floor: not yet decided beyond the accessory-set concept below.
- **Not implemented in code yet, but designed**: per-player current floor and highest-floor-cleared
  (`Aisling.Trackers.Counters`), per-floor boss/first-clearer state (new `AscensionFloorStore`), and the
  floor-tracker HUD packet. See `FLOOR_TRACKER_DESIGN.md` for the full design. Still not implemented: a
  per-floor level-cap lookup (`FloorLevelCap` is still a single global constant, not floor-aware).

## Weapon Types Per Class

Only Archer currently has an enforced weapon requirement: skills/spells check
`Equipment[EquipmentSlot.Weapon].Template.Category.EqualsI("bow")` and refuse to fire without one
(`BowShotScript`, `FlechetteScript`, `ArrowstepScript`, etc., all send "You need a bow equipped."
otherwise). No other class currently has a coded weapon-type restriction — this needs to be decided
and built out per class (e.g. Lancer/polearms, WeaponMaster/heavy weapons, Sorcerer/staves).

## Enhancement System

Implemented this session as a foundation — no material system, no failure chance yet, gold only.

- Tiers run **+1 to +9**, capped at 9. Tracked via `Item.EnhancementLevel` (persisted through
  `ItemSchema.EnhancementLevel`), not the `Trackers.Tags` pattern used on creatures — items don't
  have a Trackers/tag system, so this is a dedicated int property instead.
- Cost is gold-only: **100 gold × target tier** (+1 = 100g, +9 = 900g).
- No failure chance — every enhancement attempt that has enough gold succeeds.
- Bonus formula: each base modifier on the item's template is boosted by roughly 10% per tier,
  recalculated from the template's base values each time (not compounded):
  `enhancedValue = baseValue + round(baseValue * tier * 0.1)`.
  A +5 stat becomes +6 at tier 1 (5.5 rounds up), +6 at tier 2, ... +10 at tier 9 (9.5 rounds up).
- Requires `Template.IsModifiable = true` on the item.
- Reference implementation: `Chaos/Scripting/ItemScripts/EnhancementScript.cs` (the mechanic) plus
  `EnhanceItemInfoScript.cs` / `EnhanceItemScript.cs` (the Blacksmith NPC dialog flow — info screen,
  then a confirm step that charges gold and applies the tier).
- Not yet decided: material costs beyond gold, whether enhancement can fail at higher tiers, whether
  it's per-item-type gated (only weapons? armor too?), and whether this whole system gets reworked
  once materials are introduced.

## Spell Use-Based Progression

Parked — no details decided yet. The idea (spells leveling up or unlocking through repeated use
rather than a flat ability-point pool) has been raised but not designed or built.

## Accessory Sets Per Floor

Decided concept, not implemented: the first-clearer(s) of a floor get the full accessory set for that
floor; everyone else has to go hunt the next floor's set instead of getting a shot at the one already
claimed. "First clearer" credit is **participation-based**, not party/group membership: anyone who
dealt damage to the floor boss or took damage from it during that fight is credited, regardless of
whether they were grouped (see `FLOOR_TRACKER_DESIGN.md`'s Participation Tracking section for the
mechanism). No accessory sets have been defined yet, and the reward-granting step itself (turning a
tracked first-clear into actually handing out items) is not implemented — the tracker design covers
detecting and persisting first-clear, not granting the reward.

## Stat System

Every level up now grants **+1 to all five stats automatically** (STR, DEX, INT, WIS, CON) — no
stat point pool, no player choice. Implemented in `DefaultLevelUpScript.LevelUp()` by giving 5
points and immediately spending them one-per-stat via the existing `IncrementStat` API, so nothing
is ever left unspent. This replaced the previous default of 2 free points the player could allocate
manually.

## Open Questions / Inconsistencies to Resolve

- Reconcile Sorcerer/Mystic into the Magus + AdvClass pattern (or decide they're genuinely separate
  from the 3x3 structure).
- Wire up `AdvClass.Summoner` in the class selector.
- Define floor 2–10 level caps, and re-derive floor 1's cap (currently 19, set against the old
  20-floor plan) for the corrected 10-floor / level-255 target.
- Decide weapon-type restrictions for every class besides Archer.
- Decide what "materials" means for the enhancement system beyond gold.
- **Class-flatten Phase 2 — save-migration tooling.** Existing character saves with pre-flatten
  `baseClass`/`advClass` values were hand-fixed once (2026-08-04) as a one-off crash fix, not built
  as reusable tooling. If more save-data drift happens in the future (e.g. further class changes),
  there's no generic migration path to handle it.
- **Class-flatten Phase 3 — content re-tagging.** `Skills/Lancer/` folder, in-game skill display
  names ("Lancer's Shield," etc.), and the `LancersRetribution` effect key still reference "Lancer"
  even though the class is now `Bastion`. Cosmetic/naming inconsistency, not broken.
- **Class-flatten Phase 4 — GroupBox 11-into-5 bucket redesign.** Deferred by explicit decision to
  "future state UI changes." Not urgent — 0 live characters currently in the affected classes.
- **The 10 real Ascension Chamber floor maps.** Pure content/level-design work in
  ChaosAssetManager's map editor. Currently 0 of 10 exist. Floor tracker code is fully ready
  (`AscensionFloorNumber` field + detection) to support them once built.
- **Pet/summon ownership resolution.** Fast-follow from the floor tracker's participant-tracking
  work — ShadowClone/MirrorImage/DustDevil damage doesn't currently credit the summoning player for
  first-clearer credit, since no ownership-tracking mechanism exists on `Monster` yet (see
  `FLOOR_TRACKER_DESIGN.md` §6's "Known follow-up").
- **This doc's own Class Structure section is stale.** Still documents the old
  `Unassigned/Lancer/WeaponMaster/.../Diacht` model with `AdvClass` grouping — doesn't reflect the
  current flat 12-class `BaseClass` enum.
- **Ground-targeted/AoE-telegraph casting.** Investigation brief was sent (client targeting-mode UI,
  a new packet for coordinate-based casting, `DamageScript`/shape resolution against an arbitrary
  point) — no report has come back yet. Still unscoped.
