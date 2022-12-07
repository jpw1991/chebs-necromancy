#!/bin/bash

display_usage()
{
    ua=(
	target_path
	target_assembly
	valheim_path
	project_path
	deploy_path
	)
    echo "Usage: $0 ${ua[*]}"
    uv=(
        "FriendlySkeletonWand/bin/Release"
	"FriendlySkeletonWand.dll"
	"/home/$USER/.local/share/Steam/steamapps/common/Valheim"
	"."
	"/home/$USER/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins"
       )
    echo "Example values:"
    ua_len=${#ua[@]}
    for (( i=0; i<${ua_len}; i++ )); do
      echo "    ${ua[$i]} = ${uv[$i]}"
    done
}

# show help message if specified or args <= 5
if [[ ( $@ == "--help") ||  $@ == "-h" || $# -le 4 ]]
then
	display_usage
	exit 0
fi

TARGETPATH=$1
TARGETASSEMBLY=$2
VALHEIMPATH=$3
PROJECTPATH=$4
DEPLOYPATH=$5

# verify each argument
if [ ! -d "$TARGETPATH" ]; then
  echo "$TARGETPATH does not exist."
  exit 1
fi

#if [ ! -f "$TARGETASSEMBLY" ]; then
#  echo "$TARGETASSEMBLY does not exist."
#  exit 1
#fi

if [ ! -d "$VALHEIMPATH" ]; then
  echo "$VALHEIMPATH does not exist."
  exit 1
fi

if [ ! -d "$PROJECTPATH" ]; then
  echo "$PROJECTPATH does not exist."
  exit 1
fi

# plugin name without .dll
name=$( basename "$TARGETASSEMBLY" .dll )

# target
TARGET=$( basename "$TARGETPATH" )

# handle each target differently
if [ $TARGET == "Release" ]; then
  echo "Packaging for Thunderstore..."
  packagePath="$PROJECTPATH/Package"
  if [ -d "$packagePath/plugins" ]; then
    rm -rf "$packagePath/plugins"
  fi
  mkdir "$packagePath/plugins"
  mkdir "$packagePath/$name"
  cp "$TARGETPATH/$TARGETASSEMBLY" "$packagePath/plugins/$name"
  cp "README.md" "$packagePath"
  cp -r "$PROJECTPATH/Assets" "$packagePath/plugins/$name"
  zip -r "$TARGETPATH/$(TARGETASSEMBLY).zip" "$packagePath"
fi

if [ $TARGET == "Debug" ]; then
  if [ $DEPLOYPATH == "" ]; then
    $DEPLOYPATH="$VALHEIMPATH/BepInEx/plugins/"
  fi
  plug="$DEPLOYPATH/$name"
  # create plug and copy files there
  if [ ! -d "$plug" ]; then
	mkdir $plug
  fi
  cp -f "$TARGETPATH/$(name).dll" $plug
  cp -f "$TARGETPATH/$(name).pdb" $plug
  cp -f "$TARGETPATH/$(name).dll.mdb" $plug
  # also copy assets over if it exists
  if [ -d "$PROJECTPATH/Assets" ]; then
    cp -rf "$PROJECTPATH/Assets" "$TARGETPATH/Assets"
  fi
fi

echo "Finished"
