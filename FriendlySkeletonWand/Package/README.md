# Friendly Skeleton Wand

This mod adds a craftable wand which can be used to create skeletons with. These skeletons are supposed to guard your base and hang around the general vicinity of wherever you create them.

##  About Me

<a target="_blank" rel="noopener noreferrer" href="https://chebgonaz.pythonanywhere.com"><img alt="Necrobase" style="margin-top: 1%" src="minicheb.png" /></a> <a target="_blank" rel="noopener noreferrer" href="https://ko-fi.com/chebgonaz"><img alt="Ko-fi" style="margin-top: 1%" src="kofi-mug-small.png" /></a> <a target="_blank" rel="noopener noreferrer" href="https://www.patreon.com/chebgonaz?fan_landing=true"><img alt="Patreon" style="margin-top: 1%" src="patreon.png" /></a>

I'm a YouTuber/Game Developer/Modder who is interested in all things necromancy and minion-related. Please check out my [YouTube channel](https://www.youtube.com/channel/UCPlZ1XnekiJxKymXbXyvkCg) and if you like the work I do and want to give back, please consider supporting me on [Patreon](https://www.patreon.com/chebgonaz?fan_landing=true) or throwing me a dime on [Ko-fi](https://ko-fi.com/chebgonaz). You can also check out my [website](https://chebgonaz.pythonanywhere.com) where I host information on all known necromancy mods, games, books, videos and also some written reviews/guides.

Thank you and I hope you enjoy the mod!

## Requirements

- Valheim Mistlands
- BepInEx
- Jotunn

## Installation (manual)

- Drag the `FriendlySkeletonWand` folder from inside the archive to your Bepinex plugins folder in the Valheim directory.

## Features

- Craftable Wand at the workbench called **Friendly Skeleton Wand**.
- Pressing **B** while the Wand is equipped will create a friendly skeleton guard with **Bone Fragments** from your inventory.
- Nearby skeletons can be told to **Wait** by pressing **T**.
- Nearby skeletons can be told to **Follow** by pressing **F**.
- Skeleton quality increases with **Necromancy Skill Level**:
	+ 0 to 34: Level 1 skeletons.
	+ 35 to 69: Level 2 skeletons.
	+ 70+: Level 3 skeleotns.
- Skeletons created are either an archer or warrior. This is random.
- Minions ignore collision with the player so you won't be trapped.
- **G** to **teleport** minions to you.
- **R** to tell minions to **attack** a specific target.
- Starting at level 25, a wraith spawns to guard you at all times (until it is destroyed - then it respawns again after a little while)
- Tweakable settings by pressing **F1** to edit the config.

### Config

~~**Attention:** To edit the config as described, the [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) is required. This is a separate mod. Please download and install it.~~

~~Press **F1** to open the mod's configuration panel where you can modify the following:~~

Doesn't work for some reason; please edit the configs manually.

Property | Value | Notes
--- | --- | ---
`BoneFragmentsRequired` | `integer` | How many **Bone Fragments** are consumed from your inventory when creating a skeleton (default `3`, set to `0` for free skeletons).
`NecromancyLevelIncrease` | `float` | How much the **Necromancy Skill**'s level increases when you create a skeleton (default `0.25`).
`SkeletonsPerSummon` | `integer` | How many skeletons are created each time you create a skeleton (default `1`, set to larger numbers  for unbridled madness [**Warning: Your game might crash - this really is unlimited!**]).
`SkeletonHealthMultiplier` | `float` | `Necromancy Level * Health multiplier = Skeleton's HP`.
`BoneFragmentsDroppedAmountMin` | `integer` | The minimum amount of bones a creature will drop.
`BoneFragmentsDroppedAmountMax` | `integer` | The maximum amount of bones a creature will drop.

## Changelog

Date | Version | Notes
--- | --- | ---
25/11/2022 | 1.0.0 | Release
25/11/2022 | 1.0.1 | Fix a bug that let you create skeletons without the wand equipped.
26/11/2022 | 1.0.2 | All creatures drop bone fragments, skeletons can be told to follow/wait, skeletons health scales with necromancy level.
26/11/2022 | 1.0.3 | Default skeleton health multiplier jacked up to 15.
27/11/2022 | 1.0.4 | Amount of bone fragments dropped by creatures exposed to config file; fixed a bug where at necromancy level 0 a skeleton spawned would instantly die
27/11/2022 | 1.0.5 | Fixed a bug where after logging out and coming back in your skeletons wouldn't respond to commands anymore
27/11/2022 | 1.0.6 | Function added to teleport minions to you and to tell minions to attack what you're looking at.
29/11/2022 | 1.0.7 | Add Guardian Wraith; ignore minion collision on player

## Known issues

- Skeletons can't follow you into/out of dungeons
- Telling minions to attack what you're looking at (by spawning a big stone there - dumb but will be replaced with something more appropriate later)

## Source

You can find the github [here](https://github.com/jpw1991/Friendly-Skeleton-Wand).
