# Floor Tracker Design (Ascension Chamber)

Design-only pass for the three converging Open Items: **Floor progression tracker system**,
**Boss death → crystal spawn mechanic**, **First Clearer tracker**. Scope here is the tracking/display
system these three items need in common — not the crystal-spawn loot mechanic itself, and not the
accessory-set reward from `GAME_DESIGN.md`'s "Accessory Sets Per Floor" section (that's a consumer of
the "first clearer" flag this design produces, not part of this design).

Phase 0 (design) and Phase 1 (server-side tracking) are complete. Phase 2 (new packet/networking
fork) and Phase 3 (client HUD panel) have not been started.

## Decision point 0: 10 floors vs 20 floors — RESOLVED

**10 floors**, target endgame level **255**. `GAME_DESIGN.md` has been corrected to match (it previously
said 20 floors, carried over from an earlier session). Everything below uses `FloorCount = 10`.

Still open, and explicitly *not* guessed at (see `GAME_DESIGN.md`'s Floor Progression section, updated
alongside this doc): floor 1's level cap is currently hardcoded at 19 against the *old* 20-floor plan,
and floors 2–10 have no caps decided at all. Halving 20→10 doesn't imply a mechanical fix (evenly
spacing 255 across 10 floors gives ~25-26/floor, which doesn't match 19 either) — this needs a real
answer from you before Phase 1 touches anything level-cap-related, not a guess dressed up as a
placeholder.

## 1. Data model

**Per-player state — lives in `Aisling.Trackers.Counters`, not a new field or a new AislingScript for
storage.**

`Chaos/Collections/Trackers.cs` already has `Counters` (`CounterCollection`, explicitly marked
`IS PERSISTENT / SERIALIZED TO FILE`), and it's already the established pattern for exactly this shape
of state — `StaciasBubbleEffect` and `WebTrapReactorScript` both use `Trackers.Counters` for simple
named int state on an Aisling. There's even a forward-reference already in the codebase:
`BeastFormEffect.cs` line 62 has a comment — *"placeholder: tiering by character Level until floor
progression is tracked - once that lands, switch this to
`source.Trackers.Counters.Get("currentFloor")`"* — i.e. this exact key name was already anticipated.

Proposed counter keys on `Aisling.Trackers.Counters`:
- `"currentFloor"` — int, 1-`FloorCount`, 0/absent = not currently in the Ascension Chamber.
- `"highestFloorCleared"` — int, 0-`FloorCount`, 0 = none cleared yet.

Both persist automatically via the existing Aisling save/load path — no new schema work needed.

**Per-floor global state — needs a new store; nothing existing fits.**

Checked `MapInstance` (`Chaos/Collections/MapInstance.cs`) for a generic tag/variable bag like
`Trackers` has — there isn't one. Floors are **global**, not per-shard/per-instance (per your "shared
global flag per floor" framing), so this doesn't belong on `MapInstance` anyway — it needs to be one
value shared server-wide per floor number, independent of how many shards/instances of a floor map
exist.

Recommended shape: a new store modeled directly on the existing `GuildStore` / `MailStore` /
`BulletinBoardStore` pattern — all three extend `PeriodicSaveStoreBase<T, TOptions>`
(`Chaos/Services/Storage/Abstractions/PeriodicSaveStoreBase.cs`), which gives periodic autosave +
final-save-on-shutdown for free (this is the exact "Performing final save before shutdown" line you'd
have seen in the server logs for Guild/Mail/BulletinBoard). A new `AscensionFloorStore : PeriodicSaveStoreBase<AscensionFloorState, AscensionFloorStoreOptions>`
would key its cache by floor number (as a string, matching the `ConcurrentDictionary<string, T>` key
type the base class already uses) and store one small JSON file per floor:

```csharp
public class AscensionFloorState
{
    public int FloorNumber { get; init; }
    public bool BossAlive { get; set; } = true;
    public string? BossName { get; set; }
    public List<string> FirstClearers { get; set; } = [];   // participant names at time of boss death; set once, never appended after
    public DateTime? FirstClearedAt { get; set; }
}
```

This is new code (no existing type to reuse), but it's a small, mechanical follow of an established
pattern — low risk.

## 2. Trigger points

**Floor transition.** Checked for an existing "player changed map" script hook (`OnMapChanged` or
similar) on the Aisling/Creature script interfaces — **there isn't one**, `Chaos.Scripting.Abstractions`
has no such callback. Two viable hook points, and I'm recommending the combination of both rather than
picking one:

- `Chaos/Scripting/ReactorTileScripts/WarpScript.cs` is the existing pattern for map-transition gates
  (level-gates a destination map, then calls `source.TraverseMap(...)`). A new
  `AscensionFloorGateScript : ConfigurableReactorTileScriptBase` (sibling to `WarpScript`, not a
  modification of it — `WarpScript` is used for unrelated warps too) would additionally check
  `highestFloorCleared >= targetFloor - 1` before allowing entry, matching the "clear floors in order"
  implication of a 10-floor progression. This handles the *primary, intended* path onto a floor.
- But a reactor-tile-only hook misses anything that isn't walking onto that specific tile: a GM `/warp`,
  a relog that resumes the player mid-floor, a future recall/return mechanic. Per `builder.md`'s guidance
  on passive per-player mechanics ("implemented as an AislingScript that no-ops unless ..., ticking every
  `Update(TimeSpan delta)`" — the `BerserkerRageScript` shape you pointed at), the robust version is a
  new `AscensionFloorTrackerScript : AislingScriptBase` that polls (throttled, not every tick — see
  `BerserkerRageScript`'s `SinceLastHit`/interval pattern for the throttling shape) whether
  `Subject.MapInstance` currently resolves to a known floor map, and if the resolved floor differs from
  `Trackers.Counters["currentFloor"]`, updates the counter and fires the tracker packet. This is what
  actually keeps the tracked state honest regardless of how the player got there — the gate script is
  just the normal front door.
  - Open question: how a `MapInstance` resolves to "this is floor N" — a naming convention on
    `MapTemplate.TemplateKey` (e.g. `ascension_floor_3`), or an explicit new field. Only 3 of the
    existing `20000`/`20002`/`20003.json` maps in `Data/Configuration/Templates/Maps/` look
    Ascension-Chamber-related so far (not a clean 1-`FloorCount` set yet) — the floor↔map mapping itself
    isn't decided, separate from this tracker's mechanics.

**Boss death.** `Chaos/Scripting/MonsterScripts/DeathScript.cs` is the generic on-death handler (loot,
gold, exp, ability) attached to *every* monster via its `scriptKeys`. `CompositeMonsterScript`
(`Chaos/Scripting/MonsterScripts/CompositeMonsterScript.cs`) confirms every attached script's `OnDeath`
fires — it's explicitly composable, not single-dispatch. So a new `AscensionBossDeathScript
: MonsterScriptBase` is purely additive: a boss monster template gets `"scriptKeys": ["Death",
"AscensionBossDeath"]` (or similar), the new script only handles the floor-state side (mark
`BossAlive = false` in the per-floor store, and — see First Clearer below — set `FirstClearerName` if
unset), then broadcasts the tracker packet to every Aisling currently on that floor. `DeathScript`
itself is untouched.

**First Clearer.** Falls out of the boss-death hook above, not a separate trigger, but the crediting rule
changed from "single reward-target Aisling" to **participation-based, whole-set crediting** — see the new
§6 Participation Tracking below for the mechanism. In `AscensionBossDeathScript.OnDeath`, after marking
the boss dead, check `AscensionFloorStore`'s `FirstClearers` list for that floor — if empty/unset, set it
to the full participant-name set captured during the fight (§6) and bump `highestFloorCleared` for each
of those names' Aislings (only for ones still resolvable — see §6's name-vs-ID note). Subsequent clears
of the same floor by other players see `FirstClearers` already populated and skip it — satisfies "doesn't
overwrite on subsequent clears."

## 3. Packet payload

Single packet carrying full state, per your instruction (not partial-update packets):

```csharp
public sealed class FloorTrackerArgs : IPacketSerializable   // exact base type TBD against Chaos.Networking conventions
{
    public byte CurrentFloor { get; set; }              // 0 = not in the chamber, 1-FloorCount otherwise
    public bool BossAlive { get; set; }
    public string? BossName { get; set; }
    public List<string> FirstClearers { get; set; } = []; // empty = not yet cleared; participant names, not a party/group id
    public byte HighestFloorCleared { get; set; }        // 0-FloorCount
}
```

Changed from a single nullable `FirstClearerName` (Phase 0 draft) to a `List<string>`, since crediting is
now participation-based — every damage-dealer/damage-taker in the fight is a first-clearer, not one
reward-target individual. Worth flagging for Phase 2: this is the field most likely to need a sane upper
bound before it goes on the wire (a full-server zerg on floor 1 could in theory produce a large list) —
no cap decided yet, listing under Open Questions below rather than picking a number.

`byte` for the floor fields is deliberate future-proofing against the 255 ceiling discussed below, even
though 10 (or 20) floors obviously fits in far less — no reason to use a wider type here.

Delivery, per your confirmed update model (real-time push on any relevant event) and Phase 2's targeting
note:
- Floor change → send only to the player who moved.
- Boss death / first-clearer → broadcast to every Aisling currently on that floor (all shards/instances
  of it, since the state is global) — needs a "get all Aislings currently on floor N" query, which is
  just "all Aislings whose `Trackers.Counters["currentFloor"] == N`" rather than anything MapInstance-shard
  specific, consistent with floors being global state.

## 4. First-clearer persistence — RESOLVED: persist it

Confirmed: use the `AscensionFloorStore` design from §1 (`PeriodicSaveStoreBase` pattern, same shape as
`GuildStore`/`MailStore`/`BulletinBoardStore`). `AscensionFloorState.FirstClearerName` (Phase 0 draft,
single nullable string) becomes `FirstClearers` (`List<string>`, populated once and never appended to
after the floor's first clear) to match the participation-based crediting in §6.

## 5. Character level byte ceiling — confirmed

Checked both sides of the wire:
- Server-internal `Chaos/Models/Data/StatSheet.cs` stores `Level` as `int` — no internal ceiling.
- The network packet that actually reaches the client, `Chaos.Networking/Entities/Server/AttributesArgs.cs`,
  declares `public byte Level { get; set; }` — **this is the real hard ceiling**. Whatever the server
  thinks a character's level is, only 0-255 survives the trip to the client's HUD/stat display.
- No explicit "max character level" clamp exists elsewhere in the leveling code (`DefaultLevelUpScript`,
  `DefaultExperienceDistributionScript`) — level is currently unbounded server-side and gated only by however
  much experience the curve requires, so the byte wire ceiling is the *only* hard 255 cap in the system
  today.

Confirmed: **255 is a real, load-bearing ceiling**, not just a round design target — a character that
somehow reached level 256+ server-side would send corrupted/wrapped level data to the client. Doesn't
constrain anything else in *this* feature (floor count is a separate, much smaller number either way),
but worth having on record since the task brief called it out as something to verify rather than assume.

## 6. Participation tracking (new — replaces the single-reward-target credit model)

**Where damage flows through today.** Confirmed `Chaos/Scripting/FunctionalScripts/ApplyDamage/ApplyAttackDamageScript.cs`
is the single choke point for both directions, per `builder.md`'s note — one `ApplyDamage(Creature
source, Creature target, ...)` method, dispatched by a `switch (target)`:
- `target` is `Monster` → this is "an Aisling (or other source) damaged the boss." After HP is
  subtracted, it calls `monster.Script.OnAttacked(source, damage)`.
- `target` is `Aisling` → this is "the boss damaged an Aisling." After HP is subtracted, it calls
  `aisling.Script.OnAttacked(source, damage)`.

Both hooks already exist and already fire on every hostile hit (`ApplyDamage` returns early on
`damage <= 0`, so a 0-damage swing never reaches either hook — heals don't route through this method at
all, they're a separate pipeline, so "does healing count" is a non-issue by construction, not something
that needs a guard). I'm proposing new script logic that taps both existing hooks rather than modifying
`ApplyAttackDamageScript.cs` itself.

**Where the participant set lives.** `Monster` already carries one similarly-shaped collection —
`Contribution` (`Chaos/Collections/ContributionList.cs`, a `ConcurrentDictionary<uint, int>` of attacker
ID → cumulative damage, populated by the existing `ContributionScript.OnAttacked`, consumed by
`DeathScript` for reward-target resolution). I considered reusing `Contribution` directly for the
"dealt damage to boss" half, but decided against it: `Contribution` is keyed by `Creature.Id` (`uint`),
which breaks the moment a participant logs out before the boss dies — the ID is meaningless without a
live `Aisling` to resolve it back to a name, and `Map.TryGetEntity<Aisling>(id)` (what `DeathScript` uses)
only finds Aislings *currently on that map*, not ones who left. Since one of your confirmed edge cases is
"a player who deals 1 damage and immediately leaves the floor still counts," name resolution can't be
deferred to death time.

So: a new, separate, self-contained collection, keyed by **name** (captured at the moment of the hit,
while the Aisling reference is still guaranteed live), not ID:

```csharp
// new file, e.g. Chaos/Collections/ParticipantSet.cs — mirrors ContributionList's shape
public class ParticipantSet : IEnumerable<string>
{
    private readonly ConcurrentDictionary<string, byte> Names = new(StringComparer.OrdinalIgnoreCase);
    public bool Add(Aisling aisling) => Names.TryAdd(aisling.Name, 0);
    public IEnumerator<string> GetEnumerator() => Names.Keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

...exposed as a new property directly on `Monster` (`Chaos/Models/World/Monster.cs`), the same way
`Contribution` already is: `public ParticipantSet AscensionParticipants { get; } = new();`. This does
touch a shared/core file — flagging that plainly, same as `Contribution` already does today for every
monster in the game, boss or not (negligible cost, but it's not scoped only to bosses).

**Lifecycle / reset.** Confirmed via `Chaos/Models/Data/MonsterSpawn.cs`: every spawn (including a boss
respawning after death) goes through `MonsterFactory.Create(...)`, a **brand-new `Monster` instance**,
not a HP-reset on a reused object. So "reset on boss spawn" is automatic and free — a fresh instance
starts with an empty `AscensionParticipants`, no explicit reset code needed.

**Marking a monster as a tracked boss.** New tag on spawn, per `builder.md`'s existing guidance
("tag-based gating... standard way... add a new tag there rather than inventing a parallel mechanism"):
`Trackers.Tags["ascensionFloor"] = "3"` (string value = floor number), set once when the boss's dedicated
death-script sees it's freshly constructed. This single tag both marks "this is a tracked boss" *and*
carries which floor, avoiding a separate floor↔monster lookup at death time.

**Populating both directions**, both via the new `AscensionBossDeathScript : MonsterScriptBase`
(the same script Phase 0 already proposed for the death hook — this gives it a second responsibility
rather than adding yet another new script class):
- Monster-side (dealt damage to boss): override `OnAttacked(Creature source, int damage, int?
  aggroOverride)` — if `source is Aisling aisling`, `Subject.AscensionParticipants.Add(aisling)`.
- Aisling-side (damaged by boss): this direction fires on the *Aisling's* script, not the boss's, so it
  needs the other new script from Phase 0, `AscensionFloorTrackerScript : AislingScriptBase`, to also
  override `OnAttacked(Creature source, int damage)` — if `source is Monster boss &&
  boss.Trackers.Tags.ContainsKey("ascensionFloor")`, `boss.AscensionParticipants.Add(Subject)`.

**At death**, `AscensionBossDeathScript.OnDeath` reads `Subject.AscensionParticipants` (already just
names, already resolved, no further lookup needed) as the `FirstClearers` list for `AscensionFloorStore`.

### Edge cases — answered where the mechanism settles it, flagged where it's a real judgment call

- **Does healing count as "damage received"?** No — settled by construction, not a rule I had to invent.
  Heals don't route through `ApplyAttackDamageScript`/`OnAttacked` at all.
- **Does a drive-by participant (1 damage, then leaves) still count?** Yes — settled by the name-capture
  design above; they're in the set the instant the hit lands, independent of whether they're still around
  at death.
- **Minimum participation threshold, to prevent someone tagging the boss once from across the map and
  claiming credit?** **Not deciding this — asking.** Current design's default is "any nonzero hit counts,
  no threshold" (falls out naturally from `ApplyDamage`'s existing `damage <= 0` early-return — there's no
  extra code either way, the floor is just wherever that early-return already sits). If you want a real
  bar (e.g. minimum damage dealt, or minimum % of the boss's max HP, or minimum number of hits), that's a
  number I don't have a principled way to pick and shouldn't guess.
- **Do pets/summons count toward their owner?** Confirmed: yes, they exist, and per your decision, credit
  must resolve to the owning player, not the pet's own name. **However: no existing ownership-resolution
  mechanism was found to hook into, after a real search — this is now blocking, not just flagged.**
  Checked every summon/decoy-flavored script in the codebase: `ShadowCloneScript.cs`,
  `MirrorImageScript.cs`, `DustDevilScript.cs` (the `Monsters/Spawned/` category `builder.md` describes).
  All three follow the identical shape: `MonsterFactory.Create(templateKey, map, spawnPoint)` with **no
  reference back to the summoning Aisling passed in or stored anywhere on the resulting `Monster`** — no
  `Owner`/`Summoner`/`Master` property on `Monster` (`Chaos/Models/World/Monster.cs`; the only `Owner`
  properties anywhere in `Chaos/Models/World/` belong to unrelated types — `ReactorTile.Owner` for
  traps, `GroundEntity.Owners` for item pickup rights, neither applicable to a live combatant). Also
  checked `SummonCommand.cs` — that's a GM `/summon <player>` teleport command, unrelated to pets.
  `ShadowCloneScript`'s clone genuinely fights on its own (aggro-pulled monsters attack it, and per its
  own doc comment it "actively attacks nearby hostile monsters on its own"), so it's a real case where a
  non-Aisling `Creature` deals and receives damage that, per your decision, should credit a player — and
  there is currently no way to ask "whose clone is this" from the clone object itself.
  Per your instruction not to guess at a new ownership model: **stopping here on this specific point**
  rather than inventing one (e.g., a new `Trackers.Tags["summonedBy"]` or a new `Monster.OwnerName`
  property would both work mechanically, but "which shape" and "does it retrofit onto the three existing
  spawn scripts too" is a design decision, not an implementation detail). Rest of §6 and Phase 1 proceeds
  on `source is Aisling`/`target is Aisling` only for now, with pet/summon damage simply **not** captured
  into `AscensionParticipants` until this is resolved — a known, explicit gap, not a silent one.

  **DECIDED for this pass:** `AscensionParticipants` captures **Aisling-source / Aisling-target damage
  only**. A hit dealt by or received from a pet/summon (e.g. `shadow_clone`, `mirror_image_decoy`, a
  `DustDevilScript` hazard) does **not** credit anyone — not the pet, not an owner — because there is
  currently no way to resolve "whose pet is this." This is a deliberate, documented exclusion for this
  pass, not an oversight.

  > **Known follow-up (own future task, not part of this feature):** pet/summon damage should credit the
  > owning player once an ownership-resolution mechanism exists. None does today. Would need either a new
  > `Monster.OwnerName` property or a `Trackers.Tags["summonedBy"]` entry, set at spawn time and retrofitted
  > onto all three existing untethered spawn scripts — `Chaos/Scripting/SkillScripts/ShadowCloneScript.cs`,
  > `Chaos/Scripting/SkillScripts/MirrorImageScript.cs`, `Chaos/Scripting/SpellScripts/DustDevilScript.cs` —
  > plus whatever the participant-capture hooks in this section would need updated to check that field
  > before falling back to "no credit." Tracked here so it isn't lost; not scheduled.

## Summary of what's genuinely new code vs. reused patterns

| Piece | Status |
|---|---|
| Per-player floor state storage | Reuse `Trackers.Counters` (existing, persistent, already anticipated by a code comment) |
| Per-floor global state storage | New `AscensionFloorStore`, mechanically modeled on `GuildStore`/`MailStore`; persistence confirmed (§4) |
| Floor-entry detection | New `AscensionFloorGateScript` (reactor tile, sibling to `WarpScript`) + new `AscensionFloorTrackerScript` (polling `AislingScript`, sibling to `BerserkerRageScript`) |
| Boss-death hook | New `AscensionBossDeathScript` (`MonsterScript`), purely additive alongside existing `DeathScript` via `scriptKeys` |
| Participation tracking | New `Monster.AscensionParticipants` (`ParticipantSet`, name-keyed) populated from both existing `OnAttacked` hooks (§6) — does **not** reuse `Contribution`, see §6 for why |
| First-clearer resolution | Participation-based: whole `AscensionParticipants` set at time of death, not a single reward-target (§2, §6) |
| New packet | Genuinely new — requires forking `Chaos.Networking` source into `Chaos.Client` per its README's "brand-new packet" section, since this isn't an opcode the compiled NuGet package knows about |

## Open questions before Phase 1

1. ~~10 vs 20 floors~~ — **resolved**, 10 floors (§0).
2. ~~First-clearer persistence~~ — **resolved**, persist (§4).
3. ~~First-clearer credit model~~ — **resolved**, participation-based (§6).
4. **Floor 1–10 level caps** — floor 1's existing 19 was set against the old 20-floor plan and hasn't
   been re-derived; floors 2–10 have no numbers at all. Not obvious from existing design intent (§0) —
   needs an explicit answer, not a guess.
5. Floor↔map resolution convention (naming convention vs. explicit field) — only 3 of the expected floor
   maps currently exist, so this may be gated on more map content existing, not just code.
6. ~~Minimum participation threshold~~ — **resolved**, none; any nonzero hit counts (§6).
7. ~~Pets/summons crediting their owner~~ — **resolved for this pass**: excluded, documented known
   follow-up (§6). Confirmed pets/summons do exist (`shadow_clone`, `mirror_image_decoy`, dust devil
   hazards) and no ownership mechanism exists to credit them — own future task, not blocking Phase 1.
8. `FirstClearers` list has no upper bound defined yet (§3) — probably fine for a 10-floor chamber's
   likely-small early-floor groups, but flagging before it goes on the wire in Phase 2.
