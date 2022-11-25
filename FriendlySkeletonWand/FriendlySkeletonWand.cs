// FriendlySkeletonWand
// a Valheim mod skeleton using Jötunn
// 
// File:    FriendlySkeletonWand.cs
// Project: FriendlySkeletonWand

using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;


namespace FriendlySkeletonWand
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class FriendlySkeletonWand : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.FriendlySkeletonWand";
        public const string PluginName = "FriendlySkeletonWand";
        public const string PluginVersion = "0.0.5";

        public const string CustomItemName = "FriendlySkeletonWand";
        private CustomItem friendlySkeletonWand;

        private ConfigEntry<KeyCode> FriendlySkeletonWandSpecialConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandGamepadConfig;
        private ButtonConfig FriendlySkeletonWandSpecialButton;

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        //public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        private CustomLocalization Localization;

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();
            AddInputs();
            AddLocalizations();

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
                    if (ZInput.GetButton(FriendlySkeletonWandSpecialButton.Name))// && MessageHud.instance.que //MessageHud.instance.m_msgQeue.Count == 0)
                    {
                        SpawnFriendlySkeleton(Player.m_localPlayer);
                        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_beevilmessage");
                    }
                }
            }
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
                HintToken = "$friendlyskeletonwand_beevil",        // Displayed KeyHint
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
            friendlySkeletonWandConfig.AddRequirement(new RequirementConfig("Stone", 1));
            //FriendlySkeletonWandConfig.AddRequirement(new RequirementConfig("Wood", 1));

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

        // Adds hardcoded localizations
        private void AddLocalizations()
        {
            // Create a custom Localization instance and add it to the Manager
            Localization = new CustomLocalization();
            LocalizationManager.Instance.AddLocalization(Localization);


            // Add translations for the custom item in AddClonedItems
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"item_friendlyskeletonwand", "Friendly Skeleton Wand"}, {"item_friendlyskeletonwand_desc", "Spawn friendly skeleton minions."},
                {"friendlyskeletonwand_shwing", "Woooosh"}, {"friendlyskeletonwand_scroll", "*scroll*"},
                {"friendlyskeletonwand_beevil", "Be evil"}, {"friendlyskeletonwand_beevilmessage", "test"},
                {"friendlyskeletonwand_effectname", "Evil"}, {"friendlyskeletonwand_effectstart", "You feel evil"},
                {"friendlyskeletonwand_effectstop", "You feel nice again"}
            });

        }

        public static void SpawnFriendlySkeleton(Player player, int amount = 1, int level = 1)
        {
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
                if (level > 1)
                    character.SetLevel(level);
                spawnedObjects.Add(spawnedChar);
            }
        }
    }
}

