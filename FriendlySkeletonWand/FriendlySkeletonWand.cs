// FriendlySkeletonWand
// a Valheim mod skeleton using Jötunn
// 
// File:    FriendlySkeletonWand.cs
// Project: FriendlySkeletonWand

using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace FriendlySkeletonWand
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class FriendlySkeletonWand : BaseUnityPlugin
    {
        public const string PluginGUID = "com.chebgonaz.FriendlySkeletonWand";
        public const string PluginName = "FriendlySkeletonWand";
        public const string PluginVersion = "1.0.0";

        public const string CustomItemName = "FriendlySkeletonWand";
        private CustomItem friendlySkeletonWand;
        private const string necromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private ConfigEntry<KeyCode> FriendlySkeletonWandSpecialConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandGamepadConfig;
        private ButtonConfig FriendlySkeletonWandSpecialButton;

        private ConfigEntry<int> boneFragmentsRequiredConfig;
        private ConfigEntry<float> necromancyLevelIncrease;
        private ConfigEntry<int> skeletonsPerSummon;

        private float nextSummon = 0;

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();
            AddInputs();
            AddNecromancy();

            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;
        }

        private void CreateConfigValues()
        {
            Config.SaveOnConfigSet = true;

            // Add a client side custom input key for the FriendlySkeletonWand
            FriendlySkeletonWandSpecialConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_special_attack", 
                KeyCode.B, new ConfigDescription("$friendlyskeletonwand_config_special_attack_desc"));
            // Also add an alternative Gamepad button for the FriendlySkeletonWand
            FriendlySkeletonWandGamepadConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_special_attack_gamepad", 
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("$friendlyskeletonwand_config_special_attack_gamepad_desc"));

            // add configs for values that user can set in-game with F1
            boneFragmentsRequiredConfig = Config.Bind("Client config", "BoneFragmentsRequired",
                3, new ConfigDescription("$friendlyskeletonwand_config_bonefragmentsrequired_desc"));
            necromancyLevelIncrease = Config.Bind("Client config", "NecromancyLevelIncrease",
                .25f, new ConfigDescription("$friendlyskeletonwand_config_necromancylevelincrease_desc"));
            skeletonsPerSummon = Config.Bind("Client config", "SkeletonsPerSummon",
                1, new ConfigDescription("$friendlyskeletonwand_config_skeletonspersummon_desc"));

        }

        private void Update()
        {
            if (ZInput.instance != null)
            {
                if (FriendlySkeletonWandSpecialButton != null
                    && MessageHud.instance != null
                    && Player.m_localPlayer != null)
                {
                    if (ZInput.GetButton(FriendlySkeletonWandSpecialButton.Name) && Time.time > nextSummon)
                    {
                        SpawnFriendlySkeleton(Player.m_localPlayer,
                            boneFragmentsRequiredConfig.Value,
                            necromancyLevelIncrease.Value,
                            skeletonsPerSummon.Value
                            );
                        nextSummon = Time.time + .5f;
                    }
                }
            }
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

        private void AddInputs()
        {
            FriendlySkeletonWandSpecialButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandSpecialAttack",
                Config = FriendlySkeletonWandSpecialConfig,
                GamepadConfig = FriendlySkeletonWandGamepadConfig,
                HintToken = "$friendlyskeletonwand_create",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, FriendlySkeletonWandSpecialButton);
        }

        private void AddClonedItems()
        {
            // Create and add a custom item based on Club
            ItemConfig friendlySkeletonWandConfig = new ItemConfig();
            friendlySkeletonWandConfig.Name = "$item_friendlyskeletonwand";
            friendlySkeletonWandConfig.Description = "$item_friendlyskeletonwand_desc";
            friendlySkeletonWandConfig.CraftingStation = "piece_workbench";
            friendlySkeletonWandConfig.AddRequirement(new RequirementConfig("Wood", 5));

            friendlySkeletonWand = new CustomItem("FriendlySkeletonWand", "Club", friendlySkeletonWandConfig);
            ItemManager.Instance.AddItem(friendlySkeletonWand);

            KeyHintsFriendlySkeletonWand();

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
        }

        private void KeyHintsFriendlySkeletonWand()
        {
            // Create custom KeyHints for the item
            KeyHintConfig KHC = new KeyHintConfig
            {
                Item = "FriendlySkeletonWand",
                ButtonConfigs = new[]
                {
                    // Override vanilla "Attack" key text
                    new ButtonConfig { Name = "Attack", HintToken = "$friendlyskeletonwand_attack" },
                    // User our custom button defined earlier, syncs with the backing config value
                    FriendlySkeletonWandSpecialButton,
                    // Override vanilla "Mouse Wheel" text
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$friendlyskeletonwand_scroll" }
                }
            };
            KeyHintManager.Instance.AddKeyHint(KHC);
        }

        public static void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, float necromancyLevelIncrease, int amount)
        {
            // check player inventory for requirements
            if (boneFragmentsRequired > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                Jotunn.Logger.LogInfo("BoneFragments in inventory: " + boneFragmentsInInventory.ToString());
                if (boneFragmentsInInventory < boneFragmentsRequired)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenoughbones");
                    return;
                }

                // consume the fragments
                player.GetInventory().RemoveItem("$item_bonefragments", boneFragmentsRequired);
            }

            // scale according to skill
            float playerNecromancyLevel = 1;
            try
            {
                playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(necromancySkillIdentifier).m_skill);
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError("Failed to get player necromancy level:" + e.ToString());
            }
            Jotunn.Logger.LogInfo("Player necromancy level:" + playerNecromancyLevel.ToString());

            int quality = 1;
            if (playerNecromancyLevel >= 70) { quality = 3; }
            else if (playerNecromancyLevel >= 35) { quality = 2; }

            // go on to spawn skeleton
            GameObject prefab = ZNetScene.instance.GetPrefab("Skeleton_Friendly");
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Skeleton_Friendly does not exist");
                Debug.Log("FriendlySkeletonWand: spawning Skeleton_Friendly failed");
            }

            List<GameObject> spawnedObjects = new List<GameObject>();
            for (int i = 0; i < amount; i++)
            {
                GameObject spawnedChar = Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                Character character = spawnedChar.GetComponent<Character>();
                character.SetLevel(quality);
                spawnedObjects.Add(spawnedChar);

                try
                {
                    player.RaiseSkill(SkillManager.Instance.GetSkill(necromancySkillIdentifier).m_skill, necromancyLevelIncrease);
                }
                catch (Exception e)
                {
                    Jotunn.Logger.LogError("Failed to raise player necromancy level:" + e.ToString());
                }
            }
        }
    }
}

