// ChebsNecromancy
// 
// File:    ChebsNecromancy.cs
// Project: ChebsNecromancy

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Commands;
using ChebsNecromancy.CustomPrefabs;
using ChebsNecromancy.Items.Armor.Player;
using ChebsNecromancy.Items.Wands;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Draugr;
using ChebsNecromancy.Minions.Skeletons;
using ChebsNecromancy.Structures;
using ChebsValheimLibrary;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.PvP;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Paths = BepInEx.Paths;

namespace ChebsNecromancy
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.chebgonaz.ChebsNecromancy";
        public const string PluginName = "ChebsNecromancy";
        public const string PluginVersion = "4.5.3";
        private const string ConfigFileName = PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        public readonly System.Version ChebsValheimLibraryVersion = new("2.5.3");

        private readonly Harmony harmony = new(PluginGuid);
        
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private readonly List<Wand> wands = new()
        {
            new SkeletonWand(),
            new DraugrWand(),
            new OrbOfBeckoning()
        };

        public const string NecromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private readonly SpectralShroud spectralShroudItem = new();
        private readonly NecromancerHood necromancersHoodItem = new();
        private readonly NecromancerCape necromancerCapeItem = new();

        private float inputDelay = 0;

        public static SE_Stats SetEffectNecromancyArmor, SetEffectNecromancyArmor2;

        // Global Config Acceptable Values
        public AcceptableValueList<bool> BoolValue = new(true, false);
        public AcceptableValueRange<float> FloatQuantityValue = new(1f, 1000f);
        public AcceptableValueRange<int> IntQuantityValue = new(1, 1000);
        
        public static ConfigEntry<bool> HeavyLogging;
        
        public static ConfigEntry<bool> PvPAllowed;

        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly;
        // so that players may disable smoke on wraiths, if they wish (feature request)
        public static ConfigEntry<bool> NoWraithSmoke;

        public static ConfigEntry<int> BoneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> BoneFragmentsDroppedAmountMax;
        public static ConfigEntry<float> BoneFragmentsDroppedChance;

        public static ConfigEntry<int> ArmorLeatherScrapsRequiredConfig;
        public static ConfigEntry<int> ArmorBronzeRequiredConfig;
        public static ConfigEntry<int> ArmorIronRequiredConfig;
        public static ConfigEntry<int> ArmorBlackIronRequiredConfig;
        public static ConfigEntry<int> NeedlesRequiredConfig;

        public static ConfigEntry<bool> DurabilityDamage;
        public static ConfigEntry<float> DurabilityDamageWarrior;
        public static ConfigEntry<float> DurabilityDamageMage;
        public static ConfigEntry<float> DurabilityDamageArcher;
        public static ConfigEntry<float> DurabilityDamagePoison;
        public static ConfigEntry<float> DurabilityDamageLeather;
        public static ConfigEntry<float> DurabilityDamageBronze;
        public static ConfigEntry<float> DurabilityDamageIron;
        public static ConfigEntry<float> DurabilityDamageBlackIron;

        private void Awake()
        {
            if (!Base.VersionCheck(ChebsValheimLibraryVersion, out var message))
            {
                Jotunn.Logger.LogWarning(message);
            }

            CreateConfigValues();

            Phylactery.ConfigureRPC();
            PvPManager.ConfigureRPC();

            LoadChebGonazAssetBundle();

            harmony.PatchAll();

            CommandManager.Instance.AddConsoleCommand(new KillAllMinions());
            CommandManager.Instance.AddConsoleCommand(new SummonAllMinions());
            CommandManager.Instance.AddConsoleCommand(new KillAllNeckros());
            CommandManager.Instance.AddConsoleCommand(new SetMinionOwnership());
            CommandManager.Instance.AddConsoleCommand(new SetNeckroHome());
            CommandManager.Instance.AddConsoleCommand(new TeleportNeckros());

            var pvpCommands = new List<ConsoleCommand>()
                { new PvPAddFriend(), new PvPRemoveFriend(), new PvPListFriends() };
            foreach (var pvpCommand in pvpCommands)
            {
                if (!CommandManager.Instance.CustomCommands
                        .ToList().Exists(c => c.Name == pvpCommand.Name))
                    CommandManager.Instance.AddConsoleCommand(pvpCommand);
            }

            StartCoroutine(Phylactery.PhylacteriesCheck());

            SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
            {
                Logger.LogInfo(!attr.InitialSynchronization
                    ? "Syncing configuration changes from server..."
                    : "Syncing initial configuration...");
                UpdateAllRecipes();
                StartCoroutine(RequestPvPDict());

            };

            StartCoroutine(WatchConfigFile());
        }

        private IEnumerator RequestPvPDict()
        {
            yield return new WaitUntil(() => ZNet.instance != null && Player.m_localPlayer != null);
            PvPManager.InitialFriendsListRequest();
        }
        
        #region ConfigUpdate
        private byte[] GetFileHash(string fileName)
        {
            var sha1 = HashAlgorithm.Create();
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            return sha1.ComputeHash(stream);
        }

        private IEnumerator WatchConfigFile()
        {
            var lastHash = GetFileHash(ConfigFileFullPath);
            while (true)
            {
                yield return new WaitForSeconds(5);
                var hash = GetFileHash(ConfigFileFullPath);
                if (!hash.SequenceEqual(lastHash))
                {
                    lastHash = hash;
                    ReadConfigValues();
                }
            }
        }
        
        private void ReadConfigValues()
        {
            try
            {
                var adminOrLocal = ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance();
                Logger.LogInfo($"Read updated config values (admin/local={adminOrLocal})");
                if (adminOrLocal) Config.Reload();
                UpdateAllRecipes();
            }
            catch (Exception exc)
            {
                Logger.LogError($"There was an issue loading your {ConfigFileName}: {exc}");
            }
        }
        #endregion

        private void UpdateAllRecipes()
        {
            wands.ForEach(wand => wand.UpdateRecipe());
            necromancersHoodItem.UpdateRecipe();
            spectralShroudItem.UpdateRecipe();

            SpiritPylon.UpdateRecipe();
            RefuelerPylon.UpdateRecipe();
            NeckroGathererPylon.UpdateRecipe();
            BatBeacon.UpdateRecipe();
            FarmingPylon.UpdateRecipe();
            RepairPylon.UpdateRecipe();
            TreasurePylon.UpdateRecipe();
            Phylactery.UpdateRecipe();
        }

        public ConfigEntry<T> ModConfig<T>(
            string group,
            string name,
            T default_value,
            string description = "",
            AcceptableValueBase acceptableValues = null,
            bool serverSync = false,
            params object[] tags)
        {
            // Create extended description with list of valid values and server sync
            ConfigDescription extendedDescription = new(
                description + (serverSync
                    ? " [Synced with Server]"
                    : " [Not Synced with Server]"),
                acceptableValues,
                new ConfigurationManagerAttributes { IsAdminOnly = serverSync },
                tags);

            var configEntry = Config.Bind(group, name, default_value, extendedDescription);

            return configEntry;
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;
            
            HeavyLogging = Config.Bind("General (Client)", "HeavyLogging",
                false, new ConfigDescription("Switch on to fill the logs with excessive " +
                                             "logging to assist with debugging."));
            
            PvPAllowed = Config.Bind("General (Server Synced)", "PvPAllowed",
                false, new ConfigDescription("Whether minions will target and attack other players and their minions.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            RadeonFriendly = Config.Bind("General (Client)", "RadeonFriendly",
                false, new ConfigDescription("ONLY set this to true if you have graphical issues with " +
                                             "the mod. It will disable all particle effects for the mod's prefabs " +
                                             "which seem to give users with Radeon cards trouble for unknown " +
                                             "reasons. If you have problems with lag it might also help to switch" +
                                             "this setting on."));
            
            NoWraithSmoke = Config.Bind("General (Client)", "NoWraithSmoke",
                false, new ConfigDescription("Set this to true if you want to disable smoke on the wraith."));

            #region BoneFragments

            BoneFragmentsDroppedAmountMin = Config.Bind("General (Server Synced)", "BoneFragmentsDroppedAmountMin",
                1, new ConfigDescription("The minimum amount of bones dropped by creatures.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            BoneFragmentsDroppedAmountMax = Config.Bind("General (Server Synced)", "BoneFragmentsDroppedAmountMax",
                3, new ConfigDescription("The maximum amount of bones dropped by creatures.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            BoneFragmentsDroppedChance = Config.Bind("General (Server Synced)", "BoneFragmentsDroppedChance",
                .25f, new ConfigDescription("The chance of bones dropped by creatures (0 = 0%, 1 = 100%).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            #endregion

            #region MinionUpgrades

            ArmorLeatherScrapsRequiredConfig = Config.Bind("General (Server Synced)", "ArmorLeatherScrapsRequired",
                2, new ConfigDescription("The amount of LeatherScraps required to craft a minion in leather armor.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBronzeRequiredConfig = Config.Bind("General (Server Synced)", "ArmorBronzeRequired",
                1, new ConfigDescription("The amount of Bronze required to craft a minion in bronze armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorIronRequiredConfig = Config.Bind("General (Server Synced)", "ArmoredIronRequired",
                1, new ConfigDescription("The amount of Iron required to craft a minion in iron armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBlackIronRequiredConfig = Config.Bind("General (Server Synced)", "ArmorBlackIronRequired",
                1, new ConfigDescription("The amount of Black Metal required to craft a minion in black iron armor.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            NeedlesRequiredConfig = Config.Bind("General (Server Synced)", "NeedlesRequired",
                5, new ConfigDescription("The amount of needles required to craft a needle warrior.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            #endregion

            #region Durability

            DurabilityDamage = Config.Bind("General (Server Synced)", "DurabilityDamage",
                true, new ConfigDescription("Whether using a wand damages its durability.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageWarrior = Config.Bind("General (Server Synced)", "DurabilityDamageWarrior",
                1f, new ConfigDescription("How much creating a warrior damages a wand.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageArcher = Config.Bind("General (Server Synced)", "DurabilityDamageArcher",
                3f, new ConfigDescription("How much creating an archer damages a wand.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageMage = Config.Bind("General (Server Synced)", "DurabilityDamageMage",
                5f, new ConfigDescription("How much creating a mage damages a wand.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamagePoison = Config.Bind("General (Server Synced)", "DurabilityDamagePoison",
                5f, new ConfigDescription("How much creating a poison skeleton damages a wand.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageLeather = Config.Bind("General (Server Synced)", "DurabilityDamageLeather",
                1f, new ConfigDescription(
                    "How much armoring the minion in leather damages the wand (value is added on top of damage from minion type).",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageBronze = Config.Bind("General (Server Synced)", "DurabilityDamageBronze",
                1f, new ConfigDescription(
                    "How much armoring the minion in bronze damages the wand (value is added on top of damage from minion type)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageIron = Config.Bind("General (Server Synced)", "DurabilityDamageIron",
                1f, new ConfigDescription(
                    "How much armoring the minion in iron damages the wand (value is added on top of damage from minion type)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageBlackIron = Config.Bind("General (Server Synced)", "DurabilityDamageBlackIron",
                1f, new ConfigDescription(
                    "How much armoring the minion in black iron damages the wand (value is added on top of damage from minion type)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            #endregion

            UndeadMinion.CreateConfigs(this);

            SkeletonMinion.CreateConfigs(this);
            PoisonSkeletonMinion.CreateConfigs(this);
            SkeletonArcherFireMinion.CreateConfigs(this);
            SkeletonArcherFrostMinion.CreateConfigs(this);
            SkeletonArcherPoisonMinion.CreateConfigs(this);
            SkeletonArcherSilverMinion.CreateConfigs(this);
            SkeletonArcherTier1Minion.CreateConfigs(this);
            SkeletonArcherTier2Minion.CreateConfigs(this);
            SkeletonArcherTier3Minion.CreateConfigs(this);
            SkeletonMageMinion.CreateConfigs(this);
            SkeletonWarriorMinion.CreateConfigs(this);
            SkeletonWoodcutterMinion.CreateConfigs(this);
            SkeletonMinerMinion.CreateConfigs(this);

            DraugrMinion.CreateConfigs(this);
            DraugrWarriorMinion.CreateConfigs(this);
            DraugrArcherFireMinion.CreateConfigs(this);
            DraugrArcherFrostMinion.CreateConfigs(this);
            DraugrArcherPoisonMinion.CreateConfigs(this);
            DraugrArcherSilverMinion.CreateConfigs(this);
            DraugrArcherTier1Minion.CreateConfigs(this);
            DraugrArcherTier2Minion.CreateConfigs(this);
            DraugrArcherTier3Minion.CreateConfigs(this);

            GuardianWraithMinion.CreateConfigs(this);

            LeechMinion.CreateConfigs(this);
            BattleNeckroMinion.CreateConfigs(this);

            wands.ForEach(w => w.CreateConfigs(this));

            spectralShroudItem.CreateConfigs(this);
            necromancersHoodItem.CreateConfigs(this);
            necromancerCapeItem.CreateConfigs(this);

            SpiritPylon.CreateConfigs(this);
            RefuelerPylon.CreateConfigs(this);
            NeckroGathererPylon.CreateConfigs(this);
            BatBeacon.CreateConfigs(this);
            BatLantern.CreateConfigs(this);
            FarmingPylon.CreateConfigs(this);
            RepairPylon.CreateConfigs(this);
            TreasurePylon.CreateConfigs(this);
            Phylactery.CreateConfigs(this);

            NeckroGathererMinion.CreateConfigs(this);
        }
        

        private void LoadChebGonazAssetBundle()
        {
            // order is important (I think): items, creatures, structures
            var assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "chebgonaz");
            var chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                SE_Stats LoadSetEffectFromBundle(string setEffectName, AssetBundle bundle)
                {
                    //Jotunn.Logger.LogInfo($"Loading {setEffectName}...");
                    var seStat = bundle.LoadAsset<SE_Stats>(setEffectName);
                    if (seStat == null)
                    {
                        Jotunn.Logger.LogError($"LoadSetEffectFromBundle: {setEffectName} is null!");
                    }

                    return seStat;
                }

                #region SetEffects

                SetEffectNecromancyArmor = LoadSetEffectFromBundle("SetEffect_NecromancyArmor", chebgonazAssetBundle);
                SetEffectNecromancyArmor2 = LoadSetEffectFromBundle("SetEffect_NecromancyArmor2", chebgonazAssetBundle);

                #endregion

                #region Items

                var spectralShroudPrefab = Base.LoadPrefabFromBundle(spectralShroudItem.PrefabName,
                    chebgonazAssetBundle, RadeonFriendly.Value);
                ItemManager.Instance.AddItem(spectralShroudItem.GetCustomItemFromPrefab(spectralShroudPrefab));

                var necromancersHoodPrefab = Base.LoadPrefabFromBundle(necromancersHoodItem.PrefabName,
                    chebgonazAssetBundle, RadeonFriendly.Value);
                ItemManager.Instance.AddItem(necromancersHoodItem.GetCustomItemFromPrefab(necromancersHoodPrefab));

                NecromancerCape.LoadEmblems(chebgonazAssetBundle);

                // Orb of Beckoning
                var orbOfBeckoningProjectilePrefab =
                    Base.LoadPrefabFromBundle(OrbOfBeckoning.ProjectilePrefabName, chebgonazAssetBundle,
                        RadeonFriendly.Value);
                orbOfBeckoningProjectilePrefab.AddComponent<OrbOfBeckoningProjectile>();

                // minion items
                Base.LoadMinionItems(chebgonazAssetBundle, RadeonFriendly.Value);

                wands.ForEach(wand =>
                {
                    // we do the keyhints later after vanilla items are available
                    // so we can override what's in the prefab
                    var wandPrefab =
                        Base.LoadPrefabFromBundle(wand.PrefabName, chebgonazAssetBundle, RadeonFriendly.Value);
                    wand.CreateButtons();
                    KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint());

                    // for orb of beckoning, make sure the custom projectile is set
                    if (wand is OrbOfBeckoning)
                    {
                        wandPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackProjectile =
                            orbOfBeckoningProjectilePrefab;
                    }

                    ItemManager.Instance.AddItem(wand.GetCustomItemFromPrefab(wandPrefab));
                });

                #endregion

                #region CustomPrefabs

                var largeCargoCratePrefab = Base.LoadPrefabFromBundle(LargeCargoCrate.PrefabName,
                    chebgonazAssetBundle, RadeonFriendly.Value);
                PrefabManager.Instance.AddPrefab(new CustomPrefab(largeCargoCratePrefab, false));

                #endregion

                #region Creatures

                List<string> prefabNames = new();

                foreach (DraugrMinion.DraugrType value in Enum.GetValues(typeof(DraugrMinion.DraugrType)))
                {
                    if (value is DraugrMinion.DraugrType.None) continue;
                    prefabNames.Add(InternalName.GetName(value) + ".prefab");
                }

                // 1.2.0: I had to make extra prefabs for each tier because
                // the skeletons consistently forgot their weapons and became
                // buggy (not attacking enemies) if dynamically set
                foreach (SkeletonMinion.SkeletonType value in Enum.GetValues(typeof(SkeletonMinion.SkeletonType)))
                {
                    if (value is SkeletonMinion.SkeletonType.None) continue;
                    prefabNames.Add(InternalName.GetName(value) + ".prefab");
                }

                prefabNames.Add("ChebGonaz_GuardianWraith.prefab");
                prefabNames.Add("ChebGonaz_SpiritPylonGhost.prefab");
                prefabNames.Add("ChebGonaz_NeckroGatherer.prefab");
                prefabNames.Add("ChebGonaz_Bat.prefab");
                prefabNames.Add(BattleNeckroMinion.PrefabName + ".prefab");

                foreach (LeechMinion.LeechType value in Enum.GetValues(typeof(LeechMinion.LeechType)))
                {
                    if (value is LeechMinion.LeechType.None) continue;
                    prefabNames.Add(InternalName.GetName(value) + ".prefab");
                }

                prefabNames.ForEach(prefabName =>
                {
                    var prefab = Base.LoadPrefabFromBundle(prefabName, chebgonazAssetBundle, 
                        RadeonFriendly.Value
                        || NoWraithSmoke.Value && prefabName.Equals("ChebGonaz_GuardianWraith.prefab"));
                    switch (prefabName)
                    {
                        case "ChebGonaz_DraugrWarrior.prefab":
                        case "ChebGonaz_DraugrWarriorTier2.prefab":
                        case "ChebGonaz_DraugrWarriorTier3.prefab":
                        case "ChebGonaz_DraugrWarriorTier4.prefab":
                        case "ChebGonaz_DraugrWarriorNeedle.prefab":
                            prefab.AddComponent<DraugrWarriorMinion>();
                            break;
                        case "ChebGonaz_DraugrArcher.prefab":
                            prefab.AddComponent<DraugrArcherTier1Minion>();
                            break;
                        case "ChebGonaz_DraugrArcherTier2.prefab":
                            prefab.AddComponent<DraugrArcherTier2Minion>();
                            break;
                        case "ChebGonaz_DraugrArcherTier3.prefab":
                            prefab.AddComponent<DraugrArcherTier3Minion>();
                            break;
                        case "ChebGonaz_DraugrArcherPoison.prefab":
                            prefab.AddComponent<DraugrArcherPoisonMinion>();
                            break;
                        case "ChebGonaz_DraugrArcherFire.prefab":
                            prefab.AddComponent<DraugrArcherFireMinion>();
                            break;
                        case "ChebGonaz_DraugrArcherFrost.prefab":
                            prefab.AddComponent<DraugrArcherFrostMinion>();
                            break;
                        case "ChebGonaz_DraugrArcherSilver.prefab":
                            prefab.AddComponent<DraugrArcherSilverMinion>();
                            break;
                        case "ChebGonaz_SkeletonWarrior.prefab":
                        case "ChebGonaz_SkeletonWarriorTier2.prefab":
                        case "ChebGonaz_SkeletonWarriorTier3.prefab":
                        case "ChebGonaz_SkeletonWarriorTier4.prefab":
                        case "ChebGonaz_SkeletonWarriorNeedle.prefab":
                            prefab.AddComponent<SkeletonWarriorMinion>();
                            break;
                        case "ChebGonaz_SkeletonArcher.prefab":
                            prefab.AddComponent<SkeletonArcherTier1Minion>();
                            break;
                        case "ChebGonaz_SkeletonArcherTier2.prefab":
                            prefab.AddComponent<SkeletonArcherTier2Minion>();
                            break;
                        case "ChebGonaz_SkeletonArcherTier3.prefab":
                            prefab.AddComponent<SkeletonArcherTier3Minion>();
                            break;
                        case "ChebGonaz_SkeletonArcherPoison.prefab":
                            prefab.AddComponent<SkeletonArcherPoisonMinion>();
                            break;
                        case "ChebGonaz_SkeletonArcherFire.prefab":
                            prefab.AddComponent<SkeletonArcherFireMinion>();
                            break;
                        case "ChebGonaz_SkeletonArcherFrost.prefab":
                            prefab.AddComponent<SkeletonArcherFrostMinion>();
                            break;
                        case "ChebGonaz_SkeletonArcherSilver.prefab":
                            prefab.AddComponent<SkeletonArcherSilverMinion>();
                            break;
                        case "ChebGonaz_SkeletonMage.prefab":
                        case "ChebGonaz_SkeletonMageTier2.prefab":
                        case "ChebGonaz_SkeletonMageTier3.prefab":
                            prefab.AddComponent<SkeletonMageMinion>();
                            break;
                        case "ChebGonaz_PoisonSkeleton.prefab":
                        case "ChebGonaz_PoisonSkeleton2.prefab":
                        case "ChebGonaz_PoisonSkeleton3.prefab":
                            prefab.AddComponent<PoisonSkeletonMinion>();
                            break;
                        case "ChebGonaz_SkeletonWoodcutter.prefab":
                            prefab.AddComponent<SkeletonWoodcutterMinion>();
                            break;
                        case "ChebGonaz_SkeletonMiner.prefab":
                            prefab.AddComponent<SkeletonMinerMinion>();
                            break;
                        case "ChebGonaz_GuardianWraith.prefab":
                            prefab.AddComponent<GuardianWraithMinion>();
                            break;
                        case "ChebGonaz_SpiritPylonGhost.prefab":
                            prefab.AddComponent<SpiritPylonGhostMinion>();
                            break;
                        case "ChebGonaz_NeckroGatherer.prefab":
                            prefab.AddComponent<NeckroGathererMinion>();
                            break;
                        case "ChebGonaz_Bat.prefab":
                            prefab.AddComponent<BatBeaconBatMinion>();
                            break;
                        case "ChebGonaz_BattleNeckro.prefab":
                            prefab.AddComponent<BattleNeckroMinion>();
                            break;
                        case "ChebGonaz_Leech.prefab":
                            prefab.AddComponent<LeechMinion>();
                            break;
                        default:
                            Logger.LogError($"Unknown prefab {prefabName}");
                            break;
                    }

                    CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                });

                #endregion

                #region Structures

                var spiritPylonPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(SpiritPylon.ChebsRecipeConfig.PrefabName);
                spiritPylonPrefab.AddComponent<SpiritPylon>();
                PieceManager.Instance.AddPiece(
                    SpiritPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(spiritPylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(SpiritPylon.ChebsRecipeConfig.IconName))
                );

                var refuelerPylonPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(RefuelerPylon.ChebsRecipeConfig.PrefabName);
                refuelerPylonPrefab.AddComponent<RefuelerPylon>();
                PieceManager.Instance.AddPiece(
                    RefuelerPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(refuelerPylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(RefuelerPylon.ChebsRecipeConfig.IconName))
                );

                var neckroGathererPylonPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(NeckroGathererPylon.ChebsRecipeConfig.PrefabName);
                neckroGathererPylonPrefab.AddComponent<NeckroGathererPylon>();
                PieceManager.Instance.AddPiece(
                    NeckroGathererPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(neckroGathererPylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(NeckroGathererPylon.ChebsRecipeConfig.IconName))
                );

                var batBeaconPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(BatBeacon.ChebsRecipeConfig.PrefabName);
                batBeaconPrefab.AddComponent<BatBeacon>();
                PieceManager.Instance.AddPiece(
                    BatBeacon.ChebsRecipeConfig.GetCustomPieceFromPrefab(batBeaconPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(BatBeacon.ChebsRecipeConfig.IconName))
                );

                var batLanternPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(BatLantern.ChebsRecipeConfig.PrefabName);
                batLanternPrefab.AddComponent<BatLantern>();
                PieceManager.Instance.AddPiece(
                    BatLantern.ChebsRecipeConfig.GetCustomPieceFromPrefab(batLanternPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(BatLantern.ChebsRecipeConfig.IconName))
                );

                var farmingPylonPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(FarmingPylon.ChebsRecipeConfig.PrefabName);
                farmingPylonPrefab.AddComponent<FarmingPylon>();
                PieceManager.Instance.AddPiece(
                    FarmingPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(farmingPylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(FarmingPylon.ChebsRecipeConfig.IconName))
                );

                var repairPylonPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(RepairPylon.ChebsRecipeConfig.PrefabName);
                repairPylonPrefab.AddComponent<RepairPylon>();
                PieceManager.Instance.AddPiece(
                    RepairPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(repairPylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(RepairPylon.ChebsRecipeConfig.IconName))
                );

                var treasurePylonPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(TreasurePylon.ChebsRecipeConfig.PrefabName);
                treasurePylonPrefab.AddComponent<TreasurePylon>();
                PieceManager.Instance.AddPiece(
                    TreasurePylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(treasurePylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(TreasurePylon.ChebsRecipeConfig.IconName))
                );
                var treasurePylonEffectPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(TreasurePylon.EffectName);
                PrefabManager.Instance.AddPrefab(treasurePylonEffectPrefab);
                
                var phylacteryPrefab =
                    chebgonazAssetBundle.LoadAsset<GameObject>(Phylactery.ChebsRecipeConfig.PrefabName);
                phylacteryPrefab.AddComponent<Phylactery>();
                PieceManager.Instance.AddPiece(
                    Phylactery.ChebsRecipeConfig.GetCustomPieceFromPrefab(phylacteryPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(Phylactery.ChebsRecipeConfig.IconName))
                );

                #endregion

                #region Skills

                var iconSprite = chebgonazAssetBundle.LoadAsset<Sprite>("necromancy_icon.png");
                AddNecromancy(iconSprite);

                #endregion
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while loading assets: {ex}");
            }
            finally
            {
                chebgonazAssetBundle.Unload(false);
            }
        }

        private void AddNecromancy(Sprite iconSprite)
        {
            SkillConfig skill = new()
            {
                Name = "$friendlyskeletonwand_necromancy",
                Description = "$friendlyskeletonwand_necromancy_desc",
                Icon = iconSprite,
                Identifier = NecromancySkillIdentifier
            };

            SkillManager.Instance.AddSkill(skill);

            // necromancy skill doesn't exist until mod is loaded, so we have to set it here rather than in unity
            SetEffectNecromancyArmor.m_skillLevel = SkillManager.Instance.GetSkill(NecromancySkillIdentifier).m_skill;
            SetEffectNecromancyArmor.m_skillLevelModifier = SpectralShroud.NecromancySkillBonus.Value;

            SetEffectNecromancyArmor2.m_skillLevel = SkillManager.Instance.GetSkill(NecromancySkillIdentifier).m_skill;
            SetEffectNecromancyArmor2.m_skillLevelModifier = NecromancerHood.NecromancySkillBonus.Value;
        }
        
        private void Update()
        {
            if (ZInput.instance != null)
            {
                if (Time.time > inputDelay)
                {
                    wands.ForEach(wand =>
                    {
                        if (wand.HandleInputs())
                        {
                            inputDelay = Time.time + .5f;
                        }
                    });
                }
            }

            spectralShroudItem.DoOnUpdate();
        }
    }
}