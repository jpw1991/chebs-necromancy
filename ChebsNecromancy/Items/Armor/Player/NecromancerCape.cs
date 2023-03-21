using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ChebsNecromancy.Items
{
    internal class NecromancerCape
    {
        public enum Emblem
        {
            [InternalName("ChebGonaz_NecromancerCape")] Blank,
            [InternalName("ChebGonaz_NecromancerCapeAbhoth")] Abhoth,
            [InternalName("ChebGonaz_NecromancerCapeWarrior")] Warrior,
        }
        
        public static ConfigEntry<Emblem> EmblemConfig;
        public static Dictionary<string, Material> Emblems = new();

        public void CreateConfigs(BaseUnityPlugin plugin)
        {
            EmblemConfig = plugin.Config.Bind("NecromancerCape (Client)", "Emblem", Emblem.Blank, 
                new ConfigDescription("The symbol on the cape of your armored minions."));
        }

        public static void LoadEmblems(AssetBundle bundle)
        {
            foreach (Emblem emblem in Enum.GetValues(typeof(Emblem)))
            {
                var name = InternalName.GetName(emblem);
                Emblems[name] = bundle.LoadAsset<Material>(name + ".mat");
            }
        }
    }
}