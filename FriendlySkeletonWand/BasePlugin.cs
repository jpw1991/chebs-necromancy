// FriendlySkeletonWand
// 
// File:    FriendlySkeletonWand.cs
// Project: FriendlySkeletonWand

using BepInEx;
using BepInEx.Configuration;
using FriendlySkeletonWand.Minions;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace FriendlySkeletonWand
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.chebgonaz.FriendlySkeletonWand";
        public const string PluginName = "FriendlySkeletonWand";
        public const string PluginVersion = "1.1.0";

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
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();

            AddCustomItems();
            AddCustomCreatures();
            AddCustomStructures();

            harmony.PatchAll();

            //AddButtons();
            AddNecromancy();

            //PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            GuardianWraithMinion.CreateConfigs(this);

            wands.ForEach(w => w.CreateConfigs(this));

            spectralShroudItem.CreateConfigs(this);
            necromancersHoodItem.CreateConfigs(this);

            SpiritPylon.CreateConfigs(this);
        }


        private void AddCustomItems()
        {
            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "chebgonazitems");
            AssetBundle chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                GameObject LoadPrefabFromBundle(string prefabName, AssetBundle bundle)
                {
                    Jotunn.Logger.LogInfo($"Loading {prefabName}...");
                    GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);
                    if (prefab == null)
                    {
                        Jotunn.Logger.LogError($"AddCustomItems: {prefabName} is null!");
                    }
                    return prefab;
                }

                SE_Stats LoadSetEffectFromBundle(string setEffectName, AssetBundle bundle)
                {
                    Jotunn.Logger.LogInfo($"Loading {setEffectName}...");
                    SE_Stats seStat = bundle.LoadAsset<SE_Stats>(setEffectName);
                    if (seStat == null)
                    {
                        Jotunn.Logger.LogError($"AddCustomItems: {setEffectName} is null!");
                    }
                    return seStat;
                }

                setEffectNecromancyArmor = LoadSetEffectFromBundle("SetEffect_NecromancyArmor", chebgonazAssetBundle);
                setEffectNecromancyArmor2 = LoadSetEffectFromBundle("SetEffect_NecromancyArmor2", chebgonazAssetBundle);

                GameObject spectralShroudPrefab = LoadPrefabFromBundle(spectralShroudItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(spectralShroudItem.GetCustomItemFromPrefab(spectralShroudPrefab));

                GameObject necromancersHoodPrefab = LoadPrefabFromBundle(necromancersHoodItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(necromancersHoodItem.GetCustomItemFromPrefab(necromancersHoodPrefab));

                // minion worn items
                SkeletonClub skeletonClubItem = new SkeletonClub();
                GameObject skeletonClubPrefab = LoadPrefabFromBundle(skeletonClubItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(new SkeletonClub().GetCustomItemFromPrefab(skeletonClubPrefab));

                SkeletonBow skeletonBowItem = new SkeletonBow();
                GameObject skeletonBowPrefab = LoadPrefabFromBundle(skeletonBowItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(new SkeletonBow().GetCustomItemFromPrefab(skeletonBowPrefab));

                SkeletonBow2 skeletonBow2Item = new SkeletonBow2();
                GameObject skeletonBow2Prefab = LoadPrefabFromBundle(skeletonBow2Item.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(new SkeletonBow2().GetCustomItemFromPrefab(skeletonBow2Prefab));

                SkeletonHelmetLeather skeletonHelmetLeatherItem = new SkeletonHelmetLeather();
                GameObject skeletonHelmetLeatherPrefab = LoadPrefabFromBundle(skeletonHelmetLeatherItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonHelmetLeatherItem.GetCustomItemFromPrefab(skeletonHelmetLeatherPrefab));

                SkeletonHelmetBronze skeletonHelmetBronzeItem = new SkeletonHelmetBronze();
                GameObject skeletonHelmetBronzePrefab = LoadPrefabFromBundle(skeletonHelmetBronzeItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonHelmetBronzeItem.GetCustomItemFromPrefab(skeletonHelmetBronzePrefab));

                SkeletonHelmetIron skeletonHelmetIronItem = new SkeletonHelmetIron();
                GameObject skeletonHelmetIronPrefab = LoadPrefabFromBundle(skeletonHelmetIronItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonHelmetIronItem.GetCustomItemFromPrefab(skeletonHelmetIronPrefab));

                SkeletonFireballLevel1 skeletonFireballLevel1Item = new SkeletonFireballLevel1();
                GameObject skeletonFireballLevel1Prefab = LoadPrefabFromBundle(skeletonFireballLevel1Item.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonFireballLevel1Item.GetCustomItemFromPrefab(skeletonFireballLevel1Prefab));

                SkeletonFireballLevel2 skeletonFireballLevel2Item = new SkeletonFireballLevel2();
                GameObject skeletonFireballLevel2Prefab = LoadPrefabFromBundle(skeletonFireballLevel2Item.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonFireballLevel2Item.GetCustomItemFromPrefab(skeletonFireballLevel2Prefab));

                SkeletonFireballLevel3 skeletonFireballLevel3Item = new SkeletonFireballLevel3();
                GameObject skeletonFireballLevel3Prefab = LoadPrefabFromBundle(skeletonFireballLevel3Item.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonFireballLevel3Item.GetCustomItemFromPrefab(skeletonFireballLevel3Prefab));

                SkeletonMageCirclet skeletonMageCircletItem = new SkeletonMageCirclet();
                GameObject skeletonMageCircletPrefab = LoadPrefabFromBundle(skeletonMageCircletItem.PrefabName, chebgonazAssetBundle);
                ItemManager.Instance.AddItem(skeletonMageCircletItem.GetCustomItemFromPrefab(skeletonMageCircletPrefab));

                wands.ForEach(wand =>
                {
                    // we do the keyhints later after vanilla items are available
                    // so we can override what's in the prefab
                    GameObject wandPrefab = LoadPrefabFromBundle(wand.PrefabName, chebgonazAssetBundle);
                    wand.CreateButtons();
                    KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint());
                    ItemManager.Instance.AddItem(wand.GetCustomItemFromPrefab(wandPrefab));
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while loading custom items: {ex}");
            }
            finally
            {
                chebgonazAssetBundle.Unload(false);
            }
        }

        private void AddCustomCreatures()
        {
            List<string> prefabNames = new List<string>();

            if (DraugrWand.draugrAllowed.Value)
            {
                prefabNames.Add("ChebGonaz_DraugrArcher.prefab");
                prefabNames.Add("ChebGonaz_DraugrWarrior.prefab");
            }

            if (SkeletonWand.skeletonsAllowed.Value)
            {
                prefabNames.Add("ChebGonaz_SkeletonWarrior.prefab");
                prefabNames.Add("ChebGonaz_SkeletonArcher.prefab");
                prefabNames.Add("ChebGonaz_PoisonSkeleton.prefab");
            }

            if (SpectralShroud.spawnWraith.Value)
            {
                prefabNames.Add("ChebGonaz_GuardianWraith.prefab");
            }

            if (SpiritPylon.allowed.Value)
            {
                prefabNames.Add("ChebGonaz_SpiritPylonGhost.prefab");
            }

            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "chebgonazcreatures");
            AssetBundle chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                prefabNames.ForEach(prefabName =>
                {
                    Jotunn.Logger.LogInfo($"Loading {prefabName}...");
                    GameObject prefab = chebgonazAssetBundle.LoadAsset<GameObject>(prefabName);
                    if (prefab == null) { Jotunn.Logger.LogError($"prefab for {prefabName} is null!"); }

                    CreatureManager.Instance.AddCreature(new CustomCreature(prefab, true));
                }
                );
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while adding custom creatures: {ex}");
            }
            finally
            {
                chebgonazAssetBundle.Unload(false);
            }
        }

        private void AddCustomStructures()
        {
            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "chebgonazstructures");
            AssetBundle chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                Jotunn.Logger.LogInfo($"Loading {SpiritPylon.PrefabName}...");

                GameObject spiritPylonPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(SpiritPylon.PrefabName);
                if (spiritPylonPrefab == null)
                {
                    Jotunn.Logger.LogError($"AddCustomStructures: {SpiritPylon.PrefabName} is null!");
                    return;
                }
                spiritPylonPrefab.AddComponent<SpiritPylon>();

                PieceConfig spiritPylon = new PieceConfig
                {
                    PieceTable = SpiritPylon.allowed.Value ? SpiritPylon.PieceTable : "",
                    Requirements = SpiritPylon.allowed.Value ? SpiritPylon.GetRequirements() : new RequirementConfig[] { },
                    Icon = chebgonazAssetBundle.LoadAsset<Sprite>(SpiritPylon.IconName),
                };

                PieceManager.Instance.AddPiece(new CustomPiece(spiritPylonPrefab, false, spiritPylon));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while adding custom structures: {ex}");
            }
            finally
            {
                chebgonazAssetBundle.Unload(false);
            }
        }

        //private void AddButtons()
        //{
        //    wands.ForEach(wand => wand.CreateButtons());
        //}

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

        private void AddClonedItems()
        {
            //wands.ForEach(wand => KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint()));

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;

           // wands.ForEach(wand => KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint()));
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

    //[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    //class ZNet_Patches
    //{
    //    // redseiko ¡ª 12/11/2022 7:59 PM
    //    // Yes, but you need infrastructure to essentially log player logins 
    //    // with their steamId/host as well as their peer.m_uid; this 
    //    // peer.m_uid is prefixed to all ZDOs their client should spawn for 
    //    // that client's session in the form of (UID:ID) where ID goes up 
    //    // incrementally (UID resets on every full game restart).
    //    //
    //    // With that log/database you can then perform a reverse look-up for 
    //    // any given ZDO on who created it, when, etc. (Assuming you logged 
    //    // that information from the player session to begin with).
    //    //
    //    // Source: ZDOs go brr.
    //    // For where to patch, just look at ZNet.RPC_PeerInfo and you can 
    //    // see where peer.m_refPos/m_uid/m_playerName are.

    //    [HarmonyPrefix]
    //    static void retrievePlayerInfo(ref ZRpc __rpc, ref ZPackage __pkg)
    //    {
    //        //Jotunn.Logger.LogInfo("RPC_PeerInfo: " +
    //        //$"__pkg.ReadZDOID().id={__pkg.ReadZDOID().id}" +
    //        //$"__pkg.ReadZDOID().userID={__pkg.ReadZDOID().userID}" +
    //        //$"__pkg.ReadZDOID().ToString()={__pkg.ReadZDOID().ToString()}");

    //        ZNetPeer peer = ZNet.PlayerInfo//.GetPeer(__rpc);
    //        if (peer == null)
    //        {
    //            return;
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
        static void addBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops)
        {
            if (SkeletonWand.boneFragmentsDroppedAmountMin.Value != 0
                && SkeletonWand.boneFragmentsDroppedAmountMax.Value != 0)
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
                    }
                }
            }
        }
    }
    #endregion
}

