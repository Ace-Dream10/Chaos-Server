# Elysium — Class Design (Master Document)

**Status: LIVE DOCUMENT — updated incrementally, saved after every class.**
This file is the single source of truth for class design. Do not rely on
chat memory for anything in here — if it's not in this file, it doesn't
exist yet.

**Tracked per class: Identity, Role, 12 Active, 3 Passive, 5 Evolving.**
(Gameplay Loop dropped as a tracked category per user decision.)

**Note on "built code" cross-references below:** most existing built
abilities are expected to be reworked/replaced as this redesign proceeds.
These notes aren't "content to preserve" — they're flagging which
underlying MECHANICS (resource systems, damage/shape pipelines, targeting)
are already proven and reusable, regardless of what the abilities using
them end up being named or designed as.

**Note on star ratings:** any ⭐ markers in the source material are
dropped/ignored per user instruction — not tracked in this document.

**Note on active/passive counts:** 12 active / 3 passive is a rough
template, not a hard rule — some classes intentionally deviate (e.g.
Valkyrie: 11 active / 4 passive, confirmed intentional).

## Overall Design Philosophy
Every class should have:
- A unique class mechanic/resource.
- A survivability mechanic.
- Good utility.
- A distinct gameplay loop.
- 5 evolving abilities.
- Strong identity that does not overlap heavily with other classes.

Not every class should excel at everything, but every class should always
feel useful in solo and group content.

---

## Confirmed Structure (as of tonight's class-flatten work)

BaseClass enum is flat — all 12 classes are siblings, no intermediate
path/tier grouping in code (though the design below still groups them by
"Path" conceptually for identity/lore purposes):

`Unregistered, Bastion, Berserker, Slayer, Valkyrie, Assassin, Trickster,
Archer, Sorcerer, Mystic, Bard, MartialArtist`

AdvClass is narrowed to exactly two uses:
- **Sorcerer** — 11 elemental specialization values (Floor 3/5/7 progression)
- **MartialArtist** — 3 values: Fighter, Tank, RangedChi (Floor 2 choice)
- Everyone else: AdvClass.None

## Confirmed Mana Orb Colors (HUD identity)
| Class | Color | Status |
|---|---|---|
| Berserker | Orange-red | ✅ Confirmed live |
| Valkyrie | Purple | ✅ Confirmed live |
| Slayer | Pink | ✅ Confirmed live |
| Assassin | Yellow | ✅ Confirmed live |
| Martial Artist | Green | ✅ Confirmed live |
| (remaining 6 classes) | Default blue | Not yet assigned unique colors |

---

## Lore — The Two Gods

### 🌸 Stacia
**Goddess of Grace, Light, Hope, and Protection**
Associated with: Healing, Mercy, Guidance, Life, Purity

*Note: this is the confirmed in-universe origin of the "Stacia's ___"
naming convention already used throughout existing built abilities
(Stacia's Vitae, Stacia's Blessing, Stacia's Shrine, Stacia's Wrath,
Stacia's Hymn, Stacia's March, Stacia's Veil, Stacia's Bubble, Stacia's
Cleanse, Stacia's Chorus, Stacia's Lullaby, Stacia's Pulse) — not
arbitrary naming, a real thread running through Bard/Mystic's kits.*

### ⚔️ Valkor
**God of War, Valor, Honor, and Conquest**
Associated with: Battle, Strength, Courage, Victory, Divine Wrath

**Design note:** the two feel like natural opposites without being framed
as good vs. evil — worth keeping that tension in mind for any future
faction/alignment content. Thematic cross-reference: Valkyrie's identity
("Holy battlefield commander," Divine Fury/Ragnarok, holy-damage kit) sits
right at the intersection of both gods — worth deciding at some point
whether Valkyrie is meant to be Valkor-aligned, Stacia-aligned, or
deliberately a bridge between the two (the name itself suggests a Valkor
connection, but the "holy"/healing-adjacent framing leans Stacia).

---

## Reference Material — Retail Dark Ages Priest/Wizard Spell Kit

**Purpose:** source material to inform Mystic and Bard's designs (both
still open). NOT a locked design — user's explicit intent is to divide
these between Mystic/Bard, keep it fresh, and combine some spells to make
room for unique new things rather than a direct port.

**Core mechanic context — AC in retail DA:** the LESS AC you have, the
MORE defense you get (inverted from what "AC" might suggest at a glance).
Can go as low as -99. This is why "reducing AC" is a buff (Armachd) and
"increasing AC" is a debuff (Cradh) in the list below.

**Buffs:**
- **Beannaich** — increases your hit
- **Fas Deireas** — increases attack

**Curses/Debuffs:**
- **Cradh** — increases target's AC (bad — they take more damage). Both
  monsters and priests can cast this.
- **Armachd** — reduces AC (good — despite being castable as a
  "curse"-category spell, this is actually beneficial)
- **Naomh Aite** — reduces damage taken by %
- Most/all curses have an **Ao** (AoE) variant — e.g. "Ao Cradh" cleanses
  Cradh specifically (see Cleanse below).

**Crowd control:**
- **Pramh** — sleeps a target for X seconds or until first hit
- **Suain** — freezes target
- **Dall** — blinds (alternatively, **Ao Dall** unblinds allies — AoE
  variants aren't always offensive, some are supportive cleanses)

**Immunity/Protection:**
- **Mor Dion Comhla** — team immunity
- **Dion** — self immunity

**Healing:**
- **Salvation** — full HP heal

**Wizard-exclusive (explicitly NOT wanted for Sorcerer — see note below):**
- **Fas Nadur** — amplifies damage taken based on elemental
  weakness/resistance interaction (e.g. if a target has dark defense set
  and you cast Fas Nadur on them, hitting them with light damage afterward
  deals amplified damage).

**Design note worth flagging:** Fas Nadur's "amplify damage via elemental
weakness setup" mechanic sounds thematically suited to something with an
elemental system — but the user has explicitly said they do NOT want this
on Sorcerer (Sorcerer already has its own locked elemental
specialization system, see below). Worth considering this for Mystic
instead, given Mystic's "spiritual manipulation, curses, tethers" identity
(from the original locked design doc) — an elemental-weakness curse fits
a curse-focused support/debuffer class better than it fits Sorcerer's
already-distinct "choose your element and cast it" identity anyway.

---

## Guardian Path

### Bastion (formerly Lancer)
**Role:** Primary: Tank / Battlefield Controller

**Identity:** Self Shield, Self Heal, Counter, Aggro, Positioning, Control,
Crowd Control

**Active Abilities (12):**
| Ability | Role |
|---|---|
| Challenging Shout | Mass aggro |
| Iron Bastion | Defensive cooldown |
| Iron Will | Self heal |
| Bastion's Charge | Engage / mobility |
| Lancer's Leash | Pull enemies together |
| Lancer's Shield | Personal barrier |
| Iron Cairn | Area control / damage |
| Pivot Strike | Reposition behind target |
| Counter *(rename later)* | Timed counterattack |
| Shield Thrust | Knockback |
| Perfect Stand | Temporary invulnerability |
| Slow Shout *(rename later)* | AoE attack speed slow |

**Passive Abilities (3):**
| Ability | Effect |
|---|---|
| Lancer's Retribution | Reflect/counter a portion of incoming damage |
| Guardian's Resolve | Gain defenses based on nearby enemies |
| Hold the Line | Enemies attacking nearby allies generate increased threat toward you |

**Evolving Abilities (5):**
1. **Iron Cairn** — the iconic Bastion ability. Evolves from a small earth
   eruption into a massive battlefield control tool.
2. **Lancer's Leash** — starts as a small pull. Eventually gathers entire
   groups, longer root, maybe applies a debuff.
3. **Challenging Shout** — evolves into a more commanding tank tool.
   Evolutions could add: larger radius, longer threat duration, attack
   speed reduction, damage reduction while active, brief taunt immunity.
4. **Bastion's Charge** — more range, more damage, better engage.
   Eventually could knock enemies aside or leave a cracked path.
5. **Perfect Stand** — the ultimate defensive cooldown. Longer duration
   and stronger effects with each evolution.

**Mechanics already proven reusable (per tonight's ABILITY_DATABASE.md/code
review):** shield/absorb mechanics (Lancer's Shield, Stacia's Will), pull
mechanics (Lancer's Leash), reflect/counter damage (Lancer's Retribution),
AC/defensive buffs (Iron Bastion) — these underlying systems already exist
and work, even though most of the specific abilities above are new
names/designs built on top of them.

**Status: ✅ DESIGN LOCKED — full active/passive/evolving kit defined.**

### Berserker
**Role:** AoE DPS

**Identity:** Rage-fueled destroyer. Generates Rage while remaining in
combat and unleashes devastating area damage.

**Resource:** Rage — hitting mobs generates, passive damage bonus
(confirmed built, BerserkerRageScript.cs)

**Active Abilities (12):**
1. Berserker Charge
2. Broad Swipe
3. Cyclone
4. Thunderstrike
5. Vampiric Strike
6. Battle Cry
7. Berserker Gate
8. Massacre
9. Titanic Fury
10. Ground Fracture *(or redesign later)*
11. Intimidating Shout
12. Seismic Leap

**Passive Abilities (3):**
1. Rage *(core mechanic)*
2. Carnage
3. Unbroken

**Evolving Abilities (5):**
1. **Cyclone** — evolution: more rotations → larger radius → faster spin →
   final shockwave. *(This is the class's signature spin/whirlwind — see
   Open Questions, this was previously flagged as unbuilt.)*
2. **Seismic Leap** — evolution: larger impact → bigger radius → slow →
   stun at max.
3. **Berserker Gate** — evolution: higher damage bonus, lower penalty,
   Rage interactions, longer duration. The iconic "go berserk" button.
4. **Titanic Fury** — evolution: more attack speed, more Rage consumption,
   more devastating burst. Author's note: this is Berserker's equivalent
   of Slayer's Scythe.
5. **Massacre** — evolution: wider cleave → larger AoE → additional hits →
   huge finishing swing.

**Mechanics already proven reusable:** Rage resource system (built,
BerserkerRageScript.cs), whirlwind/spin-style AoE has real precedent via
other classes' AoE shapes (allAround, frontalCone).

**Status: ✅ DESIGN LOCKED — full active/passive/evolving kit defined.**

### Slayer
**Role:** Single Target DPS

**Identity:** Execution specialist. Marks a target, builds Execution on a
single enemy, then finishes them with Scythe.

**Active Abilities (12):**
1. Scythe
2. Flourish
3. Mark of the Bane
4. Cruel Thrust
5. Cold Blood
6. Measured Slice
7. Overkill
8. Bloodlust *(working name — ⚠️ COLLIDES with Assassin's existing
   resource name "Bloodlust," see Open Questions)*
9. Death March
10. Slayer's Rush
11. Evade
12. Whirlwind

**Passive Abilities (3):**
1. Slayer's Oath
2. Precision
3. Merciless

**Evolving Abilities (5):**
1. **Scythe** — the signature Slayer ability (ultimate finisher).
   Evolutions: bigger Execution burst, better scaling, cooler animation.
2. **Flourish** — evolutions: more slashes each evolution, faster
   execution, better Execution generation.
3. **Mark of the Bane** — evolutions: higher damage cap, longer
   duration, stronger mark effects.
4. **Cruel Thrust** — evolutions: stronger bleed, armor
   penetration, better synergy with Scythe.
5. **Cold Blood** — each evolution improves the burst window:
   faster Execution generation, longer duration, reduced cooldown,
   increased lifesteal while active (open question: only if Bloodlust
   stays separate — see note above).

**Mechanics already proven reusable:** Execution/Severance-style
stack-and-consume resource pattern (existing SlayerScythe-adjacent code),
single-target execute-below-threshold logic (Assassin's Execute already
does this).

**Status: ✅ DESIGN LOCKED — full active/passive/evolving kit defined.
Naming collision flagged (Bloodlust), not resolved yet.**

### Valkyrie
**Role:** Hybrid DPS / Support

**Identity:** Holy battlefield commander. Builds Fury while fighting.
Protects allies through divine blessings. Consumes Fury with Ragnarok.

**Resource:** Fury — skill use generates, Ragnarok spends (confirmed built)

**Active Abilities (11 — confirmed intentional, not every class needs
exactly 12):**
1. Glaive Leap
2. Maelstrom
3. Ragnarok
4. Heavenly Strike
5. Spear of Heaven *(ambush/air dive)*
6. Godsfall
7. Dreamslash
8. Bifrost Step
9. Ascending Light
10. Divine Intervention
11. Rally of the Fallen

**Passive Abilities (4 — confirmed intentional):**
1. **Divine Fury** — assails and abilities generate Fury. The more Fury you
   possess, the stronger your Holy abilities become. Ragnarok consumes all
   Fury.
2. **Chooser of the Slain** — defeating a marked enemy restores health and
   empowers you. (Cooldown)
3. **Wings of Stacia** — falling below a health threshold grants a divine
   shield. (Cooldown)
4. **Divine Verdict** — taking damage builds Judgment. When the threshold
   is reached, holy lightning strikes nearby enemies before Judgment
   resets.

**Evolving Abilities (5 — per the explicitly labeled "Final Evolving"
set):**
1. **Ragnarok** — the ultimate payoff of building Fury. Evolutions: bigger
   explosion, better Fury scaling, holy aftermath at max.
2. **Heavenly Strike** — signature attack. Evolutions: Single → Triple →
   Cone → Pillar of Light.
3. **Divine Intervention** — protection evolves throughout the game.
   Evolutions: Self → Ally → Small AoE → Party-wide.
4. **Godsfall** — the ultimate holy AoE. Evolutions: larger radius, more
   damage, holy shockwave, lingering consecrated ground.
5. **Spear of Heaven** — ambush ability. Evolutions: Leap → Bigger impact →
   Holy explosion → Center stun.

**Mechanics already proven reusable:** Fury resource system (built),
Maelstrom/Valkyrie's Call/Glaive Leap already have working precedent in
current code.

**Status: ✅ DESIGN LOCKED — full active/passive/evolving kit defined.
11 active / 4 passive confirmed intentional.**

---

## Strider Path

### Assassin (FINAL)
**Role:** Burst Assassin

**Identity:** Burst Damage, Stealth, Repositioning, Executions, Kill
Chaining, Opportunistic Eliminations

**Gameplay Loop:** Hide → Open → Mark → Burst → Execute → Bloodlust →
Chain Kills

**Active Abilities (12):**
1. **Death's Strike** — instantly appear behind your target and deliver a
   devastating opening strike. Deals increased damage when used from Hide
   or to initiate combat.
2. **Phantom Blade** — slash the target, leaving a lingering shadow wound
   that deals damage over time.
3. **Death Mark** — mark an enemy. If they die before the mark expires,
   nearby allies are healed.
4. **Ghost Step** — drop all threat, enter Hide, and vanish from enemy
   sight. Your next attack from Hide deals bonus damage.
5. **Spectral Wraith** — become a living shadow, striking multiple enemies
   before materializing at the final target.
6. **Eclipse** — unleash an impossibly fast flurry of slashes upon a
   single target.
7. **Death's Conviction** — sacrifice a large portion of your health to
   deal devastating percentage damage.
8. **Killing Intent** — focus entirely on your prey, greatly empowering
   your next damaging ability.
9. **Shadow Reap** — slice all enemies surrounding you with a sweeping
   shadow attack.
10. **Vanishing Slash** — dash through your target with a swift slash
    before instantly returning to Hide, allowing you to reposition for
    your next assassination.
11. **Shadow Clone** — summon a shadow duplicate that fights beside you,
    attacking your enemies and drawing their attention.
12. **Execute** — instantly kill non-boss enemies below a health
    threshold.

**Passive Abilities (3):**
1. **Bloodlust** — kills generate Bloodlust. Upon reaching maximum
   Bloodlust, you automatically enter a killing frenzy. During Bloodlust,
   successful Executes do not trigger their cooldown. *(⚠️ COLLIDES with
   Slayer's "Bloodlust (working name)" active ability — see Open
   Questions, still unresolved.)*
2. **Witness Elimination** — deal increased damage against isolated
   enemies.
3. **Shadowmark** — every third damaging ability against the same target
   increases the damage you deal to them.

**Evolving Abilities (5):**
1. **Spectral Wraith** — evolutions: 2 targets → 4 targets → 8 targets →
   entire screen.
2. **Shadow Clone** — evolutions: one clone → stronger clone → two clones
   → clones inherit a portion of your abilities.
3. **Death Mark** — evolutions: stronger healing → larger healing radius →
   grants a temporary buff after a successful kill.
4. **Death's Conviction** — evolutions: lower health sacrifice → higher
   percentage damage → improved risk/reward → endgame survivability while
   casting.
5. **Execute** — evolutions: 15% HP → 20% HP → 25% HP → 30% HP threshold.

**Mechanics already proven reusable:** Bloodlust/Shadow Frenzy resource
system (already built), Execute-below-threshold logic (already built),
Voidwalker/Coup de Grâce repositioning (already built, relevant to Death's
Strike/Vanishing Slash), Shadow Clone has direct precedent via Trickster's
Decoy/Mirror Image.

**Status: ✅ DESIGN LOCKED (FINAL) — full active/passive/evolving kit
defined, including gameplay loop.**

### Trickster (FINAL)
**Role:** Control / Utility

**Identity:** Deception, Battlefield Manipulation, Mental Warfare,
Illusions, Traps, Crowd Control

**Class Fantasy:** The Trickster doesn't overpower enemies — it outsmarts
them. Make monsters attack each other. Blind key threats. Hide the entire
party. Fill the battlefield with illusions. Control enemy positioning.
Then end the performance with Grand Finale, converting all that chaos into
a devastating burst.

**Gameplay Loop:** Manipulate → Afflict → Confuse → Control → Grand Finale.
Unlike Assassin, Trickster isn't trying to kill enemies quickly — it wins
by making enemies lose control of the battlefield before finishing them
with a devastating payoff.

**Active Abilities (12):**
1. **Shadow Step** — instantly teleport behind a target.
2. **Vanishing Act** — vanish from sight before reappearing a short
   distance away, escaping danger.
3. **Crack the Whip** — crack your enchanted whip in a wide arc, damaging
   enemies in front of you and briefly staggering them.
4. **Blackout** — blind an enemy, severely reducing their ability to
   attack.
5. **Delirium** — shatter an enemy's mind, causing them to attack random
   nearby creatures.
6. **Puppeteer** — take complete control of an enemy, forcing them to
   fight for your side.
7. **Hall of Mirrors** — create illusionary copies of yourself that
   confuse enemies and draw their attacks.
8. **Web Trap** — place an invisible trap that ensnares enemies in an
   area, rooting them in place.
9. **Curtain Call** — cloak your entire party in illusion magic, causing
   all allies to vanish from enemy sight and immediately drop aggro.
10. **Switcheroo** — instantly swap positions with an ally or enemy.
11. **Smoke Bomb** — fill the battlefield with thick smoke, causing
    enemies to lose sight of their targets.
12. **Grand Finale** — consume every mental affliction affecting nearby
    enemies, ending their effects and dealing devastating damage based on
    the number and variety of afflictions removed.

**Passive Abilities (3):**
1. **Smoke and Mirrors** — whenever you use a mobility or deception
   ability, leave behind an illusion that briefly distracts nearby
   enemies.
2. **Chain Reaction** — mental afflictions have a chance to spread to
   nearby enemies or trigger another random mental affliction when they
   expire.
3. **Psychological Warfare** — enemies suffering from mental afflictions
   take increased damage from all allies.

**Evolving Abilities (5):**
1. **Delirium** — evolutions: longer duration → larger radius → stronger
   confusion → entire groups descend into madness.
2. **Puppeteer** — evolutions: longer control → stronger controlled
   targets → elite enemies → multiple controlled enemies.
3. **Hall of Mirrors** — evolutions: more illusions → longer duration →
   illusions attack → illusions mimic selected abilities.
4. **Curtain Call** — evolutions: longer Hide → larger radius → movement
   speed bonus → leaves illusionary decoys behind.
5. **Grand Finale** — evolutions: higher damage scaling → larger radius →
   more damage per affliction consumed → massive endgame payoff.

**Signature Afflictions:** Blind, Delirium (attack allies/random targets),
Control (Puppeteer), Target Loss, Root, Hide/Aggro Manipulation.

**Mechanics already proven reusable:** Decoy/Mirror Image (built, relevant
to Hall of Mirrors), Poison Bomb/Smoke Bomb/Delirium/Blackout (already
built, direct name matches), Mana Burst (built, MP-scaled AoE — relevant
mechanic for Grand Finale's "damage scales with consumed effects" concept).

**Status: ✅ DESIGN LOCKED (FINAL) — full active/passive/evolving kit
defined, including gameplay loop and class fantasy.**

### Fletcher *(renamed from Archer)*
**Role:** Ranged DPS

**Identity:** Master Marksman

**Gameplay Loop:** Position → Empower → Control → Execute

**Active Abilities (12):**
1. **Arrow Shot** *(Assail)* — bread-and-butter ranged attack.
2. **Arrowstep** — dash a short distance while maintaining aim.
3. **Focus** — greatly empowers your next ranged attack.
4. **Pinpoint Shot** — heavy single-target damage that briefly stuns the
   target.
5. **Flechette** — piercing arrow that tears through enemies.
6. **Warden's Net** — net arrow that evolves into a small AoE snare.
7. **Gravity Arrow** — pull nearby enemies toward the impact point.
8. **Spotter's Brand** — marks an enemy, increasing the critical strike
   chance they receive from you and your allies.
9. **Moonfall** — fire an arrow skyward that returns as celestial arrows
   striking multiple enemies.
10. **Valkor's Volley** — call upon Valkor, unleashing a divine rain of
    arrows upon a target area. *(Ties directly into the Valkor pantheon
    lore.)*
11. **Fulmination** — fire a legendary lightning arrow that pierces
    everything in a straight line.
12. **Multishot** — fire a rapid succession of arrows into a single
    target, each dealing reduced damage.

**Passive Abilities (3):**
1. **Eagle Eye** — the farther your arrows travel before striking their
   target, the more damage they deal.
2. **Phantom Quiver** — every fourth ranged attack conjures a spectral
   arrow that strikes the same target, dealing additional damage.
3. **Windrunner** — after using Arrowstep, your next ranged attack deals
   increased damage and gains additional critical strike chance.

**Evolving Abilities (5):**
1. Focus
2. Flechette
3. Warden's Net
4. Moonfall
5. Valkor's Volley
*(Evolution specifics not detailed yet for this class — flag if you want
to fill these in like the other classes' evolving sections.)*

**Mechanics already proven reusable:** bow_assail/arrowstep/fire_shot/
deadcenter/trappers_net/rain_of_arrows/flechette/focus/precise_shot
(largest existing built kit of any class, 9 abilities, direct name matches
on Arrowstep/Flechette/Focus already existing).

**Status: ✅ DESIGN LOCKED — full active/passive kit defined. Evolving
ability specifics (the "what changes per tier" detail other classes have)
not yet written. Class renamed Archer → Fletcher, confirmed final.**

---

## Magus Path

### Sorcerer — LOCKED CANON *(full document, uploaded as
Sorcerer_LOCKED.md, integrated here)*
**Role:** Elemental Mage

**Identity:** Sorcerers begin with basic Arcane magic, then choose two
elemental disciplines. The first choice determines the Tier II
foundation; the second determines the Tier III mastery block; the
combination determines the Tier IV specialization. Order matters during
progression — Fire→Earth and Earth→Fire both eventually reach Magma, but
via different spell histories (different Tier II/III blocks along the
way), so two players can reach the same final specialization through
different journeys.

**Structural rule (every final Sorcerer build):** 12 active spells, 3
passives, 5 evolving spells, 4 progression tiers (3 active + 1 passive at
Tiers I–III, 3 signature actives at Tier IV with no passive).

**Confirmed cross-class insight:** the Tier I–IV floor-tied evolving
schedule described below isn't Sorcerer-specific — it's the general
evolving-ability mechanic used across every class in this document
(confirmed by the user). Martial Artist's Form transformations are the
one deliberate exception to this pattern.

**Evolving Ability Schedule — confirmed meaning:** "Tier I/II/III/IV" in
the schedule below refers to BOTH when a spell is first learned (which
structural Tier block it comes from) AND how it evolves through 4 power
stages — the two are the same axis, not separate systems.

| Floor | Evolving #1 | Evolving #2 | Evolving #3 | Evolving #4 | Evolving #5 |
|---:|---|---|---|---|---|
| 1 | Tier I | — | — | — | — |
| 2 | Tier I | — | — | — | — |
| 3 | Tier II | Tier I | — | — | — |
| 4 | Tier III | Tier II | — | — | — |
| 5 | Tier IV | Tier III | Tier I | — | — |
| 6 | — | Tier IV | Tier II | Tier I | — |
| 7 | — | — | Tier III | Tier II | Tier I |
| 8 | — | — | Tier IV | Tier III | Tier II |
| 9 | — | — | — | Tier IV | Tier III |
| 10 | — | — | — | — | Tier IV |

*(A blank means that evolving spell has already reached its final tier.
Note: only Arcanist evolves a genuine Tier I ability — Arcane Gate. Every
other specialization's 5 evolving spells are 2 from Tier II + 1 from Tier
III + 2 from Tier IV, per each element's own evolving-spells list below.)*

**Ground Targeting — confirmed scope note:** ground-targeted casting is
"supported through Claw" — confirmed this refers to the ground-targeted
casting system built earlier this session (the GroundTargeted flag,
point-based ActivationContext, new packet). **Important — this is a much
bigger ask than what was actually built:** tonight's work was a minimal
proof-of-concept (one test spell, a simple circle AoE, reusing the
existing tile-cursor as a placeholder telegraph — real shape/wall/zone
rendering was explicitly deferred). Sorcerer's design here assumes a full
system: persistent damage fields, walls/barriers, movable/directional
zones, targeted pulls and knockbacks, delayed impacts, area denial, and
environmental spell combinations (e.g. Wind repositioning Fire effects).
This is real, substantial follow-on engineering work, not something
already covered by tonight's slice — flagged clearly so it's not assumed
done.

---

**Shared Tier I — Arcane Fundamentals (every Sorcerer):**
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Arcane Bolt | No | Fire a basic projectile of pure Arcane energy. |
| Active | Arcane Gate | Arcanist only | Teleport to a selected location. Arcanist is the only specialization that continues evolving this Tier I spell. |
| Active | Shadow Bolt | No | Launch a basic bolt of dark energy — every Sorcerer's early non-elemental attack. |
| Passive | Arcane Precision | — | The first spell cast against an enemy deals increased damage. |

---

**🔥 Fire Spell Blocks**

Tier II — Fire Foundation:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Firebolt | Yes | Launch a fire projectile. |
| Active | Solar Flare | No | Empower yourself with solar fire, temporarily increasing Fire damage and Burn application. |
| Active | Fire Wall | Yes | Create a wall of flame on the ground. Enemies crossing it take Fire damage and may be Burned. |
| Passive | Kindling | — | Fire spells have a chance to apply Burn. |

Tier III — Fire Mastery *(⚠️ Heat Wave REMOVED per resolved collision —
replaced with Combustion below)*:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Flame Lance | Yes | Fire a concentrated lance of flame that pierces enemies in a line. |
| Active | Fire Shield | No | Surround yourself with a protective flame barrier. Absorbs damage, erupts when broken/expired, damaging and Burning nearby enemies. |
| Active | **Combustion** *(NEW — replaces Heat Wave, locked this session)* | No | Detonates all Burn stacks currently on the target for a burst of damage, consuming them. Creates a mini-loop with Kindling (applies Burn) and Scorch (rewards hitting Burned targets) — build stacks, then cash them in. |
| Passive | Scorch | — | Deal increased damage to enemies affected by Burn. |

**Fire mechanic — Burn:** Fire's core status. Deals Fire damage over
time, applied via Kindling, enables Scorch, may be refreshed/spread by
advanced Fire abilities, and can now be actively consumed by Combustion.
Exact duration/stacking/damage values not yet locked.

**🔥 Ignis — Pure Fire**
Identity: Burn, sustained destruction, explosive Fire mastery, survival
through Phoenix power.
Tier IV — Ignis:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Meteor | Yes | Call down a devastating meteor at a selected location after a short delay. |
| Active | Ember Field | Yes | Ignite a large targeted area, creating persistent burning ground. |
| Active | Phoenix Rise | No | When fatal damage would kill the Sorcerer, revive at ~30% Health and release a Fire explosion around the caster. |

Ignis evolving spells: Firebolt, Fire Wall, Flame Lance, Meteor, Ember
Field.

---

**🪨 Earth Spell Blocks**

Tier II — Earth Foundation:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Earth Spike | Yes | Cause stone to erupt beneath a selected target or ground location. |
| Active | Earthen Grip | Yes | Summon stone hands that root enemies in place. |
| Active | Earthworks | No | Place destructible stones/barricades on the battlefield. Hinder movement, provide temporary cover, not permanent terrain. |
| Passive | Crushing Force | — | Deal increased damage to crowd-controlled enemies. |

Tier III — Earth Mastery:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Earthquake | Yes | Shake a targeted area, damaging and slowing enemies caught within it. |
| Active | Stone Lance | No | Launch a massive stone spear that pierces enemies in a line. |
| Active | Stone Skin | No | Harden your body with stone, greatly increasing physical and magical defenses for a short duration. |
| Passive | Stoneheart | — | Earth spells have a chance to apply Shattered. |

**🪨 Earthshaper — Pure Earth**
Identity: Terrain creation, crowd control, resilience, tectonic force.
Tier IV — Earthshaper:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Sinkhole | Yes | Collapse the ground at a selected point, pulling enemies toward the center before swallowing/damaging them. |
| Active | Tectonic Judgment | Yes | Cause stone pillars to erupt in sequence across a targeted area. Powerful, not an instant screen-clear. |
| Active | Worldbreaker | No | Tear open the battlefield with a colossal earth-shattering attack. |

Earthshaper evolving spells: Earth Spike, Earthen Grip, Earthquake,
Sinkhole, Tectonic Judgment.

---

**💧 Water Spell Blocks**

Tier II — Water Foundation:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Hydro Burst | Yes | Fire a concentrated burst of pressurized water that damages and slows enemies. |
| Active | Healing Waters | No | Apply restorative water to yourself, healing over time. |
| Active | Bubble Block | Yes | Encase an enemy in a sphere of water, briefly preventing movement and actions. |
| Passive | Flow State | — | Casting Water spells restores a small amount of Mana. |

Tier III — Water Mastery:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Whirlpool | Yes | Create a ground-targeted whirlpool that pulls enemies toward its center. |
| Active | Tidal Wave | No | Send a wave forward, dealing Water damage and pushing enemies along its path. |
| Active | Riptide | No | Mark an enemy so subsequent Water spells deal increased damage to that target. |
| Passive | Ocean's Blessing | — | Healing Waters has a chance to splash to a nearby ally. |

**💧 Hydrosage — Pure Water**
Identity: Ocean power, sustain, displacement, overwhelming tides.
Tier IV — Hydrosage:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Tsunami | Yes | Summon a devastating tsunami at a targeted location. |
| Active | Mermaid's Call | Yes | Summon spectral mermaids whose song and rushing water attack enemies — an attacking spell, not healing/charm. |
| Active | Deluge | No | Blanket a large area in torrential rain, slowing enemies and empowering Water spells cast within the storm. |

Hydrosage evolving spells: Hydro Burst, Bubble Block, Whirlpool, Tsunami,
Mermaid's Call.

---

**🌬 Wind Spell Blocks**

Tier II — Wind Foundation:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Wind Slash | Yes | Launch a blade of compressed wind that slices through enemies. |
| Active | Eye of the Storm | Yes | Temporarily increase movement speed, casting speed, and storm-related offensive power. |
| Active | Wind Wall | No | Create a wall of wind that slows enemies passing through it and weakens/deflects projectiles. |
| Passive | Killing Winds | — | Wind spells deal increased damage to enemies at or below 50% Health. |

Tier III — Wind Mastery:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Lightning Bolt | Yes | Call down a powerful bolt of lightning on a target. |
| Active | Gale Force | No | Unleash a powerful targeted gust that pushes enemies away. |
| Active | Storm Shield | No | Surround yourself with swirling winds that absorb damage, releasing a burst when the shield expires. |
| Passive | Critical Current | — | Wind spells are allowed to critically strike. |

**🌬 Tempest — Pure Wind** *(⚠️ naming note: shares "Tempest" with the
Martial Artist Harpy specialization — different systems, worth knowing
both exist)*
Identity: Speed, execution, lightning, large-scale storm mastery.
Tier IV — Tempest:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Hurricane | Yes | Summon a massive hurricane that continuously pulls enemies toward its center. |
| Active | Stormcall | Yes | Mark a large targeted area where repeated lightning strikes fall from above. |
| Active | Tornado | No | Create a colossal moving tornado that travels across the battlefield and carries enemies along its path. |

Tempest evolving spells: Wind Slash, Eye of the Storm, Lightning Bolt,
Hurricane, Stormcall.

---

**✨ Arcane Spell Blocks**

Tier II — Arcane Foundation:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Arcane Starburst | No | Release multiple Arcane projectiles at nearby enemies. |
| Active | Mor Pian na Dion | Yes | Enter a heightened magical state that increases Spell Power and reduces Mana costs. |
| Active | Mana Siphon | No | Drain Mana from an enemy and restore your own reserves. |
| Passive | Mystic Efficiency | — | Arcane spells cost less Mana. |

Tier III — Arcane Mastery:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Null Zone | Yes | Create a ground-targeted field that suppresses enemy magic and weakens magical effects within it. |
| Active | Arcane Shield | No | Form a barrier of Arcane energy that absorbs incoming damage. |
| Active | Singularity | No | Collapse Arcane energy into a point, pulling enemies toward the center. |
| Passive | Arcane Overflow | — | Arcane spells deal increased damage while the caster remains above 80% Mana. |

**✨ Arcanist — Pure Arcane**
Identity: Mana control, anti-magic, cooldown manipulation, teleportation,
absolute magical defense. The ONLY specialization that evolves a Tier I
ability (Arcane Gate).
Tier IV — Arcanist:
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Arcane Corruption | Yes | Corrupt a selected area with dark Arcane energy, dealing continuous damage to enemies standing on it. |
| Active | Spell Reset | Yes | Instantly refresh the cooldown of the previously cast spell, allowing immediate reuse. |
| Active | **Stacia's Aegis** | No | Surround yourself with divine Arcane armor that negates incoming damage for a short duration. *(Ties into pantheon lore — Stacia associated with grace/hope/protection/healing/light.)* |

Arcanist evolving spells: Arcane Gate (Tier I), Mor Pian na Dion (Tier
II), Null Zone (Tier III), Arcane Corruption (Tier IV), Spell Reset
(Tier IV).

---

**Hybrid Construction Rules:** a hybrid uses Shared Tier I + Tier II from
its FIRST chosen element + Tier III from its SECOND chosen element +
unique hybrid Tier IV. Example: Fire→Earth Magma uses Fire Tier II +
Earth Tier III; Earth→Fire Magma uses Earth Tier II + Fire Tier III. Both
reach the same Magma Tier IV, but their first 9 abilities differ.

**🌋 Magma — Fire + Earth**
Identity: Volcanoes, molten terrain, eruption, area denial.
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Volcanic Eruption | Yes | Cause a selected area to erupt with molten rock, leaving lava behind. |
| Active | Magma Prison | Yes | Trap enemies inside a ring/enclosure of molten stone. |
| Active | Caldera | No | Transform a large area into an active volcanic crater — continuous damage, periodic eruptions. |

**♨️ Cinder — Fire + Water** *(locked mapping, must not reverse)*
Identity: Steam, scalding pressure, superheated water, mixed Fire/Water
damage.
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Scalding Mist | Yes | Create a cloud of burning steam that continuously damages enemies within it. |
| Active | Geyser | Yes | Erupt a superheated geyser beneath a target location, damaging and launching enemies. |
| Active | Cinder Rain | No | Rain superheated embers and steam over a large area. |

**🔥🌬 Inferno — Fire + Wind** *(locked mapping, must not reverse)*
Identity: Wildfire, moving flames, wind-fed combustion, aggressive area
damage. Signature interaction: Wind can reposition Fire — intended
fantasy includes blowing/pushing existing flame effects across the
battlefield.
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Firestorm | Yes | Summon a moving firestorm that travels across the battlefield. |
| Active | Conflagration | Yes | Ignite a targeted area in an uncontrollable blaze that spreads outward. |
| Active | Heat Wave | No | Release a powerful wave of scorching heat that damages and ignites enemies. |

**🟤 Torrent — Earth + Water**
Identity: Mud, erosion, waterlogged terrain, heavy battlefield control.
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Sinkhole | Yes | Collapse the ground into a waterlogged sinkhole that pulls enemies toward its center. |
| Active | Mudslide | Yes | Send a wave of mud forward, pushing enemies and leaving difficult terrain. |
| Active | Bog | No | Transform a large area into a deep bog — heavy slow, repeated damage. |

**🏜️ Sirocco — Earth + Wind**
Identity: Sand, dust, desert winds, blindness, roaming storms.
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Sandstorm | Yes | Summon a swirling sandstorm that damages enemies and reduces their accuracy. |
| Active | Dust Devil | Yes | Create a smaller roaming whirlwind of sand that pursues/disrupts enemies. |
| Active | Desert's Wrath | No | Blanket a large area in scorching wind and sand — damage, slow, blind. |

**❄️ Blizzard — Water + Wind**
Identity: Ice, freezing, snowstorms, frozen terrain, battlefield lockdown.
Blizzard owns the ice fantasy so Hydrosage stays pure Water.
| Type | Ability | Evolves | Description |
|---|---|:---:|---|
| Active | Glacial Barrier | Yes | Raise a destructible wall of ice that blocks movement and projectiles. |
| Active | Frost Nova | Yes | Instantly erupt freezing energy around a point — damage + freeze nearby enemies. |
| Active | Absolute Zero | No | Sustained blizzard over an area — repeated damage/slow, escalating to frozen solid. |

Frost Nova (immediate, burst, small area, reactive) vs. Absolute Zero
(sustained, large area, ultimate battlefield-control zone) — deliberately
differentiated.

---

**Pure Specialization Summary:**
| Specialization | Elements | Tier IV |
|---|---|---|
| Ignis | Fire + Fire | Meteor, Ember Field, Phoenix Rise |
| Earthshaper | Earth + Earth | Sinkhole, Tectonic Judgment, Worldbreaker |
| Hydrosage | Water + Water | Tsunami, Mermaid's Call, Deluge |
| Tempest | Wind + Wind | Hurricane, Stormcall, Tornado |
| Arcanist | Arcane + Arcane | Arcane Corruption, Spell Reset, Stacia's Aegis |

**Hybrid Specialization Summary:**
| Specialization | Elements | Tier IV |
|---|---|---|
| Magma | Fire + Earth | Volcanic Eruption, Magma Prison, Caldera |
| Cinder | Fire + Water | Scalding Mist, Geyser, Cinder Rain |
| Inferno | Fire + Wind | Firestorm, Conflagration, Heat Wave |
| Torrent | Earth + Water | Sinkhole, Mudslide, Bog |
| Sirocco | Earth + Wind | Sandstorm, Dust Devil, Desert's Wrath |
| Blizzard | Water + Wind | Glacial Barrier, Frost Nova, Absolute Zero |

---

**Implementation Notes (potential custom server work):** Burn status,
Shattered status, destructible Earthworks, projectile deflection/
weakening from Wind Wall, enemy pulling (Whirlpool/Sinkhole/Hurricane/
Singularity), persistent ground fields, delayed ground-targeted impacts,
Spell Reset tracking the previously cast spell, Stacia's Aegis damage
negation, moving Tornado/Firestorm effects, Mermaid's Call spectral
attack behavior, Arcane Gate evolution, pure-vs-hybrid tuning of Tier III
blocks. Ground targeting's foundational packet/mechanism already exists
(this session's minimal slice) — everything else on this list is new
work.

**Remaining Decisions (not yet numerically locked):** Mana costs,
cooldowns, exact damage values, exact scaling formulas, effect radii,
Burn stacking behavior, Shattered's exact defense reduction, crowd-
control durations, pure-vs-hybrid Tier III power differences, evolution
bonuses for each Tier I–IV form, whether persistent fields can be moved
by Wind in the first implementation.

**Cross-reference to existing built code:** Wind Slash, Earth Spike, and
Lightning Bolt are direct name matches to Sorcerer spells already built
in the current codebase (per ABILITY_DATABASE.md) — real reusable
precedent. The old built "Fire Breath" and "Frost Bolt" names don't
appear in this new design at all (replaced by Firebolt and Hydro Burst
respectively) — expect those two to need renaming/replacement work, not
direct reuse.

**Future idea — NOT locked, deliberately deferred:** an "Elementalist"
path — learn 1 Tier from all 4 elements instead of 2 tiers from 2
elements, forgoing Arcanist's Tier I evolution (Arcane Gate) as the cost.
Breaks the standard "2 elements × 2 tiers" shape every other path
follows, so a full Tier III/IV for this doesn't exist and isn't planned —
if built, the simplest version stops at Tier II (Arcane Tier I minus Gate
evolution, + Tier II from all 4 elements, no Tier III/IV, no combination
spell). Trades late-game power ceiling for early breadth. Candidate for
a later addition once the 11 core paths are built and tested — not part
of current canon.

**Status: ✅ DESIGN LOCKED CANON — the most complete class document in
the whole project. Fire Tier III's Heat Wave collision resolved (replaced
with Combustion, a new Burn-consuming finisher). Tier IV counts verified
correct (3 actives, 2 evolving, per specialization) — no discrepancy
found. Remaining gaps are explicitly numerical (costs/cooldowns/damage),
not structural.**

### Mystic — LOCKED
**Role:** Support / Debuffer

**Identity:** Spiritual manipulation. Communion, curses, tethers and
dimensional magic. *(from original locked design doc)*

**Design philosophy (confirmed):** Mystic contributes meaningful damage
and setup without ever competing with Sorcerer's raw DPS, while still
having enough healing and utility to feel like a true support.

**Active Abilities (12 — cut from an original 13; Void Communion removed
as the most redundant/least-defined ability, per discussion. Flat list,
no pillar categorization — matches the format used by every other class):**
1. Spirit Burst
2. Unravel
3. Stasis
4. Regression
5. Blooming Life
6. Stacia's Shrine
7. Stacia's Pulse
8. **Spirit Rend** *(formerly "Fas")* — evolves through BEHAVIOR, not just
   numbers:
   - Early: the next magical hit against the target deals increased
     damage. Great for setting up a Sorcerer burst.
   - Mid: for 5 seconds, magical attacks against the target deal
     increased damage — now the whole party can capitalize.
   - Late: for 8–10 seconds, the target's soul is exposed, increasing
     magical damage taken AND amplifying magical damage-over-time
     effects. Now it's a true raid support spell.
9. Soul Tether
10. Communion Rite
11. Ethereal Step
12. **Stacia's Judgment** — call upon Stacia to strike enemies with divine
    spirit energy. Deliberately kept as PURE OFFENSE — Mystic already has
    3 dedicated healing tools (Blooming Life/Shrine/Pulse), so Judgment
    doesn't need to heal at all. Locked evolution path (double-strike
    version, chosen over an alternate "consecrated ground" version that
    was considered and rejected):
    - Lv. 1: single impact
    - Lv. 25: larger impact
    - Lv. 50: higher damage
    - Lv. 75: leaves a lingering spirit field
    - Lv. 100: Judgment strikes TWICE — falls, booms, then a second boom
      ~2 seconds later. Gives it weight, feels like divine judgment rather
      than just another AoE nuke.

**Passive Abilities (3):**
1. Bloomkeeper
2. Spiritual Attunement
3. Spirit Overflow

**Evolving Abilities (5):**
1. Blooming Life
2. Spirit Rend
3. Stacia's Shrine
4. Communion Rite
5. Stacia's Judgment
*(Descriptions still needed: Spirit Burst, Unravel, Stasis, Regression,
Blooming Life, Stacia's Shrine, Stacia's Pulse, Soul Tether, Communion
Rite, Ethereal Step — only Spirit Rend and Stacia's Judgment are fully
detailed so far.)*

**Mechanics already proven reusable:** Stacia's Shrine, Stacia's Pulse,
and Stacia's Judgment are direct name matches to abilities already built
in the current codebase (per ABILITY_DATABASE.md). Stasis also has direct
precedent (freeze + invulnerable AoE, already built).

**Status: ✅ DESIGN LOCKED — 12-ability active list finalized (Void
Communion cut). 2 of 12 actives fully detailed (Spirit Rend, Stacia's
Judgment); the rest still need descriptions.**

### Bard — The Divine Conductor — LOCKED (FINAL)
**Role:** Support

**Identity:** Buffs, healing, songs and battlefield support. *(from
original locked design doc)*

**Class Identity — The Ultimate Party Support:** best burst healer, best
party buffer, best defensive support, best traditional curser, iconic
raid-saving cooldown (Guardian's Anthem).

**Three support philosophies (confirmed, cross-class design note):**
- 🎵 **Bard** — Protect the party.
- 🌿 **Mystic** — Protect the individual.
- 🔮 **Sorcerer** — Destroy the enemy.

**Bard vs. Mystic — differentiation:**
| Bard | Mystic |
|---|---|
| Burst healing | Heal over time |
| Blessings | Soul manipulation |
| Party-wide buffs | Single-target support |
| Defensive cooldowns | Battlefield control |
| Traditional curses | Magical vulnerability |
| Resurrection, cleansing | — |

**Active Abilities (12 — FINAL, replaces all earlier drafts including the
now-cut "Skills" — Sacred Chord/Inspiring Presence/Dazzling Verse/
Resonance did not survive to the final list):**

| Spell | Role | Closest Retail (USDA) Spell |
|---|---|---|
| Stacia's Vitae | Powerful single-target burst heal | Beag/Mor Beannaich |
| Stacia's Chorus | Chain heal that bounces between allies | New |
| Stacia's Cleanse | Removes all negative effects from an ally | Ao-prefix cleanse spells (Ao Cradh, Ao Dall, etc.) |
| Stacia's Bubble | Shields an ally, absorbing incoming damage | New |
| Red Requiem | Revives a fallen ally | Aiseag Spiorad (Resurrection) |
| Stacia's Lullaby | Puts an enemy to sleep until damaged | Pramh |
| Stacia's Blessing ⭐ | Evolving defensive blessing (Armor + Blessing + Veil) | Armachd + Naomh Aite + Beannaich |
| Battle Hymn ⭐ | Evolving offensive party buff (Attack + Accuracy + Speed + Crit) | Fas Deireas + Beannaich |
| Cradh ⭐ | Evolving curse, increasingly weakens enemies | Cradh |
| Salvation ⭐ | Evolving emergency healing miracle | Salvation |
| Guardian's Anthem ⭐ | Evolves single-target immunity → full party immunity | Mor Dion Comhla |
| **Valkor's Smite** *(new)* | Divine offensive strike — Bard's one call on the OTHER god, contrasting its otherwise all-Stacia kit | — |

**Evolving Abilities (5):**
1. **Stacia's Blessing** — evolution track: Defense → +HP → +Stats →
   +Damage Reduction → +CC Resistance.
2. **Battle Hymn** — evolution track: Attack → +Accuracy → +Attack Speed
   → +Critical → +Mana Regeneration.
3. **Cradh** — evolution track: Defense reduction → +Damage reduction →
   +Accuracy reduction → +Critical vulnerability. Detailed tier version:
   - Early: reduce target's defense by 10%.
   - Mid: reduce defense by 20%, slightly lower damage dealt.
   - Late: reduce defense by 30%, lower damage dealt, small chance for
     attacks against the target to critically strike.
4. **Salvation** — evolution track: Large heal → Stronger heal → Splash
   healing → Better cooldown → End-game emergency miracle.
5. **Guardian's Anthem** — the iconic raid-saving cooldown:
   - Lv. 1: protect one ally, 3 seconds, 100% damage immunity.
   - Mid: one ally, longer duration.
   - Mid+: small area around the target, full immunity.
   - Late: larger area.
   - Max: entire party, 5–6 seconds, 100% damage immunity.

**Passive Abilities (3 — FINAL, resolves the earlier "no passives
defined" gap):**
1. **Crescendo** — casting spells builds Crescendo. At maximum stacks,
   your next spell is empowered.
2. **Encore** — beneficial spells have a chance to repeat at reduced
   effectiveness.
3. **Stacia's Grace** — when your Health falls below a threshold, Stacia
   intervenes, granting brief 100% damage immunity (long internal
   cooldown).

**Mechanics already proven reusable:** Stacia's Vitae, Red Requiem,
Stacia's Blessing, Stacia's Hymn/March (→Battle Hymn), Stacia's Veil
(→Stacia's Blessing), Stacia's Bubble, Stacia's Cleanse, Stacia's Chorus,
Stacia's Lullaby, Grand Finale are all direct name matches to abilities
already built in the current codebase (per ABILITY_DATABASE.md) — largest
existing overlap of any class.

**Status: ✅ DESIGN LOCKED (FINAL) — full 12-ability active list,
3 passives, 5 evolving abilities, retail cross-reference, and identity
comparison all defined.**

---

## Martial Artist
**Role:** Adaptive Fighter

**Identity:** At Floor 2, chooses one specialization: Fighter, Tank, or
Ranged/Chi. Each specialization has its own evolving abilities. Progression
schedule is unique, does NOT follow the normal 5-evolving-ability pattern
other classes use.

**Resource:** Chi — hitting + Meditate generates, Beast Form drains
(confirmed built)

**Shared Foundation (5 abilities, every Martial Artist has these before
Floor 2 specialization) — LOCKED:**

**Floor 1 (3 abilities):**
| Ability | Type | Description |
|---|---|---|
| Tiger Strike *(working name)* | Skill | A fast melee punch, primary attack. |
| Roundhouse Kick *(working name)* | Skill | A powerful kick that damages enemies in front of you. |
| Battle Focus *(working name — ⚠️ naming still open, see below)* | Skill | Enter a focused stance, increasing your damage for a short duration. |

**Floor 2 (2 abilities):**
| Ability | Type | Description |
|---|---|---|
| Meditate *(Evolving — see full tiers below)* | Spell | Restore Chi/Mana over time and prepare for battle. |
| Beast Form I *(working name)* | Spell | Transform into your martial form, gaining new bonuses. Evolves at Floors 4, 6, 8, and 10 — NOT the standard 5-tier evolving pattern, floor-gated instead. |

**Meditate + Beast Form — combined evolving progression (SUPERSEDES the
earlier separate 5-tier tables for each):**
| Floor | Meditate | Form |
|---|---|---|
| 2 | I | I |
| 4 | II | II |
| 6 | III | III |
| 8 | IV | III (Mastered Meditate) |
| 10 | IV | IV (Final Form) |

*(Note: Meditate caps at Level IV — no Level V. Beast Form caps at Form
IV ("Final Form") — no Form V. Form III repeats across Floors 6 and 8. The
"(Mastered Meditate)" label on Floor 8's Form cell is preserved exactly as
given — slightly ambiguous phrasing since it references Meditate inside
the Form column, but kept as-is per explicit confirmation this table
supersedes the earlier version.)*

**Confirmed visual design — per-tier form colors:** each Form evolution
tier (I through IV/V per specialization) has a DIFFERENT color, not one
fixed palette across all tiers. Applies to all three specializations
(Beast, Ironscale, Tempest) — confirmed, not Tempest-specific. Exact color
sequence per tier not decided yet. Relevant to the sprite pipeline/palette
work already proven viable this session (the mana-orb runtime palette-
remap technique) — likely the same kind of approach applies here once the
actual color sequence is designed.

**Confirmed gameplay loop (from the self-buff design):** Battle Focus →
Punch → Kick → Punch. Simple, satisfying, teaches the fantasy immediately.

**⚠️ Open naming decision:** the Floor 1 damage-buff stance should be a
themed martial arts stance rather than a generic buff name. Candidates
proposed: Battle Focus, Fighting Spirit, Inner Strength, Combat Trance.
Not decided yet.

**Specializations (LOCKED — names/forms/roles/fantasy confirmed, full
7-active + 3-passive kits not designed yet):**

| Specialization | Form | Role | Fantasy |
|---|---|---|---|
| 🐺 Beast | Werewolf | Fighter / DPS | Feral, relentless, brutal melee combat. |
| 🦎 Ironscale | Lizardman | Tank | Unbreakable guardian with counters and crowd control. *(renamed from Titan)* |
| 🦅 Tempest | Harpy | Ranged DPS | Agile aerial fighter using wind, feathers, and chi projectiles. |

**⚠️ Open question:** the existing built Beast Form (3 variants: Fenrir/
Celestial/Basilisk, per ABILITY_DATABASE.md) — does this old system get
fully superseded by the new Beast/Titan/Tempest specializations above, or
does the shared Floor-2 "Beast Form I" (which evolves at Floors 4/6/8/10)
still exist independently, with each of Beast/Titan/Tempest ALSO having
their own transformation on top of it? Not assumed — worth clarifying
before implementation.

**Ability naming:** deliberately deferred until the actual 7-active + 3-
passive kits are designed for each specialization — naming should follow
from what abilities actually do, not be decided in the abstract.

**Structure going forward:** 5 shared abilities + 7 specialization-unique
actives (per Fighter/Tank/RangedChi) = 12 total actives, matching the
template every other class uses. The 7 per-spec actives + 3 passives per
specialization still need full design — this is the largest remaining
gap in the whole class-design project.

**Mechanics already proven reusable:** Chi resource system (built,
MartialArtistChiScript.cs), Beast Form (3 variants already built: Fenrir/
Celestial/Basilisk — worth checking whether these map onto the 3
specializations directly, e.g. Fenrir→Fighter, Basilisk→Tank, or if
Beast Form is meant to be shared/evolving independent of spec choice).

#### 🐺 Beast (Werewolf) — Fighter/DPS Specialization — LOCKED
*(Specialization of Martial Artist — NOT a separate class. All three
specializations below belong to the single Martial Artist class.)*

**Universal (shared foundation, 5 abilities — see Shared Foundation
above for full detail):** Tiger Strike, Roundhouse Kick, Battle Focus,
Meditate (Evolving), Beast Form (Evolving).

**Specialization Actives (7):**
| Ability | Evolves | Description |
|---|---|---|
| Alpha Strike | ⭐ | Strike multiple enemies, ending behind the final target. |
| Howling Sequence | ⭐ | Rapid multi-hit claw combo. |
| Perfect Form | ⭐ | Greatly increase Attack Speed. |
| Break | — | Target takes increased damage for a short duration. |
| Predator's Pounce *(Wolf Fang)* | — | Heavy strike that stuns the target. |
| Blood Sacrifice *(Kelberoth Strike)* | — | Sacrifice Health to deal massive damage based on the Health sacrificed. |
| Traverse Punch | — | Dash behind the target and strike. |

**Passive Abilities (3):**
1. **Bone Memory** — consecutive attacks gradually increase your damage.
2. **Survival Instinct** — taking damage temporarily increases your
   damage resistance.
3. **Bloodlust** — basic attacks restore a small amount of Health.
   *(⚠️ THIRD use of this name across the class set — already Assassin's
   core resource/passive AND Slayer's working-name active. Growing
   collision pattern, not urgent but needs a dedicated resolution pass
   across all three at some point.)*

**Evolving Abilities (5, combining shared + specialization):**
1. Meditate *(shared, floor-gated — see full tiers above)*
2. Beast Form *(shared, floor-gated — see full tiers above)*
3. Alpha Strike
4. Howling Sequence
5. Perfect Form
*(Evolution tier specifics for Alpha Strike/Howling Sequence/Perfect Form
not yet written — only marked as evolving, not detailed.)*

#### 🦎 Ironscale (Lizardman) — Tank Specialization — LOCKED *(renamed from
Titan)*
*(Specialization of Martial Artist — NOT a separate class.)*

**Universal (shared foundation, 5 abilities — see Shared Foundation
above):** Tiger Strike, Roundhouse Kick, Battle Focus, Meditate
(Evolving), Ironscale Form (Evolving) *(class-specific name for the shared
"Beast Form" mechanic, per specialization theming.)*

**Specialization Actives (7):**
| Ability | Evolves | Description |
|---|---|---|
| Iron Body | ⭐ | Become immobile, generating massive threat while storing or reflecting incoming damage. |
| Pressure Point | ⭐ *(final)* | Expose an enemy's weak point, increasing all damage they take. |
| Shockwave | ⭐ *(final)* | Emit a damaging shockwave around you, generating threat and controlling nearby enemies. |
| Counter Strike | — *(final)* | Counter the next attack with a devastating retaliation. |
| Perfect Counter | — *(final)* | Reflect the next incoming attack back to its attacker. |
| Taunt | — | Force nearby enemies to attack you. |
| Anchor Hold | — | Root or pin an enemy in place, preventing movement. |

**Passive Abilities (3):**
1. **Elemental Defense** — adapt to the last element that struck you,
   temporarily increasing resistance to it.
2. **Bedrock** — gain increasing damage reduction the longer you remain
   stationary.
3. **Living Fortress** *(working name)* — taking damage grants Fortitude,
   empowering your defensive abilities.

**Evolving Abilities (5, TRUE FINAL VERSION — supersedes an earlier
Counter Strike/Perfect Counter draft that was itself a correction of the
original table):**
| Ability | Why It Evolves |
|---|---|
| Meditate *(shared)* | Better Form management. |
| Ironscale Form *(shared)* | Better transformation. |
| Iron Body | Ultimate defensive cooldown. |
| Pressure Point | Becomes a full raid-support debuff. |
| Shockwave | Evolves into the Titan's signature battlefield control ability. |

*(Counter Strike, Perfect Counter, Taunt, and Anchor Hold are reliable/
non-evolving utility abilities in this final version.)*

#### 🦅 Tempest (Harpy) — Ranged DPS Specialization — LOCKED
*(Specialization of Martial Artist — NOT a separate class.)*

**Universal (shared foundation, 5 abilities — see Shared Foundation
above):** Tiger Strike, Roundhouse Kick, Battle Focus, Meditate
(Evolving), Tempest Form (Evolving) *(class-specific name for the shared
transformation mechanic).*

**Specialization Actives (7):**
| Ability | Evolves | Description |
|---|---|---|
| Hail of Feathers | ⭐ | Launch a storm of razor feathers over an area. *(USDA-inspired.)* |
| Void Palm | ⭐ | A ranged palm strike whose impact materializes directly on the target. |
| Tempest Focus | ⭐ | Increase attack speed, cast speed, and movement speed. |
| Star Cross | — *(⚠️ table marked this ⭐, but the explicit final 5-item evolving list below does NOT include it — treating the final list as authoritative, same pattern as Titan)* | Fire multiple chi stars at nearby enemies. |
| Chi Bullet | — | Piercing chi projectile that detonates after passing through the target. |
| Flickering Step | — | Instantly reposition, leaving behind an afterimage. |
| Feather Prison | — | Encase a target in swirling feathers, preventing movement for a short time. |

**Passive Abilities (3):**
1. **Flowing Chi** — using different abilities in succession empowers the
   next ability.
2. **Perfect Rhythm** — every few abilities restore Mana while
   transformed.
3. **One with the Wind** — mobility and chi techniques briefly increase
   movement speed and evasion.

**Evolving Abilities (5, FINAL LOCKED):**
| Ability | Evolution Theme |
|---|---|
| Meditate *(shared)* | Better Mana management and Form sustain. |
| Tempest Form *(shared)* | Stronger Harpy Form with improved stats and lower Mana drain (Final Form at Floor 10). |
| Hail of Feathers | Becomes a larger, deadlier feather storm with additional effects. |
| Void Palm | Evolves from a simple ranged palm strike into an iconic chi technique. |
| Tempest Focus | Self-buff that increasingly enhances your combat flow. |

**Status: ✅ DESIGN LOCKED — full 12-active + 3-passive kit defined,
evolving list finalized with reasoning for each.**

---

**🎉 MARTIAL ARTIST COMPLETE — all 3 specializations (Beast/Titan/Tempest)
now fully locked. This completes ALL 12 classes in the roster.**

---

## Open Questions / Gaps To Fill (working list)

1. ~~Trickster — confirm if this is still a real class or was cut in the
   locked redesign.~~ RESOLVED — Trickster is confirmed, fully designed,
   one of the strongest/most distinct identities of the set.
2. Bastion, Assassin, Mystic — need full gameplay-loop writeups (currently
   only role + one-line identity from the locked doc).
3. Every class — need explicit confirmation of the FINAL 5 evolving
   abilities, since most classes currently have more built abilities than
   the "5 evolving abilities" rule specifies, and it's unclear which 5 are
   canonical vs. which are extra/legacy kit.
4. ~~Berserker's signature ability (Whirlwind-style spin) — not built at
   all.~~ RESOLVED — this is now Cyclone, fully designed.
5. ~~Martial Artist's 3-way Floor-2 split — needs full design from
   scratch.~~ RESOLVED — Beast/Titan/Tempest all fully designed with
   complete 12-active + 3-passive kits.
6. Sorcerer — Arcane path enforcement + hybrid unlock logic not yet built.
7. ~~Archer naming decision~~ RESOLVED — renamed to **Fletcher**.
8. **Bloodlust naming collision** — now used by Assassin (core resource/
   passive), Slayer (working-name active), AND Beast/Martial Artist
   (passive lifesteal). Needs a dedicated resolution pass across all
   three.
9. Void Communion — cut from Mystic's final kit. What did it actually do?
   Never described anywhere in this document; if revived later, needs a
   real description written from scratch.
10. Beast Form legacy system — does the OLD built Beast Form (3 variants:
    Fenrir/Celestial/Basilisk) get fully superseded by the new Beast/
    Titan/Tempest specializations, or coexist somehow? Not resolved.
11. Martial Artist's Floor 1 stance ability naming (Battle Focus/Fighting
    Spirit/Inner Strength/Combat Trance) — not decided yet.

---

*Last updated: this session. ALL 12 CLASSES NOW HAVE LOCKED DESIGN
CONTENT. Remaining work per-class: fill in missing descriptions
(especially Mystic's undetailed actives), resolve the open items above,
and eventually move from design → implementation.*
