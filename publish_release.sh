#!/bin/bash

RELEASEDIR=ChebsNecromancy/bin/Release/net48
DLL=$RELEASEDIR/ChebsNecromancy.dll
LIB=$RELEASEDIR/ChebsValheimLibrary.dll
BUN=chebs-necromancy-unity/Assets/AssetBundles/chebgonaz
PLUGINS=ChebsNecromancy/Package/plugins
README=README.md
TRANSLATIONS=Translations

VERSION=$1

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

if [ ! -f "$README" ]; then
    echo "Error: $README does not exist or is not readable."
    exit 1
fi


cp -f "$DLL" "$PLUGINS" || { echo "Error: Failed to copy $DLL"; exit 1; }
cp -f "$LIB" "$PLUGINS" || { echo "Error: Failed to copy $LIB"; exit 1; }
cp -f "$BUN" "$PLUGINS" || { echo "Error: Failed to copy $BUN"; exit 1; }
cp -f "$README" "$PLUGINS/../README.md" || { echo "Error: Failed to copy $README"; exit 1; }
cp -rf "$TRANSLATIONS" "$PLUGINS/" || { echo "Error: Failed to copy Translations"; exit 1; }

# merging causes problems with the mods when installed together, so I'm disabling this
#
# create dir if not existing
#if [ ! -d "$RELEASEDIR/merged" ]; then
#    mkdir $RELEASEDIR/merged
#fi
#
#cp -f $BEPINEX $RELEASEDIR
#cd $RELEASEDIR
#
#mono ../../../packages/ILRepack.2.0.18/tools/ILRepack.exe /out:merged/ChebsNecromancy.dll $(basename "$LIB") $(basename "$DLL")
#
#if [ $? != 0 ]; then
#    echo "Merging failed"
#    exit 1
#fi
#
#cd ../../../
#cp -f $RELEASEDIR/merged/ChebsNecromancy.dll $PLUGINS

ZIPDESTINATION="../bin/Release/ChebsNecromancy.$VERSION.zip"

cd "$PLUGINS/.."
if [ ! -z "$VERSION" ]; then
    VERSION=".$VERSION"
fi
zip -r "$ZIPDESTINATION" .
