# Elysium Server Operations Guide

Rebuilt from an old copy that predated the local repo reorganization (`D:\Chaos-Server` →
`D:\Elysium - Master\Server\Elysium.Server`). Every path/command below was re-verified against
the actual current scripts and config, not just find-and-replaced - see the "What changed"
callouts where something moved, was replaced, or turned out to still be accurate for a
non-obvious reason (the remote/live side didn't rename anything, only the local Windows folder
did).

## Quick Reference

| Task | Command |
|---|---|
| Ensure local server is running | `.\ensure-server.ps1` |
| Deploy to live | `.\deploy.ps1` |
| Redeploy the client (local test build) | `..\..\Client\Elysium.Client\deploy-client.ps1` |
| SSH into live server | `ssh -i "C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem" ubuntu@18.222.119.157` |
| Check live server status | `sudo systemctl status chaos-server.service --no-pager` |
| Restart live server | `sudo systemctl restart chaos-server.service` |
| Watch live logs | `journalctl -u chaos-server.service -f --no-pager` |
| One-click GUI for most of the above | `Elysium.DevPanel` (see below) |

---

## Elysium.DevPanel (new)

**Location:** `D:\Elysium - Master\Tools\Elysium.DevPanel`

A small WinForms tool (same no-MVVM/no-DI style as `Elysium.SpriteBuilder`) that wraps most of
this document's manual commands as buttons, with live streamed output. Four sections, ordered by
how far the action reaches:

- **LOCAL** (safe, one-click): Restart Local Server, Redeploy Client (local test), Redeploy
  Client + Restart Server (combo), Check Local Server Status, Open Elysium.exe, Run Full Test
  Suite.
- **CHECKS** (read-only, no confirmation needed): Check for Uncommitted Changes, Compare Local
  vs. Live Commit, Check Live Server Status, Open Live Log Tail.
- **GIT — LOCAL ONLY** (stays on this machine): Commit Changes (local) - shows the full file list
  before staging, flags scratch-like files (zips, "test"/"scratch"/"backup" in the name) as a
  warning rather than silently including or excluding them.
- **⚠ SHARE / PRODUCTION** (leaves this machine, confirmation required): Push to GitHub, Deploy
  to Live Server, Restart Live Service.

Commit and push are deliberately separate buttons in separate sections - see **Known Gotcha #7**
below for why.

---

## Deploying to Live Server

### Normal deploy (code + content changes):
```powershell
cd "D:\Elysium - Master\Server\Elysium.Server"
git add <specific files>   # NOT git add -A - see Known Gotcha #7
git commit -m "describe what you changed"
.\deploy.ps1
```

**Order matters: add → commit → deploy. Always commit before running `deploy.ps1` - it only
pushes what's already committed, it does not commit for you.**

`deploy.ps1` (unchanged in shape from the old doc, verified against its current source):
1. `git push origin linux-porting-fixes`
2. SSH into live server (`ubuntu@18.222.119.157`)
3. `git pull origin linux-porting-fixes` in `~/Chaos-Server`
4. `/home/ubuntu/.dotnet/dotnet build Chaos/Chaos.csproj -c Release`
5. `sudo systemctl restart chaos-server.service`
6. `sleep 3` then `sudo systemctl is-active chaos-server.service` - aborts loudly (red text,
   non-zero exit) if the remote step fails, rather than reporting success either way

**What changed:** nothing structurally - `deploy.ps1` still lives at the Server repo root and
still does exactly what the old doc described. The **remote path is `~/Chaos-Server`, not
renamed** - only the local Windows folder was reorganized tonight; the live Linux server's own
clone kept its original name. Don't "fix" `~/Chaos-Server` in any script that touches the remote
side - it's correct as written.

**Does NOT touch:**
- `Data/Saved/Aislings/` — player character saves
- `Data/Configuration/Access/` — player password files

### Manual deploy (if deploy.ps1 fails):
```powershell
# Step 1 — push code
git push origin linux-porting-fixes

# Step 2 — SSH in
ssh -i "C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem" ubuntu@18.222.119.157

# Step 3 — on the server
cd ~/Chaos-Server
git pull origin linux-porting-fixes
/home/ubuntu/.dotnet/dotnet build Chaos/Chaos.csproj -c Release
sudo systemctl restart chaos-server.service
sudo systemctl status chaos-server.service --no-pager
```

### Syncing player saves from local to live (manual, use with caution):
```powershell
scp -i "C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem" -r "D:\Elysium - Master\Server\Elysium.Server\Data\Saved\Aislings\" ubuntu@18.222.119.157:~/Chaos-Server/Data/Saved/Aislings/
```

### Syncing password/access files from local to live:
```powershell
scp -i "C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem" -r "D:\Elysium - Master\Server\Elysium.Server\Data\Configuration\Access\" ubuntu@18.222.119.157:~/Chaos-Server/Data/Configuration/Access/
```

---

## SSH into Live Server

```powershell
ssh -i "C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem" ubuntu@18.222.119.157
```

**Key location:** `C:\Users\roman\Downloads\LightsailDefaultKey-us-east-2.pem` — verified still
present, unaffected by the repo reorg (it was never inside either repo).
**Server IP:** `18.222.119.157`
**User:** `ubuntu`

### Common SSH commands once connected:
```bash
sudo systemctl status chaos-server.service --no-pager
sudo systemctl restart chaos-server.service
journalctl -u chaos-server.service -f --no-pager
journalctl -u chaos-server.service --no-pager -n 50
cd ~/Chaos-Server
git log --oneline -3
git pull origin linux-porting-fixes
```

---

## Running the Local Server

**New standard way:**
```powershell
cd "D:\Elysium - Master\Server\Elysium.Server"
.\ensure-server.ps1
```
Checks whether ports 4200/4201/4202 are already listening before doing anything (avoids
accidentally starting a second instance), launches the server detached in its own window if not,
and polls for up to 90s to confirm it actually came up - fails loudly with the real error surface
(the new console window) if it didn't, instead of you finding out mid-testing.

**What this replaces:** the old doc's raw `cd D:\Chaos-Server && dotnet run --project
Chaos/Chaos.csproj`. That command still works underneath (it's literally what `ensure-server.ps1`
launches), but has none of the above safety checks - use it directly only if you specifically need
the server blocking your current shell (e.g. to watch its console output inline) rather than
running detached:
```powershell
cd "D:\Elysium - Master\Server\Elysium.Server"
dotnet run --project Chaos/Chaos.csproj
```

**Local ports:**
- Lobby: `127.0.0.1:4200`
- Login: `127.0.0.1:4201`
- World: `127.0.0.1:4202`

**Live ports (same, different IP):**
- Lobby/Login/World: `18.222.119.157:4200`/`4201`/`4202`

**Local content path:** `D:\Elysium - Master\Server\Elysium.Server\Data\`
**Live content path:** `/home/ubuntu/Chaos-Server/Data/`

---

## Deploying the Client (new section - not in the old doc)

The old doc had no client-deploy section at all. `deploy-client.ps1` lives in the **Client repo**,
not this one:

```powershell
cd "D:\Elysium - Master\Client\Elysium.Client"
.\deploy-client.ps1
```

Publishes `Chaos.Client` (Release, win-x64, self-contained) and deploys it to
`D:\Elysium-Test\Elysium.exe` - the only proven-working launch location (the raw Debug build
folder has no `Data\` next to it and won't launch).

### The staleness bug (found and fixed this session)
`dotnet publish`'s own incremental-build cache could decide a project didn't need recompiling
despite genuine source changes - most likely triggered by `obj\`'s cache markers being left
inconsistent by an earlier failed/interrupted build (a file-lock build failure from an
already-running client process is exactly that kind of interruption). When this happened, the
script still reported **"Deploy complete!"** while silently serving stale output - a confident
lie, not an obvious failure.

**Fixed, permanently, not just worked around**: the script now unconditionally deletes `obj\`/
`bin\` for `Chaos.Client` and its 3 local project references before every single publish (making
the staleness structurally impossible, at the cost of always doing a full rebuild), adds
robocopy's `/IS` flag as defense-in-depth, and - the part that actually catches it if the above
somehow fails - **self-verifies freshness after every deploy**: checks `Chaos.Client.dll`'s
timestamp is within 5 minutes of "now" and aborts loudly (red text, non-zero exit) rather than
trusting exit code 0 if it isn't.

**Takeaway for any future deploy/build script:** don't trust "the command exited 0" as proof of
freshness on its own if incremental caching is anywhere in the picture - verify the actual
output artifact's timestamp.

---

## EPF Sprite Pipeline (chaos-cli)

**Verified still accurate as-is** - this tool lives outside both reorganized repos
(`D:\Elysium-Tools\`, not `D:\Elysium - Master\`), so it was untouched by tonight's move.

**Tool location:** `D:\Elysium-Tools\ChaosAssetManager\ChaosAssetManager.Cli\bin\Debug\net10.0\chaos-cli.exe`
**Source:** `D:\Elysium-Tools\ChaosAssetManager\` (dalib dependency at `D:\Elysium-Tools\dalib\`)

> Don't confuse this with `D:\Elysium - Master\Tools\Elysium.SpriteBuilder` - that's a separate,
> newer WinForms tool built this session for a different purpose. `ChaosAssetManager`/`chaos-cli`
> is the original EPF/dat-archive CLI pipeline described below; `Elysium.SpriteBuilder` doesn't
> replace it.
>
> For upstream setup/clone instructions (not covered here), see
> `docs/articles/ChaosAssetManager.md` in this repo.

**Add to PATH for current PowerShell session:**
```powershell
$env:Path += ";D:\Elysium-Tools\ChaosAssetManager\ChaosAssetManager.Cli\bin\Debug\net10.0"
```

**Rebuild after changes:**
```powershell
cd D:\Elysium-Tools\ChaosAssetManager\ChaosAssetManager.Cli
dotnet build
```

### List all entries in an archive:
```powershell
.\chaos-cli.exe list "D:\Program Files (x86)\KRU\Dark Ages\roh.dat"
.\chaos-cli.exe list "D:\Program Files (x86)\KRU\Dark Ages\roh.dat" --epf-only
```

### Export frames from an effect for editing in Aseprite:
```powershell
.\chaos-cli.exe epf-export "D:\Program Files (x86)\KRU\Dark Ages\roh.dat" efct157 --output D:\sprites\efct157\
```
Exports each frame as a numbered PNG: `frame_000.png`, `frame_001.png`...
Also exports `palette.pal` and `metadata.json`.

### Import edited frames back into archive:
```powershell
.\chaos-cli.exe epf-import "D:\Program Files (x86)\KRU\Dark Ages\roh.dat" efct157 D:\sprites\efct157\
```
Reads `frame_000.png` through `frame_NNN.png`, packages as EPF, patches back into archive.

### Transfer an effect from one archive to another:
```powershell
# Single effect
.\chaos-cli.exe transfer-effect "source\roh.dat" "target\roh.dat" 137 --family efct

# Batch transfer
.\chaos-cli.exe transfer-effect "source\roh.dat" "target\roh.dat" 130 131 132 133 --family efct

# Traveling projectile effects (mefc family)
.\chaos-cli.exe transfer-effect "source\roh.dat" "target\roh.dat" 007 --family mefc
```
Always lands at a slot PAST the end of target's effect list — no collision risk.
Outputs: `Transferred efct137 from source -> efct401 in target`

### Extract specific files from archive:
```powershell
.\chaos-cli.exe extract-by-name "roh.dat" efct157.epf efct157.tbl --output D:\extracted\
```

### Patch files into archive:
```powershell
.\chaos-cli.exe patch "roh.dat" efct400.epf efct400.tbl
```

### Archive contents by type:
| Archive | Contents |
|---|---|
| `roh.dat` | Effects (efct/mefc), display sprites |
| `hades.dat` | Monster/NPC sprites |
| `khanmad.dat` | Male accessories |
| `khanwad.dat` | Female accessories |
| `khanmeh.dat` | Male hair/headgear |
| `khanweh.dat` | Female hair/headgear |
| `khanmim.dat` | Male armor/footwear |
| `khanwim.dat` | Female armor/footwear |
| `khanmns.dat` | Male weapon full body |
| `khanwns.dat` | Female weapon full body |
| `khanmtz.dat` | Male armor animations + weapons |
| `khanwtz.dat` | Female armor animations + weapons |
| `legend.dat` | Items, icons, UI |

---

## Character Management (Live Server)

All paths below verified consistent with `~/Chaos-Server` being the correct, unrenamed remote path.

### Grant GM status:
```bash
sed -i 's/"isAdmin": false/"isAdmin": true/g' ~/Chaos-Server/Data/Saved/Aislings/<name>/aisling.json
# If isAdmin field is missing entirely:
sed -i 's/"x":/"isAdmin": true,\n  "x":/' ~/Chaos-Server/Data/Saved/Aislings/<name>/aisling.json
```

### Reset character level to 1:
```bash
sed -i 's/"level": [0-9]*/"level": 1/g' ~/Chaos-Server/Data/Saved/Aislings/<name>/aisling.json
sed -i 's/"toNextLevel": [0-9]*/"toNextLevel": 100/g' ~/Chaos-Server/Data/Saved/Aislings/<name>/aisling.json
```

### Change character gender:
```bash
sed -i 's/"bodySprite": "Male"/"bodySprite": "Female"/g' ~/Chaos-Server/Data/Saved/Aislings/<name>/aisling.json
sed -i 's/"gender": "Male"/"gender": "Female"/g' ~/Chaos-Server/Data/Saved/Aislings/<name>/aisling.json
```

### Wipe all characters (nuclear option):
```bash
cd ~/Chaos-Server/Data/Saved/Aislings
rm -rf */
```

### Create fresh character from template:
```bash
cp -r ~/Chaos-Server/Data/Saved/Aislings/valk ~/Chaos-Server/Data/Saved/Aislings/<newname>
sed -i 's/"name": "valk"/"name": "<NewName>"/g' ~/Chaos-Server/Data/Saved/Aislings/<newname>/aisling.json
```

### Fix "username taken" but can't log in (missing password file):
```bash
rm -rf ~/Chaos-Server/Data/Configuration/Access/<Name>
```

### Case sensitivity note (IMPORTANT):
Linux is case-sensitive. The save folder name must match exactly what the player types at login.
- Player types "Ace" → server looks for `Data/Saved/Aislings/Ace/`
- Player types "ace" → server looks for `Data/Saved/Aislings/ace/`
- Mismatch = disconnect on login

---

## Known Gotchas

**1. Password files blocked git pull:**
If `git pull` fails with "untracked files would be overwritten":
```bash
rm -f Data/Configuration/Access/*/password.txt
rm -f Data/Configuration/Access/blacklist.txt
rm -f Data/Configuration/Access/whitelist.txt
rm -f Data/Configuration/Access/clientIdBan.txt
git pull origin linux-porting-fixes
```
*(Carried forward from the old doc - this is a remote/live-server-side scenario, not
independently re-verified via live SSH this pass. The equivalent local `.gitignore` entries
already exist in this repo, if that's relevant context for a future check.)*

**2. ConfigurableScriptBase requires scriptVars:**
Any script extending `ConfigurableScriptBase` (skills, spells, dialogs, effects, etc.) MUST have a
matching `scriptVars` entry in its JSON template or construction throws. Error: `ScriptVars for
script "X" were not found`. Confirmed still exactly how it works - this bit multiple things built
this session (Slayer's evolving abilities, the Sorcerer dialog scripts) and is worth remembering
as a first-checklist item whenever a `Configurable*ScriptBase`-derived class won't load.

**3. Aisling AC clamp:**
Player AC is clamped to **-90** minimum (vs **-99** for monsters) - re-verified against
`Chaos/appsettings.json`'s `WorldOptions:MinimumAislingAc`/`MinimumMonsterAc`, still current.
Can't use AC manipulation for true invulnerability on players - use tag-based damage negation
instead. *(The old doc's example, `AxeBlockEffect`, no longer exists in the codebase - the
current, actively-used example of this exact pattern is `StaciasBulwarkEffect`'s
`InvulnerableTag`, checked as a hard short-circuit at the very top of
`ApplyAttackDamageScript.ApplyDamage`'s Aisling branch, before any other mitigation.)*

**4. Template keys are case-sensitive:**
`dagger_Rat` ≠ `dagger_rat`. Always use lowercase for template keys.

**5. File rename requires full restart:**
`/reload` only reloads content. Renaming or moving files requires full server restart.

**6. Live-only content:**
`chillStartingZone` and `map20001` exist on the live server but NOT in the local repo. These are
untracked - back them up before any operation that could wipe the server. *(Carried forward,
not independently re-verified via live SSH this pass - would need to actually connect to
confirm these still exist and nothing new has joined this list.)*

**7. PowerShell BOM encoding gotcha (new):**
`Set-Content -Encoding utf8` in Windows PowerShell 5.1 writes UTF-8 **with a BOM**, which the JSON
parser chokes on (`'0xEF' is an invalid start of a value`). Prefer the `Write`/`Edit` tools for
JSON; if you must use PowerShell to write JSON, verify with a hex check (`EF BB BF` prefix =
corrupted) or write via
`[System.IO.File]::WriteAllText(path, content, (New-Object System.Text.UTF8Encoding($false)))`.
See `builder.md` for the full framing of this.

**8. Commit staging must be explicit, not `git add -A` (new):**
An early version of `Elysium.DevPanel`'s combined "Commit + Push" button used an unscoped
`git add -A`, which swept in manual backup zips and clobbered a scratch content file in the same
push. Fixed at two levels: `Elysium.DevPanel` now shows the full file list (with scratch-like
files flagged, not silently filtered) before ever staging, and commit/push are separate buttons
in separate sections (see the DevPanel section above) so a "just commit locally" action can never
accidentally also push. The same discipline applies manually: review `git status` before staging,
stage specific paths, don't reach for `-A` as a default.

---

## GM Commands (In-Game)

For how the command system itself works (creating new commands, argument parsing, admin-gating),
see `docs/articles/Commands.md` in this repo - that's architecture documentation, not a command
cheat-sheet, so it doesn't duplicate this list; this is the only actual reference for real command
syntax to type in-game. All command names below re-verified to still exist in
`Chaos/Messaging/Admin/`.

```
/traverse <mapId>           — teleport to map
/traverse <mapId> <x> <y>  — teleport to specific coords
/spawnMonster <templateKey> — spawn a monster
/learn skill <templateKey>  — learn a skill
/learn spell <templateKey>  — learn a spell
/create <templateKey>       — create an item
/setlevel <N>               — set character level
/summon <characterName>     — summon a player to you
/reload items               — reload item templates
/reload skills               — reload skill templates
/reload spells               — reload spell templates
/reload monsters             — reload monster templates
/reload maps                  — reload map templates
```

### Test-only commands (new, added this session)
```
/testfloorupdate <1-10>                    — simulates entering an Ascension Chamber floor,
                                              for floor-tracker packet testing (see
                                              FLOOR_TRACKER_DESIGN.md)
/sorcererset <element1> <element2>         — TEST-ONLY: sets a Sorcerer's element picks and
                                              grants the full resulting kit immediately,
                                              bypassing level gates and the real two-step
                                              dialog flow. e.g. "/sorcererset Fire Earth" for
                                              Magma, "/sorcererset Fire Fire" for pure Ignis.
                                              Reuses the same SorcererProgressionHelper/
                                              ISorcererModuleProvider the real dialog flow
                                              uses - not a parallel grant path, so a character
                                              set up this way is representative of a real one.
```
