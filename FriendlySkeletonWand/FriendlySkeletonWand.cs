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
        public const string PluginVersion = "0.0.9";

        public const string CustomItemName = "FriendlySkeletonWand";
        private CustomItem friendlySkeletonWand;
        private Skills.SkillType necromancySkillType;
        private const string necromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private ConfigEntry<KeyCode> FriendlySkeletonWandSpecialConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandGamepadConfig;
        private ButtonConfig FriendlySkeletonWandSpecialButton;

        public const int boneFragmentsRequired = 3;

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        private CustomLocalization Localization;

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
            FriendlySkeletonWandSpecialConfig = Config.Bind("Client config", "FriendlySkeletonWand Special Attack", 
                KeyCode.B, new ConfigDescription("Key to unleash evil with the FriendlySkeletonWand"));
            // Also add an alternative Gamepad button for the FriendlySkeletonWand
            FriendlySkeletonWandGamepadConfig = Config.Bind("Client config", "FriendlySkeletonWand Special Attack Gamepad", 
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("Button to unleash evil with the FriendlySkeletonWand"));
        }

        private void Update()
        {
            // Since our Update function in our BepInEx mod class will load BEFORE Valheim loads,
            // we need to check that ZInput is ready to use first.
            if (ZInput.instance != null)
            {
                // Use the name of the ButtonConfig to identify the button pressed
                // without knowing what key the user bound to this button in his configuration.
                // Our button is configured to block all other input, so we just want to query
                // ZInput when our custom item is equipped.
                if (FriendlySkeletonWandSpecialButton != null && MessageHud.instance != null &&
                    Player.m_localPlayer != null)// && Player.m_localPlayer.IsItemEquiped(friendlySkeletonWand.ItemDrop.m_itemData)) //Player.m_localPlayer.m_visEquipment.m_rightItem == CustomItemName)
                {
                    if (ZInput.GetButton(FriendlySkeletonWandSpecialButton.Name) && Time.time > nextSummon)// && MessageHud.instance.que //MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        SpawnFriendlySkeleton(Player.m_localPlayer);
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

            necromancySkillType = SkillManager.Instance.AddSkill(skill);
        }

        private void AddInputs()
        {
            // Add key bindings backed by a config value
            // Also adds the alternative Config for the gamepad button
            // The HintToken is used for the custom KeyHint of the FriendlySkeletonWand
            FriendlySkeletonWandSpecialButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandSpecialAttack",
                Config = FriendlySkeletonWandSpecialConfig,        // Keyboard input
                GamepadConfig = FriendlySkeletonWandGamepadConfig, // Gamepad input
                HintToken = "$friendlyskeletonwand_create",        // Displayed KeyHint
                BlockOtherInputs = true   // Blocks all other input for this Key / Button
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
                    new ButtonConfig { Name = "Attack", HintToken = "$friendlyskeletonwand_shwing" },
                    // User our custom button defined earlier, syncs with the backing config value
                    FriendlySkeletonWandSpecialButton,
                    // Override vanilla "Mouse Wheel" text
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$friendlyskeletonwand_scroll" }
                }
            };
            KeyHintManager.Instance.AddKeyHint(KHC);
        }

        public static void SpawnFriendlySkeleton(Player player, int amount = 1)
        {
            // check player inventory for requirements
            int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

            Jotunn.Logger.LogInfo("BoneFragments in inventory: " + boneFragmentsInInventory.ToString());
            if (boneFragmentsInInventory < boneFragmentsRequired)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenoughbones");
                return;
            }

            // consume the fragments
            player.GetInventory().RemoveItem("$item_bonefragments", boneFragmentsRequired);

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
                    player.RaiseSkill(SkillManager.Instance.GetSkill(necromancySkillIdentifier).m_skill, .25f);
                }
                catch (Exception e)
                {
                    Jotunn.Logger.LogError("Failed to raise player necromancy level:" + e.ToString());
                }
            }
        }
    }
}

