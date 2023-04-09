using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using UnityEngine;

namespace ChebsNecromancy.Items
{
    internal class NecromancerCape
    {
        public enum Emblem
        {
            [InternalName("ChebGonaz_NecromancerCape")] Blank,
            [InternalName("ChebGonaz_NecromancerCapeAbhoth")] Abhoth,
            [InternalName("ChebGonaz_NecromancerCapeAzathoth")] Azathoth,
            [InternalName("ChebGonaz_NecromancerCapeColor")] ColorOutOfSpace,
            [InternalName("ChebGonaz_NecromancerCapeCompass")] Compass,
            [InternalName("ChebGonaz_NecromancerCapeCthulhu")] Cthulhu,
            [InternalName("ChebGonaz_NecromancerCapeDagon")] Dagon,
            [InternalName("ChebGonaz_NecromancerCapeElderthing")] Elderthing,
            [InternalName("ChebGonaz_NecromancerCapeElk")] Elk,
            [InternalName("ChebGonaz_NecromancerCapeHastur")] Hastur,
            [InternalName("ChebGonaz_NecromancerCapeHypnos")] Hypnos,
            [InternalName("ChebGonaz_NecromancerCapeMiGo")] MiGo,
            [InternalName("ChebGonaz_NecromancerCapeNight")] Night,
            [InternalName("ChebGonaz_NecromancerCapeNodens")] Nodens,
            [InternalName("ChebGonaz_NecromancerCapeNyar")] Nyarlathotep,
            [InternalName("ChebGonaz_NecromancerCapePower")] Power,
            [InternalName("ChebGonaz_NecromancerCapeShub")] ShubNiggurath,
            [InternalName("ChebGonaz_NecromancerCapeThorn")] Thorn,
            [InternalName("ChebGonaz_NecromancerCapeUbo")] Ubo,
            [InternalName("ChebGonaz_NecromancerCapeWarrior")] Warrior,
            [InternalName("ChebGonaz_NecromancerCapeWealth")] Wealth,
            [InternalName("ChebGonaz_NecromancerCapeYith")] Yith,
            [InternalName("ChebGonaz_NecromancerCapeYog")] YogSothoth,
            [InternalName("ChebGonaz_NecromancerCapeZhar")] Zhar,
        }

        
        public static ConfigEntry<Emblem> EmblemConfig;
        public static Dictionary<string, Material> Emblems = new();

        public void CreateConfigs(BaseUnityPlugin plugin)
        {
            EmblemConfig = plugin.Config.Bind($"{GetType().Name} (Client)", "Emblem", Emblem.Blank, 
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