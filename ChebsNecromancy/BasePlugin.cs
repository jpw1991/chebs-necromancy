// ChebsNecromancy
// 
// File:    ChebsNecromancy.cs
// Project: ChebsNecromancy

using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Commands;
using ChebsNecromancy.CustomPrefabs;
using ChebsNecromancy.Items;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Structures;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;
using Logger = UnityEngine.Logger;
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
        public const string PluginVersion = "1.9.2";
        private const string ConfigFileName =  PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        private readonly Harmony harmony = new(PluginGuid);

        private readonly List<Wand> wands = new()
        {
            new SkeletonWand(),
            new DraugrWand(),
            new OrbOfBeckoning()
        };
        public const string NecromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private readonly SpectralShroud spectralShroudItem = new();
        private readonly NecromancerHood necromancersHoodItem = new();

        private float inputDelay = 0;

        public static SE_Stats SetEffectNecromancyArmor, SetEffectNecromancyArmor2;

        // Global Config Acceptable Values
        public AcceptableValueList<bool> BoolValue = new(true, false);
        public AcceptableValueRange<float> FloatQuantityValue = new(1f, 1000f);
        public AcceptableValueRange<int> IntQuantityValue = new(1, 1000);
        
        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly;
        
        public static ConfigEntry<int> BoneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> BoneFragmentsDroppedAmountMax;
        public static ConfigEntry<float> BoneFragmentsDroppedChance;
        
        public static ConfigEntry<int> ArmorLeatherScrapsRequiredConfig;
        public static ConfigEntry<int> ArmorBronzeRequiredConfig;
        public static ConfigEntry<int> ArmorIronRequiredConfig;
        public static ConfigEntry<int> ArmorBlackIronRequiredConfig;
        public static ConfigEntry<int> SurtlingCoresRequiredConfig;
        public static ConfigEntry<int> ArcherTier1ArrowsRequiredConfig;
        public static ConfigEntry<int> ArcherTier2ArrowsRequiredConfig;
        public static ConfigEntry<int> ArcherTier3ArrowsRequiredConfig;
        
        public static ConfigEntry<bool> DurabilityDamage;
        public static ConfigEntry<float> DurabilityDamageWarrior;
        public static ConfigEntry<float> DurabilityDamageMage;
        public static ConfigEntry<float> DurabilityDamageArcher;
        public static ConfigEntry<float> DurabilityDamagePoison;
        public static ConfigEntry<float> DurabilityDamageLeather;
        public static ConfigEntry<float> DurabilityDamageBronze;
        public static ConfigEntry<float> DurabilityDamageIron;
        public static ConfigEntry<float> DurabilityDamageBlackIron;

#pragma warning disable IDE0051 // Remove unused private members
        private void Awake()
#pragma warning restore IDE0051 // Remove unused private members
        {
            CreateConfigValues();

            LoadChebGonazAssetBundle();

            harmony.PatchAll();

            AddNecromancy();

            CommandManager.Instance.AddConsoleCommand(new KillAllMinions());
            CommandManager.Instance.AddConsoleCommand(new SummonAllMinions());
            CommandManager.Instance.AddConsoleCommand(new KillAllNeckros());
            CommandManager.Instance.AddConsoleCommand(new SetMinionOwnership());

            SetupWatcher();
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

            RadeonFriendly = Config.Bind("General (Client)", "RadeonFriendly",
                false, new ConfigDescription("ONLY set this to true if you have graphical issues with " +
                                             "the mod. It will disable all particle effects for the mod's prefabs " +
                                             "which seem to give users with Radeon cards trouble for unknown " +
                                             "reasons. If you have problems with lag it might also help to switch" +
                                             "this setting on."));

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
                2, new ConfigDescription("The amount of LeatherScraps required to craft a minion in leather armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBronzeRequiredConfig = Config.Bind("General (Server Synced)", "ArmorBronzeRequired",
                1, new ConfigDescription("The amount of Bronze required to craft a minion in bronze armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorIronRequiredConfig = Config.Bind("General (Server Synced)", "ArmoredIronRequired",
                1, new ConfigDescription("The amount of Iron required to craft a minion in iron armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SurtlingCoresRequiredConfig = Config.Bind("General (Server Synced)", "MageSurtlingCoresRequired",
                1, new ConfigDescription("The amount of surtling cores required to craft a mage.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBlackIronRequiredConfig = Config.Bind("General (Server Synced)", "ArmorBlackIronRequired",
                1, new ConfigDescription("The amount of Black Metal required to craft a minion in black iron armor.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            ArcherTier1ArrowsRequiredConfig = Config.Bind("General (Server Synced)", "ArcherTier1ArrowsRequired",
                10, new ConfigDescription("The amount of wood arrows required to craft a tier 1 archer.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            ArcherTier2ArrowsRequiredConfig = Config.Bind("General (Server Synced)", "ArcherTier2ArrowsRequired",
                10, new ConfigDescription("The amount of bronze arrows required to craft a tier 2 archer.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            ArcherTier3ArrowsRequiredConfig = Config.Bind("General (Server Synced)", "ArcherTier3ArrowsRequired",
                10, new ConfigDescription("The amount of iron arrows required to craft a tier 3 archer.", null,
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
                1f, new ConfigDescription("How much armoring the minion in leather damages the wand (value is added on top of damage from minion type).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageBronze = Config.Bind("General (Server Synced)", "DurabilityDamageBronze",
                1f, new ConfigDescription("How much armoring the minion in bronze damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageIron = Config.Bind("General (Server Synced)", "DurabilityDamageIron",
                1f, new ConfigDescription("How much armoring the minion in iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageBlackIron = Config.Bind("General (Server Synced)", "DurabilityDamageBlackIron",
                1f, new ConfigDescription("How much armoring the minion in black iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            #endregion
            
            UndeadMinion.CreateConfigs(this);
            SkeletonMinion.CreateConfigs(this);
            DraugrMinion.CreateConfigs(this);
            GuardianWraithMinion.CreateConfigs(this);
            SkeletonWoodcutterMinion.CreateConfigs(this);
            SkeletonMinerMinion.CreateConfigs(this);

            wands.ForEach(w => w.CreateConfigs(this));

            spectralShroudItem.CreateConfigs(this);
            necromancersHoodItem.CreateConfigs(this);

            SpiritPylon.CreateConfigs(this);
            RefuelerPylon.CreateConfigs(this);
            NeckroGathererPylon.CreateConfigs(this);
            BatBeacon.CreateConfigs(this);
            BatLantern.CreateConfigs(this);
            FarmingPylon.CreateConfigs(this);

            LargeCargoCrate.CreateConfigs(this);

            NeckroGathererMinion.CreateConfigs(this);
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.Error += (sender, e) => Jotunn.Logger.LogError($"Error watching for config changes: {e}");
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Jotunn.Logger.LogInfo("Read updated config values");
                Config.Reload();
            }
            catch
            {
                Jotunn.Logger.LogError($"There was an issue loading your {ConfigFileName}");
                Jotunn.Logger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private void LoadChebGonazAssetBundle()
        {
            // order is important (I think): items, creatures, structures
            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "chebgonaz");
            AssetBundle chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                GameObject LoadPrefabFromBundle(string prefabName, AssetBundle bundle)
                {
                    //Jotunn.Logger.LogInfo($"Loading {prefabName}...");
                    GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        Jotunn.Logger.LogFatal($"LoadPrefabFromBundle: {prefabName} is null!");
                    }

                    if (RadeonFriendly.Value)
                    {
                        foreach (var child in prefab.GetComponentsInChildren<ParticleSystem>())
                        {
                            //Logger.LogInfo($"Prefab name: {prefabName}: Destroying ParticleSystem {child.name}");
                            Destroy(child);
                        }

                        if (prefab.TryGetComponent(out Humanoid humanoid))
                        {
                            humanoid.m_deathEffects = new EffectList();
                            humanoid.m_dropEffects = new EffectList();
                            humanoid.m_equipEffects = new EffectList();
                            humanoid.m_pickupEffects = new EffectList();
                            humanoid.m_consumeItemEffects = new EffectList();
                            humanoid.m_hitEffects = new EffectList();
                            humanoid.m_jumpEffects = new EffectList();
                            humanoid.m_slideEffects = new EffectList();
                            humanoid.m_perfectBlockEffect = new EffectList();
                            humanoid.m_tarEffects = new EffectList();
                            humanoid.m_waterEffects = new EffectList();
                            humanoid.m_flyingContinuousEffect = new EffectList();
                        }
                    }
                    
                    return prefab;
                }

                SE_Stats LoadSetEffectFromBundle(string setEffectName, AssetBundle bundle)
                {
                    //Jotunn.Logger.LogInfo($"Loading {setEffectName}...");
                    SE_Stats seStat = bundle.LoadAsset<SE_Stats>(setEffectName);
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
                GameObject spectralShroudPrefab = LoadPrefabFromBundle(spectralShroudItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(spectralShroudItem.GetCustomItemFromPrefab(spectralShroudPrefab));

                GameObject necromancersHoodPrefab = LoadPrefabFromBundle(necromancersHoodItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(necromancersHoodItem.GetCustomItemFromPrefab(necromancersHoodPrefab));

                // // //
                // Orb of Beckoning
                //
                GameObject orbOfBeckoningProjectilePrefab = 
                    LoadPrefabFromBundle(OrbOfBeckoning.ProjectilePrefabName, chebgonazAssetBundle);
                orbOfBeckoningProjectilePrefab.AddComponent<OrbOfBeckoningProjectile>();
                
                // GameObject orbOfBeckoningItemPrefab =
                //     LoadPrefabFromBundle(orbOfBeckoningItem.PrefabName, chebgonazAssetBundle);
                // orbOfBeckoningItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackProjectile =
                //     orbOfBeckoningProjectilePrefab;
                // ItemManager.Instance.AddItem(orbOfBeckoningItem.GetCustomItemFromPrefab(orbOfBeckoningItemPrefab));
                // // //
                
                // minion worn items
                List<Item> minionWornItems = new()
                {
                    new SkeletonClub(),
                    new SkeletonBow(),
                    new SkeletonBow2(),
                    new SkeletonBow3(),
                    new SkeletonHelmetLeather(),
                    new SkeletonHelmetBronze(),
                    new SkeletonHelmetIron(),
                    new SkeletonFireballLevel1(),
                    new SkeletonFireballLevel2(),
                    new SkeletonFireballLevel3(),
                    new SkeletonMageCirclet(),
                    new SkeletonAxe(),
                    new BlackIronChest(),
                    new BlackIronHelmet(),
                    new BlackIronLegs(),
                    new SkeletonHelmetBlackIron(),
                    new SkeletonMace(),
                    new SkeletonMace2(),
                    new SkeletonMace3(),
                    new SkeletonHelmetIronPoison(),
                    new SkeletonHelmetBlackIronPoison(),
                    new SkeletonHelmetLeatherPoison(),
                    new SkeletonHelmetBronzePoison(),
                    new SkeletonWoodAxe(),
                    new SkeletonPickaxe(),
                    new SkeletonAxeBlackMetal(),
                    new SkeletonAxeBronze(),
                    new SkeletonMaceBlackMetal(),
                    new SkeletonMaceBronze(),
                    new SkeletonMaceIron(),
                    new SkeletonSwordBlackMetal(),
                    new SkeletonSwordBronze(),
                    new SkeletonSwordIron()
                };
                minionWornItems.ForEach(minionItem =>
                {
                    GameObject minionItemPrefab = LoadPrefabFromBundle(minionItem.PrefabName, chebgonazAssetBundle);
                    ItemManager.Instance.AddItem(minionItem.GetCustomItemFromPrefab(minionItemPrefab));
                });

                wands.ForEach(wand =>
                {
                    // we do the keyhints later after vanilla items are available
                    // so we can override what's in the prefab
                    GameObject wandPrefab = LoadPrefabFromBundle(wand.PrefabName, chebgonazAssetBundle);
                    wand.CreateButtons();
                    KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint());
                    
                    // for orb of beckoning, make sure the custom projectile is set
                    if (wand is OrbOfBeckoning)
                    {
                        wandPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackProjectile = orbOfBeckoningProjectilePrefab;                        
                    }

                    ItemManager.Instance.AddItem(wand.GetCustomItemFromPrefab(wandPrefab));
                });
                #endregion

                #region CustomPrefabs
                GameObject largeCargoCratePrefab = LoadPrefabFromBundle(LargeCargoCrate.PrefabName, chebgonazAssetBundle);
                if (largeCargoCratePrefab.TryGetComponent(out Container container))
                {
                    container.m_width = LargeCargoCrate.ContainerWidth.Value;
                    container.m_height = LargeCargoCrate.ContainerHeight.Value;
                }
                else
                {
                    Jotunn.Logger.LogError($"Failed to retrieve Container component from {LargeCargoCrate.PrefabName}.");
                }
                PrefabManager.Instance.AddPrefab(new CustomPrefab(largeCargoCratePrefab, false));
                #endregion

                #region Creatures
                List<string> prefabNames = new();

                if (DraugrWand.DraugrAllowed.Value)
                {
                    foreach (DraugrMinion.DraugrType value in Enum.GetValues(typeof(DraugrMinion.DraugrType)))
                    {
                        if (value is DraugrMinion.DraugrType.None) continue;
                        prefabNames.Add(InternalName.GetName(value) + ".prefab");
                    }
                }

                if (SkeletonWand.SkeletonsAllowed.Value)
                {
                    // 1.2.0: I had to make extra prefabs for each tier because
                    // the skeletons consistently forgot their weapons and became
                    // buggy (not attacking enemies) if dynamically set
                    foreach (SkeletonMinion.SkeletonType value in Enum.GetValues(typeof(SkeletonMinion.SkeletonType)))
                    {
                        if (value is SkeletonMinion.SkeletonType.None) continue;
                        prefabNames.Add(InternalName.GetName(value) + ".prefab");
                    }
                }

                if (SpectralShroud.SpawnWraith.Value)
                {
                    prefabNames.Add("ChebGonaz_GuardianWraith.prefab");
                }

                if (SpiritPylon.ChebsRecipeConfig.Allowed.Value)
                {
                    prefabNames.Add("ChebGonaz_SpiritPylonGhost.prefab");
                }

                if (NeckroGathererMinion.Allowed.Value && LargeCargoCrate.Allowed.Value)
                {
                    prefabNames.Add("ChebGonaz_NeckroGatherer.prefab");
                }

                if (BatBeacon.ChebsRecipeConfig.Allowed.Value)
                {
                    prefabNames.Add("ChebGonaz_Bat.prefab");
                }

                prefabNames.ForEach(prefabName =>
                {
                    //Jotunn.Logger.LogInfo($"Loading {prefabName}...");
                    //GameObject prefab = chebgonazAssetBundle.LoadAsset<GameObject>(prefabName);
                    //if (prefab == null) { Jotunn.Logger.LogError($"prefab for {prefabName} is null!"); }
                    var prefab = LoadPrefabFromBundle(prefabName, chebgonazAssetBundle);

                    CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                });
                #endregion

                #region Structures   
                GameObject spiritPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(SpiritPylon.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    SpiritPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(spiritPylonPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(SpiritPylon.ChebsRecipeConfig.IconName))
                    );

                GameObject refuelerPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(RefuelerPylon.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    RefuelerPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(refuelerPylonPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(RefuelerPylon.ChebsRecipeConfig.IconName))
                    );

                GameObject neckroGathererPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(NeckroGathererPylon.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    NeckroGathererPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(neckroGathererPylonPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(NeckroGathererPylon.ChebsRecipeConfig.IconName))
                    );

                GameObject batBeaconPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(BatBeacon.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    BatBeacon.ChebsRecipeConfig.GetCustomPieceFromPrefab(batBeaconPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(BatBeacon.ChebsRecipeConfig.IconName))
                    );
                
                GameObject batLanternPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(BatLantern.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    BatLantern.ChebsRecipeConfig.GetCustomPieceFromPrefab(batLanternPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(BatLantern.ChebsRecipeConfig.IconName))
                );
                
                GameObject farmingPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(FarmingPylon.ChebsRecipeConfig.PrefabName);
                PieceManager.Instance.AddPiece(
                    FarmingPylon.ChebsRecipeConfig.GetCustomPieceFromPrefab(farmingPylonPrefab,
                        chebgonazAssetBundle.LoadAsset<Sprite>(FarmingPylon.ChebsRecipeConfig.IconName))
                );
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

        private void AddNecromancy()
        {
            string iconPath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "necromancy_icon.png");
            SkillConfig skill = new()
            {
                Name = "$friendlyskeletonwand_necromancy",
                Description = "$friendlyskeletonwand_necromancy_desc",
                IconPath = iconPath,
                Identifier = NecromancySkillIdentifier
            };

            SkillManager.Instance.AddSkill(skill);

            // necromancy skill doesn't exist until mod is loaded, so we have to set it here rather than in unity
            SetEffectNecromancyArmor.m_skillLevel = SkillManager.Instance.GetSkill(NecromancySkillIdentifier).m_skill;
            SetEffectNecromancyArmor.m_skillLevelModifier = SpectralShroud.NecromancySkillBonus.Value;

            SetEffectNecromancyArmor2.m_skillLevel = SkillManager.Instance.GetSkill(NecromancySkillIdentifier).m_skill;
            SetEffectNecromancyArmor2.m_skillLevelModifier = NecromancerHood.NecromancySkillBonus.Value;
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void Update()
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (ZInput.instance != null)
            {
                if (Time.time > inputDelay)
                {
                    wands.ForEach(wand => {
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

