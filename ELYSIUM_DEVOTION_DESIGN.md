# Elysium — Devotion System Design (Master Document)

**Status: LIVE DOCUMENT — same discipline as the other design docs.**
Saved immediately, not dependent on chat memory.

**Cross-reference:** builds on ELYSIUM_CLASS_DESIGN.md's "Lore — The Two
Gods" section (Stacia, Valkor). This is a separate system from
ELYSIUM_ECONOMY_DESIGN.md's three currencies (Valkor's Sigils, Stacia's
Essence, Elysian Tokens) — devotion items are god-reputation currency, not
equipment/enchant/cosmetic currency, even though names are close.

**⚠️ Naming collision flag:** "Valkor's Insignia" (devotion) is very close
to "Valkor's Sigils" (equipment upgrade currency, already locked in
ELYSIUM_ECONOMY_DESIGN.md). Same for "Stacia's Rose" (devotion) vs.
"Stacia's Essence" (enchanting currency). Both pairs are thematically
consistent (same god) but close enough in name that players could confuse
them. Not resolved — flagged for a decision: keep as-is (intentional
thematic echo) or diverge the names further.

---

## Design Philosophy

None of these unlocks have to be direct player power. They can mostly be
**prestige, convenience, lore, and identity** — a devotion/reputation
system per god, separate from combat progression.

## The Two Devotion Currencies (LOCKED)

### 🌹 Stacia's Rose
Dropped while devoted to Stacia. Turned in at Stacia's temples to increase
devotion.

### ⚔️ Valkor's Insignia
Dropped while devoted to Valkor. Turned in at Valkor's temples to increase
devotion.

*(User's favorites of everything designed so far — called these "perfect
counterparts.")*

---

## Religions (LOCKED)

**Players may devote themselves to ONE deity — exclusive choice, not
split/dual devotion.**

### 🌹 Stacia
- Turn in Stacia's Roses
- Grace, healing, hope, protection

### ⚔️ Valkor
- Turn in Valkor's Insignias
- War, valor, conquest, strength

**Deferred until after core game is playable** (explicitly not a current
priority — noted so this doesn't get chased prematurely):
- Devotion ranks
- Temple rewards
- Blessings
- Religion switching
- Festivals
- Quests

---

## Devotion Tier Unlocks (proposed, not fully locked)

```
Devotion 1   → Temple access
Devotion 5   → Unique title
Devotion 10  → Exclusive cosmetics
Devotion 20  → Blessing aura
Devotion 30  → Religion mount
Devotion 50  → Legendary appearance
```

## Additional unlock ideas (not tier-assigned yet)
- Daily blessings
- Temple vendors
- Exclusive quests
- Seasonal events
- Religion-themed housing decorations

---

## Open Questions / Not Yet Decided

1. Naming collision (Sigils vs. Insignia, Essence vs. Rose) — see flag
   above.
2. ~~Can a player be devoted to BOTH gods simultaneously~~ RESOLVED —
   exclusive choice, one deity only. See "Religions" section above.
3. Exact devotion-point values for the tier unlocks (1/5/10/20/30/50) are
   listed as tier thresholds, but how devotion points are earned per Rose/
   Insignia turn-in (1:1? scaled?) isn't defined.
4. Does devotion ever intersect with the class system? E.g. does Valkyrie
   (already flagged in the Lore section as sitting at the Stacia/Valkor
   intersection) get any unique interaction with devotion from both gods?
5. Is there a THIRD god-neutral path for players who don't want to commit
   to either devotion track, or is devotion meant to be a universal system
   everyone engages with regardless of class/alignment?

---

*Last updated: this session.*
