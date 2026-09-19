# GunGame Arena

A companion plugin for Kodeman's **GunGame**. Works on every GunGame map and with GunGame Progressions.

## What it does

- **Free For All** (default): every sosig fights every other sosig, and you.
- **Teams**: blue (you plus allies) versus red, optionally green and yellow. Ally kills never advance your weapon.
- **Natural match flow**: spread-out spawns, sosigs hold grudges against a few rivals at a time, a share of them hunt you, and each has a skill tier (Rookie to Elite) that changes aim, not fire rate.
- **Leaderboard HUD**: floating Arsenal-style cards with sosig head portraits, generated usernames, kill counts, crowns and team colours. Your card uses your Steam avatar.

## Config

`BepInEx/config/shaha.GunGameArena.cfg` after first launch. Sections: `Arena` (Mode, TeamCount, AllySosigs), `Leaderboard` (size, distance, names), `Behaviour` (spawns, grudges, hunters, tiers), `Tier.*` multipliers.

Sosig count is GunGame's own setting on the in-map panel.

## Credits

Kodeman for GunGame. Built with BepInEx and Harmony.
