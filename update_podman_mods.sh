#!/bin/bash
podman exec -d 00111176c93c /bin/bash -c 'rm -rf /config/bepinex/plugins'
podman cp /home/$USER/.config/r2modmanPlus-local/Valheim/profiles/cheb-development/BepInEx/plugins 00111176c93c:/config/bepinex

