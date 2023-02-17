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

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local

namespace ChebsNecromancy
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.chebgonaz.ChebsNecromancy";
        public const string PluginName = "ChebsNecromancy";
        public const string PluginVersion = "1.8.4";
        private const string ConfigFileName =  PluginGuid + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

        private readonly Harmony harmony = new(PluginGuid);

        private readonly List<Wand> wands = new()
        {
            new SkeletonWand(),
            new DraugrWand(),
        };
        public const string NecromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private readonly SpectralShroud spectralShroudItem = new();
        private readonly NecromancerHood necromancersHoodItem = new();
        private readonly OrbOfBeckoning orbOfBeckoningItem = new();

        private float inputDelay = 0;

        public static SE_Stats SetEffectNecromancyArmor, SetEffectNecromancyArmor2;

        // Global Config Acceptable Values
        public AcceptableValueList<bool> BoolValue = new(true, false);
        public AcceptableValueRange<float> FloatQuantityValue = new(1f, 1000f);
        public AcceptableValueRange<int> IntQuantityValue = new(1, 1000);
        
        // if set to true, the particle effects that for some reason hurt radeon are dynamically disabled
        public static ConfigEntry<bool> RadeonFriendly;

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
            
            UndeadMinion.CreateConfigs(this);

            SkeletonMinion.CreateConfigs(this);
            DraugrMinion.CreateConfigs(this);
            GuardianWraithMinion.CreateConfigs(this);
            SkeletonWoodcutterMinion.CreateConfigs(this);

            wands.ForEach(w => w.CreateConfigs(this));

            spectralShroudItem.CreateConfigs(this);
            necromancersHoodItem.CreateConfigs(this);
            orbOfBeckoningItem.CreateConfigs(this);

            SpiritPylon.CreateConfigs(this);
            RefuelerPylon.CreateConfigs(this);
            NeckroGathererPylon.CreateConfigs(this);
            BatBeacon.CreateConfigs(this);
            BatLantern.CreateConfigs(this);

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
                        Jotunn.Logger.LogFatal($"AddCustomItems: {prefabName} is null!");
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
                        Jotunn.Logger.LogError($"AddCustomItems: {setEffectName} is null!");
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
                // Add custom projectile script and amke sure the item is using it as its projectile object.
                GameObject orbOfBeckoningProjectilePrefab = 
                    LoadPrefabFromBundle(orbOfBeckoningItem.ProjectilePrefabName, chebgonazAssetBundle);
                orbOfBeckoningProjectilePrefab.AddComponent<OrbOfBeckoningProjectile>();
                
                GameObject orbOfBeckoningItemPrefab =
                    LoadPrefabFromBundle(orbOfBeckoningItem.PrefabName, chebgonazAssetBundle);
                orbOfBeckoningItemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackProjectile =
                    orbOfBeckoningProjectilePrefab;
                ItemManager.Instance.AddItem(orbOfBeckoningItem.GetCustomItemFromPrefab(orbOfBeckoningItemPrefab));
                // // //
                
                // minion worn items
                List<Item> minionWornItems = new()
                {
                    new SkeletonClub(),
                    new SkeletonBow(),
                    new SkeletonBow2(),
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
                    new SkeletonWoodAxe()
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
                    prefabNames.Add("ChebGonaz_DraugrArcher.prefab");
                    prefabNames.Add("ChebGonaz_DraugrWarrior.prefab");
                }

                if (SkeletonWand.SkeletonsAllowed.Value)
                {
                    // 1.2.0: I had to make extra prefabs for each tier because
                    // the skeletons consistently forgot their weapons and became
                    // buggy (not attacking enemies) if dynamically set
                    prefabNames.Add(SkeletonWand.SkeletonWarriorPrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonWarriorTier2PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonWarriorTier3PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonArcherPrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonArcherTier2PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonArcherTier3PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonMagePrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonMageTier2PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonMageTier3PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.PoisonSkeletonPrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.PoisonSkeleton2PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.PoisonSkeleton3PrefabName + ".prefab");
                    prefabNames.Add(SkeletonWand.SkeletonWoodcutterPrefabName + ".prefab");
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

    #region HarmonyPatches

    // Harmony patching is very sensitive regarding parameter names. Everything in this region should be hand crafted
    // and not touched by well-meaning but clueless IDE optimizations.
    // eg.
    // __instance MUST be named with exactly two underscores.
    // ___m_drops MUST be named with exactly three underscores.
    //
    // This is because all of this has a special meaning to Harmony.
    
    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
#pragma warning disable IDE0051 // Remove unused private members
        static void AddBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops)
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (SkeletonWand.BoneFragmentsDroppedAmountMin.Value >= 0
                && SkeletonWand.BoneFragmentsDroppedAmountMax.Value > 0)
            {
                CharacterDrop.Drop bones = new()
                {
                    m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                    m_onePerPlayer = true,
                    m_amountMin = SkeletonWand.BoneFragmentsDroppedAmountMin.Value,
                    m_amountMax = SkeletonWand.BoneFragmentsDroppedAmountMax.Value,
                    m_chance = 1f
                };
                ___m_drops.Add(bones);
            }
        }
    }

    [HarmonyPatch(typeof(Piece))]
    static class ChebGonaz_PiecePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Piece.Awake))]
        static void AwakePostfix(ref Piece __instance)
        {
            if (__instance.name.StartsWith("ChebGonaz"))
            {
                if (__instance.name.Contains("SpiritPylon"))
                {
                    if (__instance.GetComponent<SpiritPylon>() == null)
                    {
                        __instance.gameObject.AddComponent<SpiritPylon>();
                    }
                }
                else if (__instance.name.Contains("RefuelerPylon"))
                {
                    if (__instance.GetComponent<RefuelerPylon>() == null)
                    {
                        __instance.gameObject.AddComponent<RefuelerPylon>();
                    }
                }
                else if (__instance.name.Contains("NeckroGathererPylon"))
                {
                    if (__instance.GetComponent<NeckroGathererPylon>() == null)
                    {
                        __instance.gameObject.AddComponent<NeckroGathererPylon>();
                    }
                }
                else if (__instance.name.Contains("BatBeacon"))
                {
                    if (__instance.GetComponent<BatBeacon>() == null)
                    {
                        __instance.gameObject.AddComponent<BatBeacon>();
                    }
                }
                else if (__instance.name.Contains("BatLantern"))
                {
                    if (__instance.GetComponent<BatLantern>() == null)
                    {
                        __instance.gameObject.AddComponent<BatLantern>();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MonsterAI))]
    static class FriendlySkeletonPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MonsterAI.Awake))]
        static void AwakePostfix(ref Character __instance)
        {
            if (__instance.name.StartsWith("ChebGonaz"))
            {
                if (__instance.name.Contains("Wraith"))
                {
                    __instance.gameObject.AddComponent<GuardianWraithMinion>();
                }
                else
                if (__instance.name.Contains("SpiritPylonGhost") && !__instance.TryGetComponent(out SpiritPylonGhostMinion _))
                {
                    __instance.gameObject.AddComponent<SpiritPylonGhostMinion>();
                }
                else
                {
                    if (!__instance.TryGetComponent(out UndeadMinion _))
                    {
                        if (__instance.name.Contains("Woodcutter"))
                        {
                            __instance.gameObject.AddComponent<SkeletonWoodcutterMinion>();
                        }
                        if (__instance.name.Contains("PoisonSkeleton"))
                        {
                            __instance.gameObject.AddComponent<PoisonSkeletonMinion>();
                        }
                        else if (__instance.name.Contains("Skeleton"))
                        {
                            __instance.gameObject.AddComponent<SkeletonMinion>();
                        }
                        else if (__instance.name.Contains("Draugr"))
                        {
                            __instance.gameObject.AddComponent<DraugrMinion>();
                        }
                        else if (__instance.name.Contains("Neckro"))
                        {
                            __instance.gameObject.AddComponent<NeckroGathererMinion>();
                        }
                        else if (__instance.name.Contains("Bat"))
                        {
                            __instance.gameObject.AddComponent<BatBeaconBatMinion>();
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
    static class ArrowImpactPatch
    {
        // stop minions from damaging player structures
#pragma warning disable IDE0051 // Remove unused private members
        static void Prefix(ref HitData hit, Piece ___m_piece)
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (hit != null)
            {
                Character attacker = hit.GetAttacker();
                if (attacker != null 
                    && attacker.TryGetComponent(out UndeadMinion _))
                {
                    if (___m_piece.IsPlacedByPlayer())
                    {
                        hit.m_damage.m_damage = 0f;
                        hit.m_damage.m_blunt = 0f;
                        hit.m_damage.m_slash = 0f;
                        hit.m_damage.m_pierce = 0f;
                        hit.m_damage.m_chop = 0f;
                        hit.m_damage.m_pickaxe = 0f;
                        hit.m_damage.m_fire = 0f;
                        hit.m_damage.m_frost = 0f;
                        hit.m_damage.m_lightning = 0f;
                        hit.m_damage.m_poison = 0f;
                        hit.m_damage.m_spirit = 0f;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), "OnDeath")]
    static class OnDeathDropPatch
    {
#pragma warning disable IDE0051 // Remove unused private members
        static bool Prefix(CharacterDrop __instance)
#pragma warning restore IDE0051 // Remove unused private members
        {
            // Although Container component is on the Neckro, its OnDestroyed
            // isn't called on the death of the creature. So instead, implement
            // its same functionality in the creature's OnDeath instead.
            if (__instance.TryGetComponent(out NeckroGathererMinion necroNeck))
            {
                if (__instance.TryGetComponent(out Container container))
                {
                    container.DropAllItems(container.m_destroyedLootPrefab);
                    return false; // deny base method completion
                }
            }

            // For all other minions, check if they're supposed to be dropping
            // items and whether tehse should be packed into a crate or not.
            // We don't want ppls surtling cores and things to be claimed by davey jones
            else if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
            {
                void PackDropsIntoCrate()
                {
                    // use vanilla cargo crate -> same as a karve/longboat drops
                    GameObject cratePrefab = ZNetScene.instance.GetPrefab("CargoCrate");
                    if (cratePrefab != null)
                    {
                        // warning: we mustn't ever exceed the maximum storage capacity
                        // of the crate. Not a problem right now, but could be in the future
                        // if the ingredients exceed 4. Right now, can only be 3, so it's fine.
                        // eg. bones, meat, ingot (draugr) OR bones, ingot, surtling core (skele)
                        Inventory inv =
                            GameObject.Instantiate(cratePrefab, __instance.transform.position + Vector3.up, Quaternion.identity)
                            .GetComponent<Container>()
                            .GetInventory();
                        __instance.m_drops.ForEach(drop => inv.AddItem(drop.m_prefab, drop.m_amountMax));
                    }
                }

                if (undeadMinion is SkeletonMinion
                    && SkeletonMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing
                    && SkeletonMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    PackDropsIntoCrate();
                    return false; // deny base method completion
                }
                if (undeadMinion is DraugrMinion
                    && DraugrMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing
                    && DraugrMinion.PackDropItemsIntoCargoCrate.Value)
                {
                    PackDropsIntoCrate();
                    return false; // deny base method completion
                }
            }

            return true; // permit base method to complete
        }
    }

    [HarmonyPatch(typeof(Character), "RPC_Damage")]
    static class CharacterGetDamageModifiersPatch
    {
        // here we basically have to rewrite the entire RPC_Damage verbatim
        // except including the GetBodyArmor that is usually only kept
        // for players and omitted for NPCs. We also discard the durability
        // stuff cuz that doesn't matter for NPCs.
        //
        // I also pruned some player stuff out.
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter
        static bool Prefix(ref long sender, ref HitData hit, Character __instance)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (__instance.TryGetComponent(out UndeadMinion minion)
                && (minion is SkeletonMinion || minion is DraugrMinion))
            {
                if (!__instance.m_nview.IsOwner()
                    || __instance.GetHealth() <= 0f
                    || __instance.IsDead()
                    || __instance.IsTeleporting() 
                    || __instance.InCutscene() 
                    || (hit.m_dodgeable && __instance.IsDodgeInvincible()))
                {
                    return false; // deny base method completion
                }
                Character attacker = hit.GetAttacker();
                if (hit.HaveAttacker() && attacker == null)
                {
                    return false; // deny base method completion
                }
                if (attacker != null && !attacker.IsPlayer())
                {
                    float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(__instance.transform.position);
                    hit.ApplyModifier(difficultyDamageScalePlayer);
                }
                __instance.m_seman.OnDamaged(hit, attacker);
                if (__instance.m_baseAI !=null 
                    && __instance.m_baseAI.IsAggravatable() 
                    && !__instance.m_baseAI.IsAggravated())
                {
                    BaseAI.AggravateAllInArea(__instance.transform.position, 20f, BaseAI.AggravatedReason.Damage);
                }
                if (__instance.m_baseAI != null 
                    && !__instance.m_baseAI.IsAlerted() 
                    && hit.m_backstabBonus > 1f 
                    && Time.time - __instance.m_backstabTime > 300f)
                {
                    __instance.m_backstabTime = Time.time;
                    hit.ApplyModifier(hit.m_backstabBonus);
                    __instance.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
                }
                if (__instance.IsStaggering() && !__instance.IsPlayer())
                {
                    hit.ApplyModifier(2f);
                    __instance.m_critHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
                }
                if (hit.m_blockable && __instance.IsBlocking())
                {
                    __instance.BlockAttack(hit, attacker);
                }
                __instance.ApplyPushback(hit);
                if (!string.IsNullOrEmpty(hit.m_statusEffect))
                {
                    StatusEffect statusEffect = __instance.m_seman.GetStatusEffect(hit.m_statusEffect);
                    if (statusEffect == null)
                    {
                        statusEffect = __instance.m_seman.AddStatusEffect(
                            hit.m_statusEffect, 
                            false,
                            hit.m_itemLevel, 
                            hit.m_skillLevel);
                    }
                    else
                    {
                        statusEffect.ResetTime();
                        statusEffect.SetLevel(hit.m_itemLevel, hit.m_skillLevel);
                    }
                    if (statusEffect != null && attacker != null)
                    {
                        statusEffect.SetAttacker(attacker);
                    }
                }
                WeakSpot weakSpot = __instance.GetWeakSpot(hit.m_weakSpot);
                if (weakSpot != null)
                {
                    ZLog.Log($"HIT Weakspot: {weakSpot.gameObject.name}");
                }
                HitData.DamageModifiers damageModifiers = __instance.GetDamageModifiers(weakSpot);
                hit.ApplyResistance(damageModifiers, out var significantModifier);
                // THIS is what we wrote all the code above for...
                //
                // GetBodyArmor should work, but doesn't. So we tally it up
                // ourselves.
                //
                //float bodyArmor = __instance.GetBodyArmor();
                float bodyArmor = 0f;

                if (__instance.TryGetComponent(out Humanoid humanoid))
                {
                    bodyArmor += humanoid.m_chestItem != null
                        ? humanoid.m_chestItem.m_shared.m_armor : 0;

                    bodyArmor += humanoid.m_legItem != null
                        ? humanoid.m_legItem.m_shared.m_armor : 0;

                    bodyArmor += humanoid.m_helmetItem != null
                        ? humanoid.m_helmetItem.m_shared.m_armor : 0;
                }
                bodyArmor *= SkeletonWand.SkeletonArmorValueMultiplier.Value;
                hit.ApplyArmor(bodyArmor);
                //Jotunn.Logger.LogInfo($"{__instance.name} applied body armor {bodyArmor}");
                // // //
                float poison = hit.m_damage.m_poison;
                float fire = hit.m_damage.m_fire;
                float spirit = hit.m_damage.m_spirit;
                hit.m_damage.m_poison = 0f;
                hit.m_damage.m_fire = 0f;
                hit.m_damage.m_spirit = 0f;
                __instance.ApplyDamage(hit, true, true, significantModifier);
                __instance.AddFireDamage(fire);
                __instance.AddSpiritDamage(spirit);
                __instance.AddPoisonDamage(poison);
                __instance.AddFrostDamage(hit.m_damage.m_frost);
                __instance.AddLightningDamage(hit.m_damage.m_lightning);
                return false; // deny base method completion
            }
            return true; // permit base method to complete
        }
    }

    [HarmonyPatch(typeof(Aoe), "OnHit")]
    static class SharpStakesMinionPatch
    {
        // bool OnHit(Collider collider, Vector3 hitPoint)
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter
        static bool Prefix(Collider collider, Vector3 hitPoint, Aoe __instance)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (collider.TryGetComponent(out UndeadMinion _))
            {
                Piece piece = __instance.GetComponentInParent<Piece>();
                if (piece != null && piece.IsPlacedByPlayer())
                {
                    // stop minion from receiving damage from stakes placed
                    // by a player
                    return false; // deny base method completion
                }
            }
            return true; // permit base method to complete
        }
    }

    [HarmonyPatch(typeof(Tameable), "Interact")]
    static class TameablePatch1
    {
        [HarmonyPrefix]
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter
        static bool InteractPrefix(Humanoid user, bool hold, bool alt, Tameable __instance)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0051 // Remove unused private members
        {
            // Stop players that aren't the owner of a minion from interacting
            // with it. Also call UndeadMinion wait/follow methods to
            // properly update the ZDO with the waiting position.
            if (__instance.TryGetComponent(out UndeadMinion undeadMinion)
                && user.TryGetComponent(out Player player))
            {
                if (!undeadMinion.BelongsToPlayer(player.GetPlayerName()))
                {
                    user.Message(MessageHud.MessageType.Center, "$chebgonaz_notyourminion");
                    return false; // deny base method completion
                }

                if (!UndeadMinion.Commandable.Value)
                {
                    return false; // deny base method completion
                }

                // use the minion methods to ensure the ZDO is updated
                if (__instance.TryGetComponent(out MonsterAI monsterAI))
                {
                    if (monsterAI.GetFollowTarget() == player.gameObject)
                    {
                        user.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_skeletonwaiting");
                        undeadMinion.Wait(player.transform.position);
                    }
                    else
                    {
                        user.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_skeletonfollowing");
                        undeadMinion.Follow(player.gameObject);
                    }
                    return false; // deny base method completion
                }
            }

            return true; // permit base method to complete
        }
    }

    [HarmonyPatch(typeof(Tameable))]
    static class TameablePatch2
    {
        [HarmonyPatch(nameof(Tameable.GetHoverText))]
        [HarmonyPrefix]
        static bool Prefix(Tameable __instance, ref string __result)
        {
            if (__instance.m_nview.IsValid()
                && __instance.m_commandable
                && __instance.TryGetComponent(out UndeadMinion _)
                && __instance.TryGetComponent(out MonsterAI monsterAI)
                && Player.m_localPlayer != null)
            {
                __result = monsterAI.GetFollowTarget() == Player.m_localPlayer.gameObject
                    ? Localization.instance.Localize("$chebgonaz_wait")
                    : Localization.instance.Localize("$chebgonaz_follow");
                return false; // deny base method completion
            }

            return true; // allow base method completion
        }
    }
    
    [HarmonyPatch(typeof(BaseAI))]
    class BaseAIPatch
    {
        [HarmonyPatch(nameof(BaseAI.Follow))]
        [HarmonyPrefix]
        static bool Prefix(GameObject go, float dt, BaseAI __instance)
        {
            if (__instance.TryGetComponent(out UndeadMinion undeadMinion))
            {
                // use our custom implementation with custom follow distance
                float num = Vector3.Distance(go.transform.position, __instance.transform.position);
                // todo: expose to config
                bool run = num > 10f;
                // todo: expose to config
                var approachRange = undeadMinion is SkeletonWoodcutterMinion ? 0.5f : 3f;
                if (num < approachRange)
                {
                    __instance.StopMoving();
                }
                else
                {
                    __instance.MoveTo(dt, go.transform.position, 0f, run);
                }
                
                return false; // deny base method completion
            }

            return true; // allow base method completion
        }
    }

    #endregion
}

