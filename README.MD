# Friendly Skeleton Wand

This mod adds a craftable wand which can be used to create skeletons with. These skeletons are supposed to guard your base and hang around the general vicinity of wherever you create them.

## Requirements

- Valheim Mistlands
- BepInEx
- Jotunn

## Installation (manual)

- Drag the `FriendlySkeletonWand` folder from inside the archive to your Bepinex plugins folder in the Valheim directory.

## Features

- Craftable Wand at the workbench called **Friendly Skeleton Wand**.
- Pressing **B** while the Wand is equipped will create a friendly skeleton guard with **Bone Fragments** from your inventory.
- Skeleton quality increases with **Necromancy Skill Level**:
	+ 0 to 34: Level 1 skeletons.
	+ 35 to 69: Level 2 skeletons.
	+ 70+: Level 3 skeleotns.
- Skeletons created are either an archer or warrior. This is random.
- Tweakable settings by pressing **F1** to edit the config.

### Config

~~**Attention:** To edit the config as described, the [Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) is required. This is a separate mod. Please download and install it.~~

~~Press **F1** to open the mod's configuration panel where you can modify the following:~~

The Configuration Manager mod doesn't seem to work with my mod. I don't know why. Anyway, you can edit the settings manually in `Valheim/BepInEx/config/com.chebgonaz.FriendlySkeletonWand.cfg`:

Property | Value | Notes
--- | --- | ---
`BoneFragmentsRequired` | `integer` | How many **Bone Fragments** are consumed from your inventory when creating a skeleton (default `3`, set to `0` for free skeletons).
`NecromancyLevelIncrease` | `float` | How much the **Necromancy Skill**'s level increases when you create a skeleton (default `0.25`).
`SkeletonsPerSummon` | `integer` | How many skeletons are created each time you create a skeleton (default `1`, set to larger numbers  for unbridled madness [**Warning: Your game might crash - this really is unlimited!**]).

## Changelog

Date | Version | Notes
--- | --- | ---
25/11/2022 | 1.0.0 | Release

## Known issues

- None

## Source

You can find the github [here](https://github.com/jpw1991/Friendly-Skeleton-Wand).
