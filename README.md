# Cheb's Necromancy

Cheb's Necromancy adds Necromancy to Valheim via craftable wands and structures. Minions will follow you, guard your base, and perform menial tasks.

This mod was formerly called Friendly Skeleton Wand, but was renamed since it as grown into something so much more.

## Confused? Try the [wiki](https://github.com/jpw1991/chebs-necromancy/wiki).

**Pre-release versions:** To get the latest improvements but with less testing, check the [GitHub's releases page](https://github.com/jpw1991/chebs-necromancy/releases). Although less tested than the official releases, they are still tested pretty well.

##  About Me

[![image1](https://imgur.com/Fahi6sP.png)](https://chebgonaz.pythonanywhere.com)
[![image2](https://imgur.com/X18OyQs.png)](https://www.patreon.com/chebgonaz?fan_landing=true)
[![image3](https://imgur.com/4e64jQ8.png)](https://ko-fi.com/chebgonaz)

I'm a YouTuber/Game Developer/Modder who is interested in all things necromancy and minion-related. Please check out my [YouTube channel](https://www.youtube.com/channel/UCPlZ1XnekiJxKymXbXyvkCg) and if you like the work I do and want to give back, please consider supporting me on [Patreon](https://www.patreon.com/chebgonaz?fan_landing=true) or throwing me a dime on [Ko-fi](https://ko-fi.com/chebgonaz). You can also check out my [website](https://chebgonaz.pythonanywhere.com) where I host information on all known necromancy mods, games, books, videos and also some written reviews/guides.

Thank you and I hope you enjoy the mod! If you have questions or need help please join my [Discord](https://discord.com/invite/EB96ASQ).

## Reporting Bugs & Requesting Features

If you would like to report a bug or request a feature, the best way to do it (in order from most preferable to least preferable) is:

a) Create an issue on my [GitHub](https://github.com/jpw1991/chebs-necromancy).
b) Create a bug report on the [Nexus page](https://www.nexusmods.com/valheim/mods/2040?tab=bugs).
c) Write to me on [Discord](https://discord.com/invite/EB96ASQ).
d) Write a comment on the [Nexus page](https://www.nexusmods.com/valheim/mods/2040?tab=posts).

## Requirements

- Valheim Mistlands
- BepInEx
- Jotunn

## Installation (manual)

- Drag the `ChebsNecromancy` folder from inside the archive to your BepInEx plugins folder in the Valheim directory.

## Features

- Craftable wands at the workbench called **Skeleton Wand** and **Draugr Wand**.
- These wands consume **Bone Fragments** to create minions.
- **Bone Fragments** now drop from all creatures when they die.
- With a wand equipped, the following is possible:
	+ **B** will make a warrior.
	+ **H** will make an archer.
	+ **F** will make all nearby minions **follow** you.
	+ **T** will make all nearby minions **wait**.
	+ **G** will teleport all following minions to your position (useful if stuck or to get them on boats)
	+ **R** to tell minions to **attack** a specific target (this is relatively inaccurate).
- A new **Necromancy Skill**.
- Minion quality increases with **Necromancy Skill Level**:
	+ 0 to 34: Level 1.
	+ 35 to 69: Level 2.
	+ 70+: Level 3.
- Minion health increases with necromancy level.
- Minions ignore collision with the player so you won't be trapped.
- You can make a special cape called the Spectral Shroud which draws spirits to it. If you're powerful enough, they will serve (necromancy level 25). Removing the cape stops them from spawning.
- Tweakable settings by editing the config file.
- A Spirit Pylon which can be constructed to serve as static base defence. It detects nearby enemies and if any are found, it spawns temporary Ghosts to defend the base with.
- Having additional items in your inventory can change your minions. As a safety measure to prevent you from accidently consuming stuff, hold Left Shift when creating a minion to permit these resources to be consumed (or set to None in the config if you want no checking whatsoever):
  - Boar scraps or Deer Hide -> leather armor
  - Bronze -> Bronze armor
  - Iron -> Iron armor
  - Black Metal -> Black Iron armor
  - Guck -> poison skeleton
  - Surtling core -> skeleton mage
- As of 1.5.0 you can create a Neckro Pylon which spawns undead Necks from Neck Tails with a container on its back. It wanders around gathering up items for you. It's like a walking vacuum cleaner.
1.5.0 also introduces a refueler pylon, that fills your smelters with ores and coal or wood, and a Bat Beacon which spawns bats to defend your base.

### Config

**Attention:** To edit the config as described, the [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) is the most user friendly way. This is a separate mod. Please download and install it.

Press **F1** to open the mod's configuration panel.

You can also edit the configs manually. Almost everything can be tweaked to your liking.

## Known issues

- Skeletons can't follow you into/out of dungeons
	+ This might be fixable via mods like **Teleport Everything** ([Nexus](https://www.nexusmods.com/valheim/mods/1806), [Thunderstore](https://valheim.thunderstore.io/package/OdinPlus/TeleportEverything/)).
- Telling minions to attack what you're looking at (by spawning a big stone there - dumb but will be replaced with something more appropriate later)
- Players with Radeon cards may experience weird issues. I don't know what's causing it, but turning off Draugr in the config may help because it seems related. If you encounter problems, try the following:
	+ `DraugrAllowed = false`
	+ `SpectralShroudSpawnWraith = false`	
- Pylons won't show inventory unless you open a normal container first.

## Future Ideas

- Fully custom undead types.

## Source

You can find the github [here](https://github.com/jpw1991/chebs-necromancy).

## Special Thanks

- [**Dracbjorn**](https://github.com/Dracbjorn) for development help & testing.
- **Ramblez** (aka **[Thorngor](https://www.nexusmods.com/users/21532784)** on the Nexus) for texturing help and for making the custom icons.
- **redseiko** for helpful advice on the official Valheim modding Discord.
- **S970X** for making the German language localization for the mod.
- **Jetbrains** for kindly providing me with an [Open Source Development license](https://jb.gg/OpenSourceSupport) for their Rider product which makes development on this project smooth and easy.

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider.svg" alt="Rider logo." width=50% height=50%>

## Changelog

<details>
<summary>2023</summary>

Date | Version | Notes
--- | --- | ---
11/02/2023 | 1.7.7 | Neckro pylons only spawn neckros if a player is nearby and that player takes ownership of the Neckro; fix bug that permitted non-admins to use some commands; optimise draugr & skeleton Awake scaling
10/02/2023 | 1.7.6 | Dracbjorn's config overhaul; optimise imports
09/02/2023 | 1.7.5 | Neckro messages hideable; Neckro messages improved
07/02/2023 | 1.7.4 | refueler pylons can now refuel fireplaces eg. torches, bonfires, etc.
06/02/2023 | 1.7.3 | minions remember if they were following a player after player logs off, then continue following when player returns
05/02/2023 | 1.7.2 | allow minions to roam when holding shift+T; fix bug in ZDO comparison
05/02/2023 | 1.7.1 | allow admins to ignore ownership with commands; fix bug where players wouldnt be found by command; lint the entire project; catch and fix a few instances of MonoBehaviours accidently being instantiated with new -> a big no-no
03/02/2023 | 1.7.0 | minions now remember the necromancy level with which they're created and scale to that; refactor SkeletonWand and DraugrWand code to be more uniform to make diffing easier; fix a bug where minions set to follow automatically would have bugged AI with the 1.6.4 improvements; minion commandability exposed to config; commands issued via E also update ZDO; hover text for interact patched
01/02/2023 | 1.6.4 | minions can be configured to drop their crafting requirements on death; hold position now works in that minions no longer wander around; wait positions are now recorded and stored so that minions return to where they were last waiting after chasing something off
28/01/2023 | 1.6.3 | add optional timer to kill any minion after X seconds; overhaul minion ownership checks to accurately store and retrieve the minion's creator; minions will only obey commands from their creators and ignore others
24/01/2023 | 1.6.2 | minions can be told to follow/wait using E on them; neckros can be killed via terminal command - butcher's knife won't work on them, even with Tameable component added, due to their Container component
20/01/2023 | 1.6.1 | Fix a problem where the Neckro Gatherer could delete items without storing them if its inventory size is set very small like 1x1
17/01/2023 | 1.6.0 | Tier 3 skeleton mages now throw goblin fireballs; poison skeletons have damage tiers and equipment options; add optional durability damage from making minions
16/01/2023 | 1.5.1 | rename to Cheb's Necromancy. No additional changes.
13/01/2023 | 1.5.1 | tone logging down a bit; fix string name on neckro pylon
12/01/2023 | 1.5.0 | add refueler pylon that puts coal into nearby smelters; fix pylons to face toward their Z axis (it was backwards before); add neckro gatherer pylon; neckros gain 150hp and can't attack; neckros no longer afraid of fire; update icons with brilliant designs by Ramblez; finally fix transparency issue
11/01/2023 | 1.4.3 | item and structure recipes exposed to config file; defensive spikes (standard & dverger)  no longer damage minions if they've been placed by the player; add chebgonaz_prerelease_spawnneckro command to let players test with new neckro gatherer.
09/01/2023 | 1.4.2 | Neckro Gatherer now brings items back to empty containers and stores them
09/01/2023 | 1.4.1 | Fix minions not remembering their armor values
09/01/2023 | 1.4.0 | Minions no longer damage player structures; armor values actually applied to skeletons (although not remembered on logoff/login again - will fix soon); add necroneck gatherer
07/01/2023 | 1.3.0 | Add server sync so that clients must use the configuration options of the server
07/01/2023 | 1.2.1 | Add black iron armor; permit skeletons to use black iron armor; rebalance spirit pylon by introducing a delay and a limit (both configurable)
06/01/2023 | 1.2.0 | Fix bug of skeletons not remembering their weapons properly; fix problem of tier 3 melee weapon not being stronger than tier 2; add debug commands for players to kill all their skeletons or summon them all
05/01/2023 | 1.1.1 | Add left shift as key to unlock extra resource consumption; allow deer hide to be used for leather armor skeletons
04/01/2023 | 1.1.0 | Armored Skeletons and Skeleton Mages!
02/01/2023 | 1.0.25 | Fix bug where Poison Skeleton's HP is lower than it should be after leaving area and coming back
02/01/2023 | 1.0.24 | Add Poison Skeleton (requires Guck in inventory); Guardian Wraith despawns when entering portal; Alert range of all entities raised to 30

</details>

<details><summary>2022</summary>

Date | Version | Notes
--- | --- | ---
30/12/2022 | 1.0.23 | Guardian Wraith implementation changed: Guardian Wraiths only spawn if enemies are nearby and move immediately to intercept enemy; Guardian Wraiths exist on a timer and perish on their own
29/12/2022 | 1.0.22 | add neck tail to draugr meat; expose ghost timer to config; fix ghost selfdestruct by changing from coroutine to update; add localizations; fix problem with limits not working properly; setting buttons to None in config removes them from GUI
25/12/2022 | 1.0.21 | Fix bug about Spirit Pylon Ghosts not despawning
25/12/2022 | 1.0.20 | Add optional equipment effects; add Necromancer's Hood; permit omission of creatures completely via config
20/12/2022 | 1.0.19 | Max HP of minions is now remembered; Add code to make someone only able to command minions they created; in limit mode, existing minions replaced rather showing a msg
16/12/2022 | 1.0.17 | Nice models for the wands; skeleton equipment and damage scales to necromancy level; nice texture for Spectral Shroud; add config option to disable Guardian Wraith (permits purely cosmetic Spectral Shroud); made draugr optionally cost meat as well as bone fragments to create
12/12/2022 | 1.0.16 | Remove version check that kept annoying people; fix Guardian Wraith bug introduced in 1.0.15
09/12/2022 | 1.0.15 | Cleaned out and fixed up config (it contained one error); allow enabling/disabling of individual features (user request); add German localizations by S970X; fix a problem where minions beyond the follow radius could still follow the player
09/12/2022 | 1.0.14 | Rebalanced skeleton health modifiers
07/12/2022 | 1.0.13 | Add Spirit Pylon for static base defence; add **optional** minion limits for those who wish it; change skeleton eyes to blue to differentiate from deadraiser (pink) and enemy (red).
05/12/2022 | 1.0.12 | Add Vulkan support for Linux
03/12/2022 | 1.0.11 | Fix more shaders; add Spectral Shroud -> wraiths only summon when you wear it, and get dismissed when you take it off; removed drops from minions
03/12/2022 | 1.0.10 | Implement fix for Thunderstore Mod Manager that (hopefully) permits it to install correctly.
02/12/2022 | 1.0.9 | Fix some shaders that got messed up (white squares instead of particles on effects like arrow hit); allow minions to generate a little Necromancy XP for their master; fixed a null object exception in Guardian Wraith coroutine
01/12/2022 | 1.0.8 | Big Overhaul → Fully custom prefabs used for minions results in finer control: no more Blood Magic experience; removes randomness of the skeletons so you can choose warrior/archer; permits me to add draugr and wraiths that are not hostile when you log back in; permits me to (in the future) give custom equipment and textures/materials to the minions.
29/11/2022 | 1.0.7 | Add Guardian Wraith; ignore minion collision on player
27/11/2022 | 1.0.6 | Function added to teleport minions to you and to tell minions to attack what you're looking at.
27/11/2022 | 1.0.5 | Fixed a bug where after logging out and coming back in your skeletons wouldn't respond to commands anymore
27/11/2022 | 1.0.4 | Amount of bone fragments dropped by creatures exposed to config file; fixed a bug where at necromancy level 0 a skeleton spawned would instantly die
26/11/2022 | 1.0.3 | Default skeleton health multiplier jacked up to 15.
26/11/2022 | 1.0.2 | All creatures drop bone fragments, skeletons can be told to follow/wait, skeletons health scales with necromancy level.
25/11/2022 | 1.0.1 | Fix a bug that let you create skeletons without the wand equipped.
25/11/2022 | 1.0.0 | Release

</details>

