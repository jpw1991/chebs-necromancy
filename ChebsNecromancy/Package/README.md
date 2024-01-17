# Cheb's Necromancy

Cheb's Necromancy adds Necromancy to Valheim via craftable wands and structures. Minions will follow you, guard your base, and perform menial tasks like woodcutting, farming, mining.

This mod was formerly called Friendly Skeleton Wand, but was renamed since it as grown into something so much more.

## Confused? Try the [wiki](https://github.com/jpw1991/chebs-necromancy/wiki).

##  About Me

[![image1](https://imgur.com/Fahi6sP.png)](https://necrobase.chebgonaz.com)
[![image2](https://imgur.com/X18OyQs.png)](https://ko-fi.com/chebgonaz)
[![image3](https://imgur.com/4e64jQ8.png)](https://www.patreon.com/chebgonaz?fan_landing=true)

I'm a YouTuber/Game Developer/Modder who is interested in all things necromancy and minion-related. Please check out my [YouTube channel](https://www.youtube.com/channel/UCPlZ1XnekiJxKymXbXyvkCg) and if you like the work I do and want to give back, please consider supporting me on [Patreon](https://www.patreon.com/chebgonaz?fan_landing=true) or throwing me a dime on [Ko-fi](https://ko-fi.com/chebgonaz). You can also check out my [website](https://necrobase.chebgonaz.com) where I host information on all known necromancy mods, games, books, videos and also some written reviews/guides.

Thank you and I hope you enjoy the mod! If you have questions or need help please join my [Discord](https://discord.com/invite/EB96ASQ).

### Bisect Hosting

I'm partnered with [Bisect Hosting](https://bisecthosting.com/chebgonaz) to give you a discount when you use promocode `chebgonaz`.

![bisectbanner](https://www.bisecthosting.com/partners/custom-banners/b2629ae1-293a-4094-9d2d-002d14529a82.webp)

## Reporting Bugs & Requesting Features

If you would like to report a bug or request a feature, the best way to do it (in order from most preferable to least preferable) is:

a) Create an issue on my [GitHub](https://github.com/jpw1991/chebs-necromancy).

b) Write to me on [Discord](https://discord.com/invite/EB96ASQ).

c) Write a comment on the [Nexus page](https://www.nexusmods.com/valheim/mods/2040?tab=posts).

## Requirements

- Valheim
- BepInEx
- Jotunn

## Installation (manual)

- Drag the contents of the `plugins` folder from inside the archive to your BepInEx plugins folder in the Valheim directory.

### Cheb's Valheim Library

[Cheb's Valheim Library](https://jpw1991.github.io/chebs-valheim-library/index.html) (CVL) is a DLL that contains shared code across my mods. For example, both skeletons from Cheb's Necromancy and mercenaries from Cheb's Mercenaries inherit the `ChebGonazMinion` type from CVL. This permits mercenaries to be commanded by a wand, and vice versa.

My mods are bundled with the latest CVL at the time of their release, but if you want to upgrade, you can get the newest CVL [here](https://github.com/jpw1991/chebs-valheim-library/releases).

## Features

Detailed info in the [wiki](https://github.com/jpw1991/chebs-necromancy/wiki). Here's the short version:

- Almost everything is configurable. Minions too weak/overpowered? Tweak them.
- [Player vs Player (PvP)](https://github.com/jpw1991/chebs-necromancy/wiki/PvP) settings can be configured as of 4.5.0.
- As of 4.6.0, further minion [appearance customization](https://github.com/jpw1991/chebs-necromancy/wiki/Appearance-Customization) options are available. You can change the eye color of your minions and also give them special symbols on their capes.
- Craftable items at the workbench and forge:
	+ [**Skeleton Wand**](https://github.com/jpw1991/chebs-necromancy/wiki/item_skeletonwand): Summons skeleton warriors, archers, miners, and woodcutters.
	+ [**Draugr Wand**](https://github.com/jpw1991/chebs-necromancy/wiki/item_draugrwand): Summons draugr warriors and archers.
	+ [**Orb of Beckoning**](https://github.com/jpw1991/chebs-necromancy/wiki/OrbOfBeckoning) Summons skeleton mages and can be thrown to direct minions to go somewhere. It also sticks to enemies so that minions will chase after an enemy.
	+ [**Spectral Shroud**](https://github.com/jpw1991/chebs-necromancy/wiki/item_spectralshroud): A cloak that increases your Necromancy level, looks cool, and attracts spirits to it. If you're powerful enough, they will serve.
	+ [**Necromancer Hood**](https://github.com/jpw1991/chebs-necromancy/wiki/item_necromancerhood): A cool hood to match the cloak.
	+ [**Necromancer's Backpack**](https://github.com/jpw1991/chebs-necromancy/wiki/NecromancyBackpack): An optional integration with **AdventureBackpacks** ([Nexus](https://www.nexusmods.com/valheim/mods/2204), [Thunderstore](https://valheim.thunderstore.io/package/Vapok/AdventureBackpacks/)), which offers a Necromancy backpack that doubles as a Spectral Shroud.
- While holding a Skeleton Wand, Draugr Wand, or Orb of Beckoning you can control the minions:
	+ **B** will make a minion.
	+ **H** will cycle minion types (eg. switch from Warrior to Archer) created by **B**.
	+ **F** will make all nearby minions **follow** you.
	+ **T** will make all nearby minions **wait**.
	+ **Shift+T** will make minions **roam**.
	+ **G** will teleport all following minions to your position (useful if stuck or to get them on boats)
- A new **Necromancy Skill** which increases the quality of your minions and allows you to upgrade them.
- Minion health increases with Necromancy level.
- Minions **ignore collision** with the player so you won't be trapped.
- Minions do not deal damage to player structures and are not affected by sharpened stakes.
- Minions remember who created them and cannot be commanded by other players.
- **Upgrades:** Having additional items in your inventory when you summon can create an upgraded version of a minion. As a safety measure to prevent you from accidentally consuming stuff, hold Left Shift when creating a minion to permit these resources to be consumed (or set to None in the config if you want no checking whatsoever):
  - Boar scraps, Deer Hide, Scale Hide -> leather armor
  - Troll Hide -> Troll armor
  - Wolf Pelt -> Wolf armor
  - Lox Pelt -> Lox armor
  - Bronze -> Bronze armor
  - Iron -> Iron armor
  - Black Metal -> Black Iron armor
- Some minions require more than bones:
  - Guck -> poison skeleton
  - Surtling core -> skeleton mage
  - Flint -> skeleton lumberjack
  - Hard Antler -> Skeleton Miner
- New craftable structures:
	+ [**Neckro Pylon**](https://github.com/jpw1991/chebs-necromancy/wiki/piece_neckropylon): Spawns undead Neckro Gatherers from Neck Tails. These have a container on its back and wander around gathering up items for you. It's like a walking vacuum cleaner. When full, the Neckro will find a nearby container and dump the items inside.
	+ [**Refueler Pylon**](https://github.com/jpw1991/chebs-necromancy/wiki/piece_refueler): Fills your smelters with ores/coal; kilns with wood; fireplaces with wood and meat; and torches etc. with resin, guck, eyeballs, whatever.
	+ [**Bat Beacon**](https://github.com/jpw1991/chebs-necromancy/wiki/piece_batbeacon): Spawns bats to defend your base.
	+ [**Spirit Pylon**](https://github.com/jpw1991/chebs-necromancy/wiki/piece_spiritpylon): Spawns spirits to defend your base.
	+ [**Farming Pylon**](https://github.com/jpw1991/chebs-necromancy/wiki/FarmingPylon): Automatically harvests your crops. Combine with **Seed Totem** ([Nexus](https://www.nexusmods.com/valheim/mods/876), [Thunderstore](https://valheim.thunderstore.io/package/MathiasDecrock/SeedTotem/)) to then automatically replant those seeds for an automated base.
	+ [**Repair Pylon**](https://github.com/jpw1991/chebs-necromancy/wiki/RepairPylon): Consumes resin to repair your structures.
	+ [**Treasure Pylon**](https://github.com/jpw1991/chebs-necromancy/wiki/TreasurePylon): Organizes your chests in a rather chaotic way. By default only works on the basic wooden boxes but can be told to sort other things too via config. The idea is that Neckros haul all this stuff back, then the Treasure Pylon organizes it.
	+ [**Phylactery**](https://github.com/jpw1991/chebs-necromancy/wiki/Phylactery): The player who creates the phylactery will be saved from death by being teleported to the phylactery rather than killed. Each rescue costs one dragon egg and if there are no dragon eggs loaded into the phylactery, then the player will die normally.

### Config

**Attention:** To edit the config as described, the [Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/) is the most user friendly way. This is a separate mod. Please download and install it.

Press **F1** to open the mod's configuration panel.

You can also edit the configs manually. Almost everything can be tweaked to your liking. For a complete list of all configuration options, please look [here](https://github.com/jpw1991/chebs-necromancy/wiki/Configs).

## Known issues

- Players with Radeon cards may experience weird issues. I don't know what's causing it, but it's linked to the particle effects. You can switch them off by turning `RadeonFriendly = true` in the config.

## Known Incompatibilities

- Soft incompatibility with [slope combat fix](https://github.com/jpw1991/chebs-necromancy/issues/180) because it can mess up worker minion aiming. Not a big deal - especially if you never use miners/woodcutters. As an alternative, you may consider [Slope Combat Assistance](https://valheim.thunderstore.io/package/Digitalroot/Digitalroots_Slope_Combat_Assistance/) because it only affects the player.
- Soft incompatibility with [Ward is Love](https://github.com/jpw1991/chebs-necromancy/issues/177) because it will identify workers as enemies and yeet them. As an alternative, you may consider using [Better Wards](https://valheim.thunderstore.io/package/Azumatt/BetterWards/).
- Soft incompatibility with [Item Drawers Mod](https://github.com/jpw1991/chebs-necromancy/issues/147) because the Neckro Gatherers try to pick the items out of the drawers.

## Source

You can find the github [here](https://github.com/jpw1991/chebs-necromancy).

## Special Thanks

A special thanks to the people who've helped me along the way:

- Developers
	+ [**Dracbjorn**](https://github.com/Dracbjorn) - Extensive work on the configuration files & parsing (including server-sync); general help & testing.
	+ [**CW-Jesse**](https://github.com/CW-Jesse) - Refinements and improvements on networking code and minion AI; general help & testing.
	+ [**WalterWillis**](https://github.com/WalterWillis) - Improvements to Treasure Pylon & Testing.
	+ [**jneb802**](https://github.com/jneb802) - Finding and fixing a wrongly named sprite.
- Artists
	+ **Ramblez** (aka **[Thorngor](https://www.nexusmods.com/users/21532784)** on the Nexus) - Most of custom textures and icons.
	+ [**piacenti**](https://opengameart.org/users/piacenti) for the [skull prop](https://opengameart.org/content/skull-prop) that I use as part of the phylactery.
- Translations
	+ **S970X** - German language localization.
	+ [**hanawa07**](https://forums.nexusmods.com/index.php?/user/134678658-hanawa07/) - Korean language localization.
	+ **007LEXX** - Russian language localization.
- Other
    - **Hugo the Dwarf** and **Pfhoenix** for advice and help for fixing the frozen minions problem.
	- [**Vapok**](https://github.com/Vapok) and [**jdalfonso4341**](https://github.com/jdalfonso4341) for help with [Adventure Backpacks](https://github.com/Vapok/AdventureBackpacks) integration.
	- **redseiko** for helpful advice on the official Valheim modding Discord.
	- **Ogrebane** for the [spell effect sound](https://opengameart.org/content/teleport-spell).
	- **Jetbrains** for kindly providing me with an [Open Source Development license](https://jb.gg/OpenSourceSupport) for their Rider product which makes development on this project smooth and easy.
	- The unknown author of this awesome [Lovecraftian chart](https://lovecraftzine.files.wordpress.com/2013/04/lovecraft-bestiary.jpg), from which I took inspiration for a lot of the Lovecraft symbol designs for the minion capes.

<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider.svg" alt="Rider logo." width=50% height=50%>

## Changelog


<details>
<summary>2024</summary>

Date | Version | Notes
--- | --- | ---
18/01/2024 | 4.6.1 | Fix problem of custom eye color not being applied to freshly created minions
17/01/2024 | 4.6.0 | Changing minion eye color added; changing minion cape emblems updates dynamically
07/01/2024 | 4.5.0 | PvP with friends as exceptions implemented via console commands.

</details>

<details>
<summary>2023</summary>

Date | Version | Notes
--- | --- | ---
22/12/2023 | 4.4.2 | Permit PvP - if enabled, minions will attack players and creatures that are not their owner.
29/11/2023 | 4.4.1 | Fix issue of configs not syncing reliably
22/11/2023 | 4.4.0 | Fix problem of phylactery not reliably working by overhauling its checking mechanism to be done via RPCs between server and client.
10/10/2023 | 4.3.2 | hildr's request changed Text to TextMeshProUGUI (the new Unity UI text), which broke the wand keyhints. This is now fixed
07/10/2023 | 4.3.1 | add custom two-sided shader that was missing from chebgonaz bundle which caused a pink wand
06/10/2023 | 4.3.0 | update for hildr's request; fix [issue related to bat icon](https://github.com/jpw1991/chebs-necromancy/pull/240)
23/08/2023 | 4.2.0 | update for new valheim patch
02/08/2023 | 4.1.1 | strengthen null checks in Phylactery patch
30/07/2023 | 4.1.0 | add phylactery that consumes dragon eggs when you die and teleports you back to its location
28/07/2023 | 4.0.0 | Workers should behave more realistically with gradual destruction of rocks, trees, etc.
23/07/2023 | 3.7.1 | Fix problem where the minion state wasn't getting localized in the hover text
22/07/2023 | 3.7.0 | Remove configurable Neckro Gatherer container size because containers have not been configurable since 3.3.1 (it resulted in a bug - see issue 100 on GitHub); add Russian translation
21/07/2023 | 3.6.2 | fix bug where onePerPlayer was set to true for character drops, which would result in incorrect resource amounts getting refunded
20/07/2023 | 3.6.1 | Add config option to make draugr quiet; update CVL to 2.1.1
13/07/2023 | 3.6.0 | Try to make workers behave better; update CVL to 2.1.0 to prepare for upcoming changes
21/06/2023 | 3.5.2 | Treasure pylon checks that Piece is not null before processing container
16/06/2023 | 3.5.1 | Option to toggle smoke effects off wraiths (some players getting blinded out here)
12/06/2023 | 3.5.0 | Update for new Valheim version
24/05/2023 | 3.3.2 | Functions created in 3.3.0 moved into Cheb's Valheim Library for Cheb's Mercenaries
24/05/2023 | 3.3.1 | Come up with a solution for issue 100 by removing ability to set custom container sizes.
18/05/2023 | 3.2.3 | Extra safe-guarding against null objects on m_piece and GetInventory when Neckro Gatherer is attempting to find containers. These things shouldn't ever be null as far as I understand - but other mods can do wild things apparently.
16/05/2023 | 3.3.0 | Make Orb behave like modern wands; Permit alternative materials for minion making via piping
12/05/2023 | 3.2.2 | Configurable item cost for Neckro Gatherers
11/05/2023 | 3.2.1 | Unbundle DLL to fix bug of wands not working; ignore collision with carts
02/05/2023 | 3.2.0 | Commandable workers; If a woodcutter is swinging, but missing, the damage gets dealt anyway; remove tooltier stuff for simplicity and streamlining. People can use 3rd party item-alteration mods instead
21/04/2023 | 3.1.0 | Battle Neckro and possible fix to frozen minions
17/04/2023 | 3.0.7 | Cycling minion selection for greater control
13/04/2023 | 3.0.6 | fix Farming Pylon to no longer say Armor stand on hover text; merge ChebsValheimLibrary.dll into ChebsNecromancy.dll for user convenience
11/04/2023 | 3.0.5 | add container whitelist to Neckros & limit their selection to just player made containers
11/04/2023 | 3.0.4 | upgrade ChebsValheimLib to 1.0.1 to fix ToolTier
09/04/2023 | 3.0.1 | Refactor to be compatible with Cheb's Mercenaries; bug fixes
05/04/2023 | 2.5.15 | Miners prioritize copper, silver, and tin over rocks
05/04/2023 | 2.5.14 | Fix bug where bones get consumed even when not enough are in the inventory; fix config descriptions
31/03/2023 | 2.5.13 | Character.m_tamed is also checked when looking for hostiles so that things like tamed animals aren't detected; Configurable teleport durability cost & cooldown
28/03/2023 | 2.5.11 | don't log error if NPC is using the Orb of Beckoning
28/03/2023 | 2.5.10 | improve repair pylon behaviour; upgrade to jotunn 1.11.2; make neckro update more null resistant
27/03/2023 | 2.5.9 | fix a null object exception for structure friendly-fire code; ships ignore minion impact damage; fix problem of duplicate resource drops on death if crate is enabled
25/03/2023 | 2.5.8 | miners pop an entire rock/node in one whack because completely mining the fragments seems impossible
25/03/2023 | 2.5.7 | permit multiple miners to whack same rock; overhaul rock whacking logic for lumberjacks and miners; remove collision on draugr heads
23/03/2023 | 2.5.6 | make miners lerp as they mine so that they have increased odds of being able to connect a blow with a stone; fix null object
23/03/2023 | 2.5.5 | pylons suspend their work while players are sleeping
23/03/2023 | 2.5.4 | Neckros return home before searching for containers
23/03/2023 | 2.5.3 | whitelist for treasure pylon container access
22/03/2023 | 2.5.2 | fix dumb exception
22/03/2023 | 2.5.0 | treasure pylon
21/03/2023 | 2.4.4 | optimize workers by staggering update intervals (50 Neckros/lumberjacks/miners no longer attempt to scan at the same time); add custom cape emblems for armored skeletons; add wolf, lox and troll leather armor varieties
16/03/2023 | 2.3.11 | repair pylon consumes resin more intelligently; repair pylon permits multiple customizable fuel types
15/03/2023 | 2.3.10 | implement maximum time allowed between dropoffs for Neckros
14/03/2023 | 2.3.9 | upgrade jotunn; mess with references; fix logic error
14/03/2023 | 2.3.8 | combine crates to reduce pollution
13/03/2023 | 2.3.7 | forget position of current enemy so they don't start chasing after it after teleporting
13/03/2023 | 2.3.6 | even if disabled still load assets from asset bundle
11/03/2023 | 2.3.5 | guardian wraith shouldn't drop fragments anymore
11/03/2023 | 2.3.4 | fix possible null object
08/03/2023 | 2.3.3 | improve miner/woodcutter
08/03/2023 | 2.3.2 | hitting minions with wand dismisses them; add leeches as summon to orb; neckro hivemind; refactor count code; slight speed increase by not searching parents for ZNetView; fixed performance drop for new Miners and Woodcutters after game has been running for a while
03/03/2023 | 2.2.3 | Hotfix Neckro null object
03/03/2023 | 2.2.2 | Remove unnecessary attack collider on Spirit Pylon Ghosts that was causing unwanted collisions with player; if Neckros don't see any other items to pick up, they can return their inventories before they're full
02/03/2023 | 2.2.1 | Add CW-Jesse's network optimization for Neckros
02/03/2023 | 2.2.0 | Add Repair pylon
02/03/2023 | 2.1.2 | Add delay to Neckros before they pick up an item; make Neckros pick up items one by one
01/03/2023 | 2.1.1 | Add bonus minions for necromancy level if configured to use limits
28/02/2023 | 2.1.0 | Add porcupine skeleton & draugr warriors; add fire, frost, silver, and poison arrow skeletons; only following skeletons respond to the wand's wait command; armor materials consumed in order of best to worst
28/02/2023 | 2.0.8 | Fix a problem where recipes for items and structures wouldn't sync with server
27/02/2023 | 2.0.5 | Neckro only says it's picking up items that it actually successfully picks up, rather than all candidates
26/02/2023 | 2.0.4 | Fix bug in Neckro where the remainder of an item stack could get deleted if not fully deposited
25/02/2023 | 2.0.2 | Fix worker skeletons
25/02/2023 | 2.0.1 | Fix poison skeleton
25/02/2023 | 2.0.0 | Big refactor & reorganization of things
24/02/2023 | 1.9.2 | fix issue about wand not commanding minions to follow reliably if they're roaming; include item layer when matching Pieceables for Farming Pylon; general code refactor/clean; Refactor harmony patches to be more friendly to other mods
23/02/2023 | 1.9.1 | Change some Prefix patches to Postfix for better mod compatibility
22/02/2023 | 1.9.0 | Add Farming Pylon; Add miner skeleton; Fix rare null object in Neckro update function
21/02/2023 | 1.8.8 | Add owner name and status to minion hover text
20/02/2023 | 1.8.7 | configurable bones drop chance; configurable follow distance and run distance; archers now cost arrows
19/02/2023 | 1.8.6 | Support for [Adventure Backpacks](https://github.com/Vapok/AdventureBackpacks) by Vapok.
16/02/2023 | 1.8.4 | Add optional Radeon Friendly switch to the config which disables all effects to permit Radeon users to play without graphical glitches.
15/02/2023 | 1.8.2 | Skeleton woodcutters can be crafted using flint; Refueler pylon optimized and can now also cook
14/02/2023 | 1.8.1 | Skeleton woodcutters added
12/02/2023 | 1.8.0 | Orb of Beckoning working; remove old attack target code from wand
12/02/2023 | 1.7.9 | Fix bug that caused coroutines to occur twice on pylons - resulting in performance impacts and limits bypass.
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

