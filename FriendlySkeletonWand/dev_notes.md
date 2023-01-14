# Dev Notes

## Run in container to test dedicated

```sh
# pull popular valheim image
podman pull docker.io/lloesche/valheim-server

# run container
podman run -d --name valheim-server --cap-add=sys_nice --stop-timeout 120 -p 2456-2457:2456-2457/udp -v $HOME/valheim-server/config:/config -v $HOME/valheim-server/data:/opt/valheim -e SERVER_NAME="My Server" -e WORLD_NAME="podmantest" -e SERVER_PASS="secret" -e BEPINEX="true" lloesche/valheim-server

# copy mods in
cd ~/.local/share/Steam/steamapps/common/Valheim/BepInEx/plugins;
for f in *; do
  podman cp --overwrite $f a2dc88de3800:/config/bepinex/plugins
done

# restart container
podman restart a2dc88de3800
```

Then start valheim and conntect to 127.0.0.1