// FriendlySkeletonWand
// a Valheim mod skeleton using Jötunn
// 
// File:    FriendlySkeletonWand.cs
// Project: FriendlySkeletonWand

using BepInEx;
using BepInEx.Configuration;
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
        public const string PluginVersion = "1.0.17";

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private List<Wand> wands = new List<Wand>()
        {
            new SkeletonWand(),
            new DraugrWand(),
        };
        public const string necromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private SpectralShroud spectralShroudItem = new SpectralShroud();

        private float inputDelay = 0;

        private Dictionary<string, Sprite> itemIcons = new Dictionary<string, Sprite>();

        private void Awake()
        {
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();

            AddCustomItems();
            AddCustomCreatures();
            AddCustomStructures();

            harmony.PatchAll();

            AddButtons();
            AddNecromancy();

            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;

            StartCoroutine(GuardianWraithMinion.GuardianWraithCoroutine());
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            GuardianWraithMinion.CreateConfigs(this);

            wands.ForEach(w => w.CreateConfigs(this));

            spectralShroudItem.CreateConfigs(this);

            SpiritPylon.CreateConfigs(this);
        }


        private void AddCustomItems()
        {
            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "chebgonazitems");
            AssetBundle chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                Jotunn.Logger.LogInfo($"Loading {SpectralShroud.PrefabName}...");
                GameObject spectralShroudPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(SpectralShroud.PrefabName);
                if (spectralShroudPrefab == null)
                {
                    Jotunn.Logger.LogError($"AddCustomItems: {SpectralShroud.PrefabName} is null!");
                    return;
                }
                ItemManager.Instance.AddItem(spectralShroudItem.GetCustomItemFromPrefab(spectralShroudPrefab));

                Jotunn.Logger.LogInfo($"Loading {SkeletonClub.prefabName}...");
                GameObject skeletonClubPrefab = chebgonazAssetBundle.LoadAsset<GameObject>(SkeletonClub.prefabName);
                if (skeletonClubPrefab == null)
                {
                    Jotunn.Logger.LogError($"AddCustomItems: {SkeletonClub.prefabName} is null!");
                    return;
                }
                ItemManager.Instance.AddItem(new SkeletonClub().GetCustomItemFromPrefab(skeletonClubPrefab));

                // get custom icons
                wands.ForEach(wand => itemIcons[wand.IconFile] = chebgonazAssetBundle.LoadAsset<Sprite>(wand.IconFile));
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
            List<string> prefabNames = new List<string>()
            {
                "ChebGonaz_DraugrArcher.prefab",
                "ChebGonaz_DraugrWarrior.prefab",
                "ChebGonaz_SkeletonWarrior.prefab",
                "ChebGonaz_SkeletonArcher.prefab",
                "ChebGonaz_GuardianWraith.prefab",
                "ChebGonaz_SpiritPylonGhost.prefab",
            };
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

        private void AddButtons()
        {
            wands.ForEach(wand => wand.CreateButtons());
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
        }

        private void AddClonedItems()
        {
            wands.ForEach(wand =>
            {
                ItemManager.Instance.AddItem(wand.GetCustomItem(itemIcons[wand.IconFile]));
                KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint());
            });

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
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
        }
    }

    #region HarmonyPatches

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
                if (__instance.name.Contains("SpiritPylonGhost"))
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
                //Jotunn.Logger.LogInfo($"AwakePostfix: Processing {__instance.name}");
                if (__instance.name.Contains("Wraith"))
                {
                    //Jotunn.Logger.LogInfo($"AwakePostfix: Wraith found - {__instance.name}");
                    // remove duplicate wraiths
                    if (GuardianWraithMinion.instance != null)
                    {
                        //Jotunn.Logger.LogInfo("Removing duplicate wraith...");
                        GameObject.Destroy(__instance.gameObject, 5);
                    }
                    else
                    {
                        __instance.gameObject.AddComponent<GuardianWraithMinion>();
                        GuardianWraithMinion.instance = __instance.gameObject;
                    }
                }
                else if (__instance.name.Contains("SpiritPylonGhost") && __instance.GetComponent<UndeadMinion>() == null)
                {
                    // any pylon ghost awakening we want to self-destruct after the period
                    // so add the component
                    if (__instance.GetComponent<KillAfterPeriod>() == null)
                    {
                        __instance.gameObject.AddComponent<KillAfterPeriod>();
                    }
                }
                else
                {
                    //Jotunn.Logger.LogInfo($"AwakePostfix: Skeleton or Draugr found - {__instance.name}");
                    if (__instance.GetComponent<UndeadMinion>() == null)
                    {
                        //Jotunn.Logger.LogInfo($"AwakePostfix: Adding UndeadMinion component to {__instance.name}");
                        __instance.gameObject.AddComponent<UndeadMinion>();

                        if (__instance.name.Contains("Skeleton"))
                        {
                            //todo: localplayer probably no good for multiplayer -> gotta fix
                            //SkeletonWand.AdjustSkeletonStatsToNecromancyLevel(
                            //    __instance.gameObject,
                            //    Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill));

                            // add to the silly limits if needed
                            if (SkeletonWand.maxSkeletons.Value > 0)
                            {
                                SkeletonWand.skeletons.Add(__instance.gameObject);
                            }
                        }

                        if (__instance.name.Contains("Draugr"))
                        {
                            //todo: localplayer probably no good for multiplayer -> gotta fix
                            //DraugrWand.AdjustDraugrStatsToNecromancyLevel(
                            //    __instance.gameObject,
                            //    Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill));
                            if (DraugrWand.maxDraugr.Value > 0)
                            {
                                DraugrWand.draugr.Add(__instance.gameObject);
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
}

