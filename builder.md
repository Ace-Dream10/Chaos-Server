# builder.md

Guidance for any AI assistant (ChatGPT, Claude, or otherwise) helping build content and features on Chaos-Server.

**Read `CLAUDE.md` first** — it covers build/test/run commands, solution layout, the entity hierarchy, configuration cascade, testing conventions, and coding standards. This file doesn't repeat any of that; it captures conventions and pitfalls that only show up once you've actually built things in this repo, so a new assistant doesn't have to rediscover them the hard way.

## Content authoring: where things go

`Data/Configuration/Templates/` is organized by class/type, not flat:

- `Skills/<Class>/` — e.g. `Skills/Berserker/cyclone.json`. `Skills/Universal/` holds class-agnostic base skills (assail, ambush, twohandedattack).
- `Spells/<Class>/` — e.g. `Spells/Mystic/quick_mend.json`. Empty class folders (`Lancer/`, `MartialArtist/`) exist on purpose for classes with no unique spells yet.
- `Monsters/Enemies/` — real combat monsters. `Monsters/Spawned/` — skill/spell-spawned entities (decoys, shrines, hazards, dust devils) that are never placed by a map spawner directly.
- `Items/` has its own subfolders by slot (Helms, Weapons, etc.) — check neighboring files for the pattern before adding a new one.

**Template loading is recursive and keyed by filename, not path.** `Recursive: true` is set for every template cache in `appsettings.json`, and lookups match by `templateKey` (the filename, case-insensitive), scanned across all subfolders. This means:
- You can reorganize folders freely without updating any `scriptKeys`/`skillTemplateKeys`/`spellTemplateKeys`/loot table references — they're all string keys, not paths.
- Two files with the same basename anywhere under the same template root *will* collide silently (first match wins) — keep names unique within a template type.

## The #1 pitfall: `ConfigurableScriptBase` requires a matching `scriptVars` entry

Every skill/spell/effect script picks one of two base classes:

- `Configurable*ScriptBase` (e.g. `ConfigurableSkillScriptBase`) — for scripts with tunable values. The constructor does `subject.Template.ScriptVars[scriptKey]` and **throws `NullReferenceException` at construction time** if the template's `scriptVars` JSON has no entry under that script's key. The exception message says exactly this: *"If this script has no configurable variables, do not use a ConfigurableScript."*
- Plain `*ScriptBase` (e.g. `SkillScriptBase`) — for scripts with zero configurable values (hardcoded constants, or pure pass-through/placeholder scripts). No `scriptVars` requirement at all.

**Rule of thumb:** if the script class has no `#region ScriptVars` block of public settable properties, it must not extend a `Configurable*ScriptBase`. This is an easy mistake when copy-pasting a script as a starting point — the crash won't show up until something actually constructs that specific `Skill`/`Spell`/`Effect` instance (e.g. a player who has the skill logs in), so it can pass a build and even a server boot silently before biting in production.

Note the default `CanUse()` differs between the two: `Configurable*ScriptBase` defaults to `true` (always usable), plain `*ScriptBase` defaults to `context.Source.IsAlive`. Switching base classes to fix the above can subtly tighten this — usually the more correct behavior (dead characters generally shouldn't be able to use skills), but worth knowing it's not purely cosmetic.

## Common patterns worth reusing rather than reinventing

- **`EffectBase` lifecycle**: `OnApplied()` / `OnTerminated()` / `Update(TimeSpan)` / `ShouldApply(source, target)` (return `false` to reject application — base impl blocks a same-name/same-icon conflicting effect). `SetDuration()` sets how long it lasts.
- **Tag-based AI gating**: `Subject.Trackers.Tags` (a `ConcurrentDictionary<string,string>`) is the standard way to make a status effect suppress monster AI behavior. `WanderingScript.cs` and `AggroTargetingScript.cs` already check tags like `"stasis"`, `"rooted"`, `"asleep"`, `"feared"`, `"vanished"` — add a new tag there rather than inventing a parallel mechanism.
- **Passive class mechanics** (rage, chi, resource-on-hit systems): implemented as an `AislingScript` that no-ops unless `Subject.UserStatSheet.AdvClass == AdvClass.X`, ticking every `Update(TimeSpan delta)`. See `BerserkerRageScript.cs` for the reference shape: builds a resource on hit, decays it when idle, syncs a scaling stat bonus, and drives tiered visual auras — all in one script, no need to hook the damage pipeline directly for the stat-bonus part.
- **Damage pipeline hooks** (shields, counters, on-hit procs, damage-taken modifiers): `Chaos/Scripting/FunctionalScripts/ApplyDamage/ApplyAttackDamageScript.cs` is the shared choke point for all attack damage. It already has an `if/else if` chain of tag/effect checks in the Aisling case (Veil, Shrine shield, Bubble shield, Lancer's Shield, Counter Strike, Phoenix Rise) — add new "block/negate/redirect damage" mechanics as another branch here, not as a bolt-on effect that tries to intercept damage on its own.
- **True invulnerability/damage negation**: don't rely on a huge negative AC bonus. Aisling AC and Monster AC are clamped to *separate*, independently-configured floors (`WorldOptions.MinimumAislingAc` vs `MinimumMonsterAc`) — a `-10000` AC trick that fully negates damage for a monster will NOT do the same for a player, since the Aisling floor is far less extreme. For a player-side "block", set a tag and short-circuit the damage in `ApplyAttackDamageScript` instead.

## Workflow

- Standard build/restart cycle: stop whatever's on port 5000, `dotnet build Chaos.slnx --nologo` and confirm `0 Error(s)`, run the server, grep its log for `error|exception|fail` (excluding lines containing `warning`).
- **PowerShell BOM gotcha**: `Set-Content -Encoding utf8` in Windows PowerShell 5.1 writes UTF-8 **with BOM**, which the JSON parser chokes on (`'0xEF' is an invalid start of a value`). Prefer the `Write`/`Edit` tools for JSON; if you must use PowerShell to write JSON, verify with a hex check (`EF BB BF` prefix = corrupted) or write via `[System.IO.File]::WriteAllText(path, content, (New-Object System.Text.UTF8Encoding($false)))`.
- **PowerShell + existing collections is landmine territory**: wrapping an already-existing `List<T>`/other `IEnumerable` in `@(...)` when passing it as a bare positional argument to a user-defined function can corrupt parameter binding in ways that are hard to diagnose (silently reports the wrong error, or the wrong line). Prefer named parameters (`-ParamName $value`) for anything beyond trivial scripts, or just do it in C# instead — `ConvertTo-Json` also silently collapses a single-element array into a bare object, which will corrupt any JSON file expected to stay an array. For anything that reads/writes real save-file JSON, a small C# console tool using `System.Text.Json.Nodes` is safer than PowerShell.
- Never commit or deploy without being explicitly asked. This repo has a live production deployment (git push → SSH pull → `dotnet build` → restart the systemd service) — treat any request to touch it as a distinct, higher-stakes ask from local testing.
- `Data/Configuration/Access/*/password.txt` is gitignored and untracked (per-account runtime credential data, not source) — don't re-add it.
