// ChebsNecromancy
// 
// File:    ChebsNecromancy.cs
// Project: ChebsNecromancy

using BepInEx;
using ChebsNecromancy.Commands;
using ChebsNecromancy.Minions;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace ChebsNecromancy
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.chebgonaz.ChebsNecromancy";
        public const string PluginName = "ChebsNecromancy";
        public const string PluginVersion = "1.6.5";
        private const string ConfigFileName =  PluginGUID + ".cfg";
        private static readonly string ConfigFileFullPath = Path.Combine(BepInEx.Paths.ConfigPath, ConfigFileName);

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private List<Wand> wands = new List<Wand>()
        {
            new SkeletonWand(),
            new DraugrWand(),
        };
        public const string necromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private SpectralShroud spectralShroudItem = new SpectralShroud();
        private NecromancerHood necromancersHoodItem = new NecromancerHood();

        private float inputDelay = 0;

        public static SE_Stats setEffectNecromancyArmor, setEffectNecromancyArmor2;

        private void Awake()
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

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            UndeadMinion.CreateConfigs(this);

            SkeletonMinion.CreateConfigs(this);
            DraugrMinion.CreateConfigs(this);
            GuardianWraithMinion.CreateConfigs(this);

            wands.ForEach(w => w.CreateConfigs(this));

            spectralShroudItem.CreateConfigs(this);
            necromancersHoodItem.CreateConfigs(this);

            SpiritPylon.CreateConfigs(this);
            RefuelerPylon.CreateConfigs(this);
            NeckroGathererPylon.CreateConfigs(this);
            BatBeacon.CreateConfigs(this);

            LargeCargoCrate.CreateConfigs(this);

            NeckroGathererMinion.CreateConfigs(this);
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(BepInEx.Paths.ConfigPath, ConfigFileName);
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
                        Jotunn.Logger.LogError($"AddCustomItems: {prefabName} is null!");
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
                setEffectNecromancyArmor = LoadSetEffectFromBundle("SetEffect_NecromancyArmor", chebgonazAssetBundle);
                setEffectNecromancyArmor2 = LoadSetEffectFromBundle("SetEffect_NecromancyArmor2", chebgonazAssetBundle);
                #endregion

                #region Items
                // by great Cthulhu, this needs refactoring!
                //
                GameObject spectralShroudPrefab = LoadPrefabFromBundle(spectralShroudItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(spectralShroudItem.GetCustomItemFromPrefab(spectralShroudPrefab));

                GameObject necromancersHoodPrefab = LoadPrefabFromBundle(necromancersHoodItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(necromancersHoodItem.GetCustomItemFromPrefab(necromancersHoodPrefab));

                // minion worn items
                List<Item> minionWornItems = new List<Item>
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
                };
                minionWornItems.ForEach((minionItem) =>
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
                    container.m_width = LargeCargoCrate.containerWidth.Value;
                    container.m_height = LargeCargoCrate.containerHeight.Value;
                }
                else
                {
                    Jotunn.Logger.LogError($"Failed to retrieve Container component from {LargeCargoCrate.PrefabName}.");
                }
                PrefabManager.Instance.AddPrefab(new CustomPrefab(largeCargoCratePrefab, false));
                #endregion

                #region Creatures
                List<string> prefabNames = new List<string>();

                if (DraugrWand.draugrAllowed.Value)
                {
                    prefabNames.Add("ChebGonaz_DraugrArcher.prefab");
                    prefabNames.Add("ChebGonaz_DraugrWarrior.prefab");
                }

                if (SkeletonWand.skeletonsAllowed.Value)
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
                }

                if (SpectralShroud.spawnWraith.Value)
                {
                    prefabNames.Add("ChebGonaz_GuardianWraith.prefab");
                }

                if (SpiritPylon.allowed.Value)
                {
                    prefabNames.Add("ChebGonaz_SpiritPylonGhost.prefab");
                }

                if (NeckroGathererMinion.allowed.Value && LargeCargoCrate.allowed.Value)
                {
                    prefabNames.Add("ChebGonaz_NeckroGatherer.prefab");
                }

                if (BatBeacon.allowed.Value)
                {
                    prefabNames.Add("ChebGonaz_Bat.prefab");
                }

                prefabNames.ForEach(prefabName =>
                {
                    //Jotunn.Logger.LogInfo($"Loading {prefabName}...");
                    GameObject prefab = chebgonazAssetBundle.LoadAsset<GameObject>(prefabName);
                    if (prefab == null) { Jotunn.Logger.LogError($"prefab for {prefabName} is null!"); }

                    CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                }
                    );
                #endregion

                #region Structures   
                GameObject spiritPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(SpiritPylon.PrefabName);
                spiritPylonPrefab.AddComponent<SpiritPylon>();
                PieceManager.Instance.AddPiece(
                    new SpiritPylon().GetCustomPieceFromPrefab(spiritPylonPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(SpiritPylon.IconName))
                    );

                GameObject refuelerPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(RefuelerPylon.PrefabName);
                refuelerPylonPrefab.AddComponent<RefuelerPylon>();
                PieceManager.Instance.AddPiece(
                    new RefuelerPylon().GetCustomPieceFromPrefab(refuelerPylonPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(RefuelerPylon.IconName))
                    );

                GameObject neckroGathererPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(NeckroGathererPylon.PrefabName);
                neckroGathererPylonPrefab.AddComponent<NeckroGathererPylon>();
                PieceManager.Instance.AddPiece(
                    new NeckroGathererPylon().GetCustomPieceFromPrefab(neckroGathererPylonPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(NeckroGathererPylon.IconName))
                    );

                GameObject batBeaconPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(BatBeacon.PrefabName);
                batBeaconPrefab.AddComponent<BatBeacon>();
                PieceManager.Instance.AddPiece(
                    new BatBeacon().GetCustomPieceFromPrefab(batBeaconPrefab,
                    chebgonazAssetBundle.LoadAsset<Sprite>(BatBeacon.IconName))
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
            SkillConfig skill = new SkillConfig();
            skill.Name = "$friendlyskeletonwand_necromancy";
            skill.Description = "$friendlyskeletonwand_necromancy_desc";
            skill.IconPath = iconPath;
            skill.Identifier = necromancySkillIdentifier;

            SkillManager.Instance.AddSkill(skill);

            // necromancy skill doesn't exist until mod is loaded, so we have to set it here rather than in unity
            setEffectNecromancyArmor.m_skillLevel = SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill;
            setEffectNecromancyArmor.m_skillLevelModifier = SpectralShroud.necromancySkillBonus.Value;

            setEffectNecromancyArmor2.m_skillLevel = SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill;
            setEffectNecromancyArmor2.m_skillLevelModifier = NecromancerHood.necromancySkillBonus.Value;
        }

        private void Update()
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

    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
        static void addBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops)
        {
            if (SkeletonWand.boneFragmentsDroppedAmountMin.Value >= 0
                && SkeletonWand.boneFragmentsDroppedAmountMax.Value > 0)
            {
                CharacterDrop.Drop bones = new CharacterDrop.Drop();
                bones.m_prefab = ZNetScene.instance.GetPrefab("BoneFragments");
                bones.m_onePerPlayer = true;
                bones.m_amountMin = SkeletonWand.boneFragmentsDroppedAmountMin.Value;
                bones.m_amountMax = SkeletonWand.boneFragmentsDroppedAmountMax.Value;
                bones.m_chance = 1f;
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
                if (__instance.name.Contains("SpiritPylonGhost") && __instance.GetComponent<SpiritPylonGhostMinion>() == null)
                {
                    __instance.gameObject.AddComponent<SpiritPylonGhostMinion>();
                }
                else
                {
                    if (__instance.GetComponent<UndeadMinion>() == null)
                    {
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
        static void Prefix(ref HitData hit, Piece ___m_piece)
        {
            if (hit != null)
            {
                Character attacker = hit.GetAttacker();
                if (attacker != null 
                    && attacker.TryGetComponent(out UndeadMinion undeadMinion))
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
        static bool Prefix(CharacterDrop __instance)
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
                    && SkeletonMinion.dropOnDeath.Value != UndeadMinion.DropType.Nothing
                    && SkeletonMinion.packDropItemsIntoCargoCrate.Value)
                {
                    PackDropsIntoCrate();
                    return false; // deny base method completion
                }
                if (undeadMinion is DraugrMinion
                    && DraugrMinion.dropOnDeath.Value != UndeadMinion.DropType.Nothing
                    && DraugrMinion.packDropItemsIntoCargoCrate.Value)
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
        static bool Prefix(ref long sender, ref HitData hit, Character __instance)
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
                bodyArmor *= SkeletonWand.skeletonArmorValueMultiplier.Value;
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
        static bool Prefix(Collider collider, Vector3 hitPoint, Aoe __instance)
        {
            if (collider.TryGetComponent(out UndeadMinion minion))
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
        static bool InteractPrefix(Humanoid user, bool hold, bool alt, Tameable __instance)
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

                if (!UndeadMinion.commandable.Value)
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
            if (!__instance.m_nview.IsValid()
                || !__instance.m_commandable
                || !__instance.TryGetComponent(out MonsterAI monsterAI)
                || Player.m_localPlayer == null)
            {
                __result = "";
            }
            else
            {
                __result = monsterAI.GetFollowTarget() == Player.m_localPlayer.gameObject
                ? Localization.instance.Localize("$chebgonaz_wait")
                : Localization.instance.Localize("$chebgonaz_follow");
            }

            return false; // deny base method completion
        }
    }
    #endregion
}

