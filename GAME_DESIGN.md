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

20 floors total (decided, not yet implemented — there is no floor-tracking system in code today).

- Each floor has a level cap. **Floor 1 cap is 19**, hardcoded as of this session
  (`DefaultLevelUpScript.FloorLevelCap`, enforced in `DefaultExperienceDistributionScript.GiveExp`).
  Caps for floors 2–20 are not yet decided.
- Chamber rewards per floor: not yet decided beyond the accessory-set concept below.
- **Not implemented**: which floor a player is currently on, floor unlock/progression logic, and a
  per-floor cap lookup (right now `FloorLevelCap` is a single global constant, not floor-aware).

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

Decided concept, not implemented: the first player/group to clear a floor gets the full accessory
set for that floor; everyone else has to go hunt the next floor's set instead of getting a shot at
the one already claimed. No mechanism exists yet for tracking "first clear," no accessory sets have
been defined, and there's no floor-clear detection system to hook this into.

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
- Define floor 2–20 level caps and the actual floor-tracking mechanism.
- Decide weapon-type restrictions for every class besides Archer.
- Decide what "materials" means for the enhancement system beyond gold.
