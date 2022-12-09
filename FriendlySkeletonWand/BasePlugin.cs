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
        public const string PluginVersion = "1.0.13";
        private readonly Harmony harmony = new Harmony(PluginGUID);

        private List<Wand> wands = new List<Wand>()
        {
            new SkeletonWand(),
            new DraugrWand(),
        };
        public const string necromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        // todo: move to GuardianWraithMinion.cs
        private ConfigEntry<int> guardianWraithLevelRequirement;
        public static ConfigEntry<float> guardianWraithTetherDistance;

        public static GameObject guardianWraith;

        private SpectralShroud spectralShroudItem = new SpectralShroud();

        private float inputDelay = 0;

        private void Awake()
        {
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();

            // custom material not working cuz unknown reasons
            //LoadCustomMaterials();
            AddCustomCreatures();
            AddCustomStructures();

            harmony.PatchAll();

            AddButtons();
            AddNecromancy();

            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;

            StartCoroutine(GuardianWraithCoroutine());
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            guardianWraithLevelRequirement = Config.Bind("Client config", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("$friendlyskeletonwand_config_guardianwraithlevelrequirement_desc"));
            guardianWraithTetherDistance = Config.Bind("Client config", "GuardianWraithTetherDistance",
                30f, new ConfigDescription("$friendlyskeletonwand_config_guardianwraithtetherdistance_desc"));

            wands.ForEach(w => w.CreateConfigs(this));

            SpiritPylon.CreateConfigs(this);
        }

        

        private void LoadCustomMaterials()
        {
            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets", "chebgonazitems");
            AssetBundle chebgonazAssetBundle = AssetUtils.LoadAssetBundle(assetBundlePath);
            try
            {
                spectralShroudItem.LoadSpectralShroudMaterial(chebgonazAssetBundle);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception caught while loading custom materials: {ex}");
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
                    PieceTable = SpiritPylon.PieceTable,
                    Requirements = SpiritPylon.GetRequirements(),
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
                ItemManager.Instance.AddItem(wand.GetCustomItem());
                KeyHintManager.Instance.AddKeyHint(wand.GetKeyHint());
            });

            ItemManager.Instance.AddItem(spectralShroudItem.GetCustomItem());

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

        IEnumerator GuardianWraithCoroutine()
        {
            while (true)
            {
                if (ZInput.instance != null && Player.m_localPlayer != null)
                {
                    Player player = Player.m_localPlayer;
                    float necromancyLevel = player.GetSkillLevel(
                        SkillManager.Instance.GetSkill(necromancySkillIdentifier).m_skill);

                    if (Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                            equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud")
                            ) != null)
                    {
                        if (necromancyLevel >= guardianWraithLevelRequirement.Value)
                        {
                            if (guardianWraith == null || guardianWraith.GetComponent<Character>().IsDead())
                            {
                                GameObject prefab = ZNetScene.instance.GetPrefab("ChebGonaz_GuardianWraith");
                                if (!prefab)
                                {
                                    Jotunn.Logger.LogError("GuardianWraithCoroutine: spawning Wraith failed");
                                }
                                else
                                {
                                    int quality = 1;
                                    if (necromancyLevel >= 70) { quality = 3; }
                                    else if (necromancyLevel >= 35) { quality = 2; }

                                    player.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_wraithmessage");
                                    guardianWraith = Instantiate(prefab,
                                        player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                                    GuardianWraithMinion guardianWraithMinion = guardianWraith.AddComponent<GuardianWraithMinion>();
                                    guardianWraithMinion.canBeCommanded = false;
                                    Character character = guardianWraith.GetComponent<Character>();
                                    character.SetLevel(quality);
                                    character.m_faction = Character.Faction.Players;
                                    guardianWraith.GetComponent<MonsterAI>().SetFollowTarget(player.gameObject);
                                }
                            }
                        }
                        else
                        {
                            // instantiate hostile wraith to punish player
                            player.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_wraithangrymessage");
                            GameObject prefab = ZNetScene.instance.GetPrefab("Wraith");
                            if (!prefab)
                            {
                                Jotunn.Logger.LogError("Wraith prefab null!");
                            }
                            else
                            {
                                Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                            }
                        }
                    }
                    else
                    {
                        if (guardianWraith != null)
                        {
                            if (guardianWraith.TryGetComponent(out Humanoid humanoid))
                            {
                                guardianWraith.GetComponent<Humanoid>().SetHealth(0);
                            } else { Destroy(guardianWraith); }
                        }
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }
    }

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
                    if (BasePlugin.guardianWraith != null)
                    {
                        //Jotunn.Logger.LogInfo("Removing duplicate wraith...");
                        GameObject.Destroy(__instance.gameObject, 5);
                    }
                    else
                    {
                        __instance.gameObject.AddComponent<GuardianWraithMinion>();
                        BasePlugin.guardianWraith = __instance.gameObject;
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

                        // add to the silly limits if needed
                        if (SkeletonWand.maxSkeletons.Value > 0 && __instance.name.Contains("Skeleton"))
                        {
                            SkeletonWand.skeletons.Add(__instance.gameObject);
                        }
                        else if (DraugrWand.maxDraugr.Value > 0 && __instance.name.Contains("Draugr"))
                        {
                            DraugrWand.draugr.Add(__instance.gameObject);
                        }
                    }
                }
            }
        }
    }
}

