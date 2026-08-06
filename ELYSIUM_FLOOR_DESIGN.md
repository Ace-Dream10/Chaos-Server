# Elysium — Floor Development System (Master Document)

**Status: LIVE DOCUMENT — same discipline as ELYSIUM_CLASS_DESIGN.md.**
Saved immediately, not dependent on chat memory.

**Cross-reference:** this directly fills in one of the biggest open items
from tonight's session — the Ascension Chamber currently has 0 of 10 real
floor maps built. The floor-tracker CODE (AscensionFloorNumber field,
participant tracking, boss/first-clearer state, HUD panel) is already
fully built and tested — what's been missing is exactly this: the actual
content design for what a floor IS. This document is that answer.

---

## Core Philosophy

Every floor should have multiple ways to play. Every floor is built as a
**complete vertical slice** before moving on to the next one — no floor
ships partial.

## Every Floor Must Contain

### 🌍 Open Hunting Area
- Main leveling zone
- Exploration
- Gathering
- Side quests
- Field elites

### 🕳️ Starter Cave
- First dungeon experience for the floor
- Simple mechanics
- Introduces the floor's monsters and mechanics — teaches players the floor

### 🏰 Repeatable Dungeon
- XP
- Gear farming
- Materials
- Designed for repeat/farm content, not one-time content

### 🌀 Labyrinth
**This is the differentiator — where Elysium can become unique.** Not
"just another dungeon." Should feel genuinely dangerous:
- Getting lost
- Locked rooms
- Elite monsters
- Hidden merchants
- Rare materials
- Secret bosses
- Treasure vaults
Something players keep coming back to, not a one-and-done clear.

### 👑 Field Boss
### 💀 Floor Boss
### ⚔️ Equipment Tier (Weapon Tier)
### 👹 Unique Monster Roster
- Avoid reusing monsters between floors whenever possible.
- Every floor should feel like entering a new ecosystem.

### Additional per-floor checklist items (from the full list provided)
- Story NPCs
- Crafting Materials

---

## Per-Floor Checklist Template

Use this exact checklist for every floor as it's built. Nothing gets
marked ✓ until it's actually built and tested, not just planned.

```
Floor N

☐ Open Area
☐ Starter Cave
☐ Repeatable Dungeon
☐ Labyrinth
☐ Field Boss
☐ Floor Boss
☐ Gathering
☐ Crafting Materials
☐ Story NPCs
☐ Equipment Set
☐ Weapon Tier
☐ Unique Monster Roster (no reuse from other floors)
```

## Floor Status (10 floors total, per tonight's confirmed floor count)

| Floor | Status |
|---|---|
| 1 | ☐ Not started |
| 2 | ☐ Not started |
| 3 | ☐ Not started |
| 4 | ☐ Not started |
| 5 | ☐ Not started |
| 6 | ☐ Not started |
| 7 | ☐ Not started |
| 8 | ☐ Not started |
| 9 | ☐ Not started |
| 10 | ☐ Not started |

*(Sorcerer's Floor 3/5/7 specialization choices and Martial Artist's
Floor 2 spec choice, per ELYSIUM_CLASS_DESIGN.md, mean Floors 2, 3, 5, and
7 have extra design weight — worth building those with class-progression
gating in mind, not just as generic floors.)*

---

## Open Questions / Not Yet Decided

1. Floor 1–10 level caps — still not decided (carried over from
   GAME_DESIGN.md's existing open item). This vertical-slice structure
   makes the question more concrete: what level should a player be to
   reasonably clear Starter Cave vs. Labyrinth vs. Floor Boss on a given
   floor — these probably need different sub-targets, not one flat number
   per floor.
2. How does the Labyrinth interact with the floor tracker's existing
   first-clearer/participant-tracking system (already built) — does the
   Labyrinth have its OWN secret-boss first-clearer tracking distinct from
   the main Floor Boss, given it's explicitly designed to have "secret
   bosses" as a sub-feature?
3. Field Boss vs. Floor Boss — are these tracked separately by the
   existing AscensionBossDeathScript/participant system, or is only the
   Floor Boss wired into that system? Worth confirming before Floor 1
   implementation starts, since the existing code was built with "the
   boss" as a singular concept per floor.
4. Unique Monster Roster "no reuse" rule — worth checking against
   ABILITY_DATABASE.md/existing monster templates for how many genuinely
   distinct monster types already exist vs. need to be newly designed for
   10 floors × unique rosters.

---

*Last updated: this session.*
