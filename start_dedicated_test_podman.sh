#!/bin/bash
VALHEIMSERVERNAME=valheim-server
#PLUGINS_PATH=~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins
PLUGINS_PATH=/home/$USER/.config/r2modmanPlus-local/Valheim/profiles/cheb-development/BepInEx/plugins

podman pull docker.io/lloesche/valheim-server

podman container exists $VALHEIMSERVERNAME

if [ $? -eq 0 ]; then
    podman kill $VALHEIMSERVERNAME
    podman wait $VALHEIMSERVERNAME
    echo "Starting existing container $VALHEIMSERVERNAME"
    podman start $VALHEIMSERVERNAME
else
    # create new container
    echo "Creating new container $VALHEIMSERVERNAME"
    podman run -d --name valheim-server --cap-add=sys_nice --stop-timeout 120 -p 2456-2457:2456-2457/udp -v $HOME/valheim-server/config:/config -v $HOME/valheim-server/data:/opt/valheim -e SERVER_NAME="My Server" -e WORLD_NAME="podmantest" -e SERVER_PASS="secret" -e BEPINEX="true" lloesche/valheim-server
    #podman run -d --name valheim-server --cap-add=sys_nice --stop-timeout 120 -p 2456-2457:2456-2457/udp -e SERVER_NAME="My Server" -e WORLD_NAME="podmantest" -e SERVER_PASS="secret" -e BEPINEX="true" lloesche/valheim-server
fi

CONTAINER=$(podman ps --filter "name=$VALHEIMSERVERNAME" --format "{{.ID}}")

echo "Started $CONTAINER"

echo "Waiting for container to start..."
podman wait --condition=running $VALHEIMSERVERNAME

echo "Copying mods into container..."
podman exec -d $CONTAINER /bin/bash -c 'rm -rf /config/bepinex/plugins'
podman cp $PLUGINS_PATH $CONTAINER:/config/bepinex/

podman restart $CONTAINER
