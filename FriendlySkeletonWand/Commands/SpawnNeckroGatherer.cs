
// console command to summon player's minions.
// attention: only summons THEIR minions

using System;
using System.Collections.Generic;
using Jotunn.Entities;
using UnityEngine;

namespace FriendlySkeletonWand.Commands
{
    public class SpawnNeckroGatherer : ConsoleCommand
    {
        public override string Name => "chebgonaz_prerelease_spawnneckro";

        public override string Help => "Spawns a Neckro Gatherer (for debug/testing, will replace with proper method for creation soon)";

        public override void Run(string[] args)
        {
            Player player = Player.m_localPlayer;
            GameObject neckroPrefab = ZNetScene.instance.GetPrefab("ChebGonaz_NecroNeck");
            GameObject.Instantiate(neckroPrefab, 
                player.transform.position + player.transform.forward * 2f + Vector3.up, 
                Quaternion.identity);
        }

        public override List<string> CommandOptionList()
        {
            return ZNetScene.instance?.GetPrefabNames();
        }
    }
}
