using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.Armor.Player
{
    internal class NecromancerCape
    {
        public enum Emblem
        {
            [InternalName("ChebGonaz_NecromancerCape")]
            Blank,

            [InternalName("ChebGonaz_NecromancerCapeAbhoth")]
            Abhoth,

            [InternalName("ChebGonaz_NecromancerCapeAzathoth")]
            Azathoth,

            [InternalName("ChebGonaz_NecromancerCapeColor")]
            ColorOutOfSpace,

            [InternalName("ChebGonaz_NecromancerCapeCompass")]
            Compass,

            [InternalName("ChebGonaz_NecromancerCapeCthulhu")]
            Cthulhu,

            [InternalName("ChebGonaz_NecromancerCapeDagon")]
            Dagon,

            [InternalName("ChebGonaz_NecromancerCapeElderthing")]
            Elderthing,

            [InternalName("ChebGonaz_NecromancerCapeElk")]
            Elk,

            [InternalName("ChebGonaz_NecromancerCapeHastur")]
            Hastur,

            [InternalName("ChebGonaz_NecromancerCapeHypnos")]
            Hypnos,

            [InternalName("ChebGonaz_NecromancerCapeMiGo")]
            MiGo,

            [InternalName("ChebGonaz_NecromancerCapeNight")]
            Night,

            [InternalName("ChebGonaz_NecromancerCapeNodens")]
            Nodens,

            [InternalName("ChebGonaz_NecromancerCapeNyar")]
            Nyarlathotep,

            [InternalName("ChebGonaz_NecromancerCapePower")]
            Power,

            [InternalName("ChebGonaz_NecromancerCapeShub")]
            ShubNiggurath,

            [InternalName("ChebGonaz_NecromancerCapeThorn")]
            Thorn,

            [InternalName("ChebGonaz_NecromancerCapeUbo")]
            Ubo,

            [InternalName("ChebGonaz_NecromancerCapeWarrior")]
            Warrior,

            [InternalName("ChebGonaz_NecromancerCapeWealth")]
            Wealth,

            [InternalName("ChebGonaz_NecromancerCapeYith")]
            Yith,

            [InternalName("ChebGonaz_NecromancerCapeYog")]
            YogSothoth,

            [InternalName("ChebGonaz_NecromancerCapeZhar")]
            Zhar,
        }

        private static int PlayerEmblemZdoKeyHash => "ChebGonazEmblemSetting".GetHashCode();
        
        public static ConfigEntry<Emblem> EmblemConfig;
        public static Dictionary<string, Material> Emblems = new();

        public void CreateConfigs(BaseUnityPlugin plugin)
        {
            EmblemConfig = plugin.Config.Bind($"{GetType().Name} (Client)", "Emblem", Emblem.Blank,
                new ConfigDescription("The symbol on the cape of your armored minions."));
            EmblemConfig.SettingChanged += (sender, args) =>
            {
                Logger.LogInfo($"Emblem changed to {EmblemConfig.Value} in config");
                SetEmblem(EmblemConfig.Value);
            };
        }

        public static void LoadEmblems(AssetBundle bundle)
        {
            foreach (Emblem emblem in Enum.GetValues(typeof(Emblem)))
            {
                var name = InternalName.GetName(emblem);
                Emblems[name] = bundle.LoadAsset<Material>(name + ".mat");
            }
        }

        public static void SetEmblem(Emblem emblem)
        {
            // update minion capes with new emblem
            Logger.LogInfo($"Emblem changed to {emblem}, updating minion materials...");
            var player = global::Player.m_localPlayer;
            if (player == null)
            {
                Logger.LogInfo("Failed to update minion capes: m_localPlayer is null. This is not an " +
                               "error unless you're in-game right now & just means that cape emblems " +
                               "couldn't be updated on existing minions at this moment in time.");
                return;
            }
            
            player.m_nview.GetZDO().Set(PlayerEmblemZdoKeyHash, (int)emblem);

            var matName = InternalName.GetName(emblem);
            var minionsBelongingToPlayer = ZDOMan.instance.m_objectsByID
                .Values
                .ToList()
                .FindAll(zdo => SkeletonMinion.IsSkeletonHash(zdo.GetPrefab()))
                .Where(zdo =>
                    zdo.GetString(ChebGonazMinion.MinionOwnershipZdoKey) ==
                    player.GetPlayerName())
                .ToList();
            Logger.LogInfo($"Found {minionsBelongingToPlayer.Count} to update...");
            foreach (var zdo in minionsBelongingToPlayer)
            {
                zdo.Set(UndeadMinion.MinionEmblemZdoKey, matName);
            }

            // now that ZDOs have been set, update loaded minions
            var allCharacters = Character.GetAllCharacters();
            foreach (var character in allCharacters)
            {
                if (character.IsDead())
                {
                    continue;
                }

                var minion = character.GetComponent<SkeletonMinion>();
                if (minion == null || !minion.BelongsToPlayer(player.GetPlayerName())) continue;
                if (!minion.TryGetComponent(out Humanoid humanoid))
                {
                    Logger.LogInfo("Unable to get humanoid");
                    return;
                }

                minion.LoadEmblemMaterial(humanoid);
            }
        }
    }
}