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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ClutterSystem;


namespace FriendlySkeletonWand
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class BasePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.chebgonaz.FriendlySkeletonWand";
        public const string PluginName = "FriendlySkeletonWand";
        public const string PluginVersion = "1.0.8";
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

        private float inputDelay = 0;

        private void Awake()
        {
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();

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
                15f, new ConfigDescription("$friendlyskeletonwand_config_guardianwraithtetherdistance_desc"));

            wands.ForEach(w => w.CreateConfigs(this));
        }

        private void AddButtons()
        {
            wands.ForEach(wand => wand.CreateButtons());
        }

        private void AddNecromancy()
        {
            SkillConfig skill = new SkillConfig();
            skill.Name = "$friendlyskeletonwand_necromancy";
            skill.Description = "$friendlyskeletonwand_necromancy_desc";
            skill.IconPath = "FriendlySkeletonWand/Assets/necromancy_icon.png";
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
                if (ZInput.instance != null)
                {
                    Player player = Player.m_localPlayer;
                    float necromancyLevel = player.GetSkillLevel(
                        SkillManager.Instance.GetSkill(necromancySkillIdentifier).m_skill);

                    if (necromancyLevel >= guardianWraithLevelRequirement.Value && (guardianWraith == null || guardianWraith.GetComponent<Character>().IsDead()))
                    {
                        GameObject prefab = ZNetScene.instance.GetPrefab("Wraith");
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
                yield return new WaitForSeconds(30);
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

    [HarmonyPatch(typeof(MonsterAI))]
    static class FriendlySkeletonPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MonsterAI.Awake))]
        static void AwakePostfix(ref Character __instance)
        {
            if (__instance.name.Contains("Skeleton_Friendly") 
                && __instance.gameObject.GetComponent<UndeadMinion>() == null)
            {
                __instance.gameObject.AddComponent<UndeadMinion>();
            }
            else if (__instance.name.Contains("Wraith"))
            {
                GameObject.Destroy(__instance);
                //
                //__instance.gameObject.AddComponent<GuardianWraithMinion>();
                //BasePlugin.guardianWraith = __instance.gameObject;
            }
        }
    }
}

