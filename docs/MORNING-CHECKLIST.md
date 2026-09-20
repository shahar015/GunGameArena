# GunGame Arena — in-game verification checklist

Everything below was built and code-reviewed overnight but has **not** been run in the headset.
The plugin DLL is already in your Thunderstore profile (`Default`), so just launch H3VR from the mod manager.

Log file: `%APPDATA%\Thunderstore Mod Manager\DataFolder\H3VR\profiles\Default\BepInEx\LogOutput.log`
Config file (created on first launch): `...\profiles\Default\BepInEx\config\shaha.GunGameArena.cfg`

Tip: after each session, search the log for `GunGame Arena` and paste anything with `[Error` back to me.

## Session 1 — Free For All (default config)

Map: **NuketownGunGame** (or any GunGame map). Start a round from the panel, play about five minutes, die at least once.

- [ ] Log has `GunGame Arena 0.1.0 loaded. Mode=FreeForAll Leaderboard=True` and no `[Error  :GunGame Arena]` lines.
- [ ] Log has `Roster reset: 8 sosigs, mode FreeForAll…` followed by 8 `slot N:` lines with different names, IFFs 1..8 and a mix of tiers.
- [ ] Sosigs shoot **each other**, not only you. Log `Spawned … iffN … (game IFF N)` lines show the same N on both sides of each line.
- [ ] Initial sosigs are spread around the map rather than clustered.
- [ ] Some sosigs come looking for you. Log has `Hunters: name, name` every 10–20 s naming about two of eight.
- [ ] Log has `Grudges <name> -> a, b, c` at round start and every 20–40 s; your Steam name appears in roughly half of them.
- [ ] **No** `Blocked progression credit` lines appear in FFA (they belong to Teams mode only).
- [ ] Restart the round from the in-map panel without changing map: `Hunters:` lines keep the same 10–20 s cadence (no doubling).
- [ ] After you shoot a sosig that wasn't hunting you, it keeps fighting you for at least the next grudge re-roll (log line ends with `+1 grudge`).
- [ ] A sosig you shoot turns on you immediately, even if it wasn't hunting you.
- [ ] Kill credit: `KILL <sosig> -> <sosig>` for sosig-on-sosig, `KILL <YourSteamName> -> <sosig>` for yours, `KILL <sosig> -> <YourSteamName> (player…)` when you die.
- [ ] Your weapon advances **only** on your own kills.
- [ ] `^` sosigs (Rookie) miss noticeably more than `^^^^` (Elite). Log has `Tier <X> applied to <name>` per spawn and **no** `reaction fields not found` warning.

### HUD
- [ ] Header **Free For All** with up to six cards, floating about a metre ahead and a bit above eye line, following your head turn smoothly.
- [ ] Sosig cards show a rendered head (hat visible). If heads are cut off, from behind or empty, tell me: the fix is a one-line change of camera facing.
- [ ] Your card shows your **Steam avatar** and has a white outline. If it shows a grey silhouette instead, check the log for `Steam avatar unavailable`.
- [ ] Kill numbers update on kills. The leader has a **gold border and crown**; nobody has a crown before the first kill.
- [ ] Names under cards; chevrons `^`…`^^^^` under sosig names.
- [ ] If you drop out of the top five, your card is pinned at the far right with a small gap before it.

## Session 2 — Teams

Edit the config: `[Arena] Mode = Teams` (keep TeamCount 2, AllySosigs -1). Restart the game.

- [ ] Header **Team Deathmatch**; blue card backgrounds for you + 4 allies, red for the 4 enemies.
- [ ] Blue sosigs never shoot you and never appear under `Hunters:`.
- [ ] One crown per team (best scorer of blue, best of red), none for a team with zero kills.
- [ ] Log shows `Blocked progression credit: kill was by an ally` when a blue sosig kills someone, and your weapon does **not** advance on it.
- [ ] Number of weapons controls on GunGame's board are dimmed and unclickable; log `'Number of weapons' controls locked`.
- [ ] Reaching the last weapon loops back to the first (`weapon rotation looped`).
- [ ] Shooting a blue sosig does nothing (Friendly fire OFF).
- [ ] Panel shows Points to win and Friendly fire rows when Mode = Teams.

## Session 3 (optional) — zip install test

Only needed before publishing to Thunderstore.

1. In Thunderstore Mod Manager: Settings → *Import local mod* → `D:\Projects\gungamesosig\GunGameArena\dist\GunGameArena-0.1.0.zip`.
2. Delete the manually copied folder `...\profiles\Default\BepInEx\plugins\GunGameArena\` first (otherwise two copies of the plugin load).
3. Launch: log shows `GunGame Arena 0.1.0 loaded`.

## Session 4 — In-map Arena panel

- [ ] On any GunGame map a blue **Arena** panel stands to the left of GunGame's "More options" board, sized to match it. Log should read `Arena panel placed via more-options-board (board '<name>', <w>x<h> m).` If it instead falls back to a different strategy (`Arena panel placed via <strategy>.`), or the panel is missing or misplaced, paste the log lines starting `Arena panel placed via` and the `[Panel dump]` block (includes a `More-options board:` section and a `WeaponPoolSelectionPanel` children listing).
- [ ] Laser pointer + trigger works on every button; toggles flip their ON/OFF text; `<`/`>` step values.
- [ ] Toggling **Leaderboard** mid-round hides/shows the HUD immediately.
- [ ] Set Mode to Teams; Teams and Allies rows brighten; Start Game → header "Team Deathmatch".
- [ ] Values persist: quit, relaunch, panel shows what you set (they are in `shaha.GunGameArena.cfg`).

## Things you can tune without rebuilding

All in `shaha.GunGameArena.cfg`:

| Want | Change |
|---|---|
| More pressure on you | `[Behaviour] HunterShare` 0.25 → 0.5, or `PlayerRivalWeight` 2 → 4 |
| Fewer / more sosigs | GunGame's own +/- on the in-map panel |
| Harder sosigs | `[Behaviour] TierWeights` e.g. `10,30,35,25` |
| HUD too big / close | `[Leaderboard] Scale`, `Distance`, `Height` |
| Arena panel too big / small | `[Panel] Scale` 1.0 → 0.5–2.0 |
| Only the leaderboard, original GunGame combat | `[Arena] Mode = Off` |
