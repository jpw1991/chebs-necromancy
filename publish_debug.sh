#!/bin/bash

DEBUGDIR=ChebsNecromancy/bin/Debug/net48
DLL=$DEBUGDIR/ChebsNecromancy.dll
LIB=$DEBUGDIR/ChebsValheimLibrary.dll
BUN=ChebsNecromancyUnity/Assets/AssetBundles/chebgonaz
#PLUGINS=/home/joshua/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins
PLUGINS=/home/$USER/.config/r2modmanPlus-local/Valheim/profiles/cheb-development/BepInEx/plugins/ChebGonaz-ChebsNecromancy

# Check that source files exist and are readable
if [ ! -f "$DLL" ]; then
    echo "Error: $DLL does not exist or is not readable."
    exit 1
fi

if [ ! -f "$LIB" ]; then
    echo "Error: $LIB does not exist or is not readable."
    exit 1
fi

if [ ! -f "$BUN" ]; then
    echo "Error: $BUN does not exist or is not readable."
    exit 1
fi

# Check that target directory exists and is writable
if [ ! -d "$PLUGINS" ]; then
    echo "Error: $PLUGINS directory does not exist."
    exit 1
fi

if [ ! -w "$PLUGINS" ]; then
    echo "Error: $PLUGINS directory is not writable."
    exit 1
fi

cp -f "$DLL" "$PLUGINS" || { echo "Error: Failed to copy $DLL"; exit 1; }
cp -f "$LIB" "$PLUGINS" || { echo "Error: Failed to copy $LIB"; exit 1; }
cp -f "$BUN" "$PLUGINS" || { echo "Error: Failed to copy $BUN"; exit 1; }
cp -f "$BUN.manifest" "$PLUGINS" || { echo "Error: Failed to copy $BUN.manifest"; exit 1; }

# merging causes problems with the mods when installed together, so I'm disabling this
#
# create dir if not existing
#if [ ! -d "$DEBUGDIR/merged" ]; then
#    mkdir $DEBUGDIR/merged
#fi
#
#cp -f $BEPINEX $DEBUGDIR
#cd $DEBUGDIR
#
#mono ../../../packages/ILRepack.2.0.18/tools/ILRepack.exe /out:merged/ChebsNecromancy.dll $(basename "$LIB") $(basename "$DLL")
#
#if [ $? != 0 ]; then
#    echo "Merging failed"
#    exit 1
#fi
#
#cp -f "merged/ChebsNecromancy.dll" "$PLUGINS" || { echo "Error: Failed to copy merged dll"; exit 1; }

