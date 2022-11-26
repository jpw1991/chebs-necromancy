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
using System.Collections.Generic;
using System.Linq;
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
        public const string PluginVersion = "1.0.2";
        private readonly Harmony harmony = new Harmony(PluginGUID);
        public const string friendlySkeletonName = "FriendlySkeletonWand_SkeletonMinion";

        public const string CustomItemName = "FriendlySkeletonWand";
        private CustomItem friendlySkeletonWand;
        private const string necromancySkillIdentifier = "friendlyskeletonwand_necromancy_skill";

        private ConfigEntry<KeyCode> FriendlySkeletonWandSpecialConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandGamepadConfig;
        private ButtonConfig FriendlySkeletonWandSpecialButton;

        private ConfigEntry<KeyCode> FriendlySkeletonWandFollowConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandFollowGamepadConfig;
        private ButtonConfig FriendlySkeletonWandFollowButton;

        private ConfigEntry<KeyCode> FriendlySkeletonWandWaitConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandWaitGamepadConfig;
        private ButtonConfig FriendlySkeletonWandWaitButton;

        private ConfigEntry<int> boneFragmentsRequiredConfig;
        private ConfigEntry<float> necromancyLevelIncrease;
        private ConfigEntry<int> skeletonsPerSummon;
        private ConfigEntry<float> skeletonHealthMultiplier;
        private ConfigEntry<float> skeletonSetFollowRange;

        private float inputDelay = 0;

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            harmony.PatchAll();

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

            skeletonHealthMultiplier = Config.Bind("Client config", "SkeletonHealthMultiplier",
                .25f, new ConfigDescription("$friendlyskeletonwand_config_skeletonhealthmultiplier_desc"));
            skeletonSetFollowRange = Config.Bind("Client config", "SkeletonSetFollowRange",
                10f, new ConfigDescription("$friendlyskeletonwand_config_skeletonsetfollowrange_desc"));

            FriendlySkeletonWandFollowConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_follow",
                KeyCode.F, new ConfigDescription("$friendlyskeletonwand_config_follow_desc"));
            FriendlySkeletonWandFollowGamepadConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_follow_gamepad",
                InputManager.GamepadButton.ButtonWest,
                new ConfigDescription("$friendlyskeletonwand_config_follow_gamepad_desc"));

            FriendlySkeletonWandWaitConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_wait",
                KeyCode.T, new ConfigDescription("$friendlyskeletonwand_config_wait_desc"));
            FriendlySkeletonWandWaitGamepadConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_wait_gamepad",
                InputManager.GamepadButton.ButtonEast,
                new ConfigDescription("$friendlyskeletonwand_config_wait_gamepad_desc"));

        }

        private void Update()
        {
            if (ZInput.instance != null)
            {
                if (FriendlySkeletonWandSpecialButton != null
                    && MessageHud.instance != null
                    && Player.m_localPlayer != null
                    && Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                        equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand")
                        ) != null
                    )
                {
                    if (ZInput.GetButton(FriendlySkeletonWandSpecialButton.Name) && Time.time > inputDelay)
                    {
                        SpawnFriendlySkeleton(Player.m_localPlayer,
                            boneFragmentsRequiredConfig.Value,
                            necromancyLevelIncrease.Value,
                            skeletonsPerSummon.Value
                            );
                        inputDelay = Time.time + .5f;
                    }
                    else if (ZInput.GetButton(FriendlySkeletonWandFollowButton.Name) && Time.time > inputDelay)
                    {
                        MakeNearbySkeletonsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, true);
                        inputDelay = Time.time + .5f;
                    }
                    else if (ZInput.GetButton(FriendlySkeletonWandWaitButton.Name) && Time.time > inputDelay)
                    {
                        MakeNearbySkeletonsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, false);
                        inputDelay = Time.time + .5f;
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

            FriendlySkeletonWandFollowButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandFollow",
                Config = FriendlySkeletonWandFollowConfig,
                GamepadConfig = FriendlySkeletonWandFollowGamepadConfig,
                HintToken = "$friendlyskeletonwand_follow",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, FriendlySkeletonWandFollowButton);

            FriendlySkeletonWandWaitButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandWait",
                Config = FriendlySkeletonWandWaitConfig,
                GamepadConfig = FriendlySkeletonWandWaitGamepadConfig,
                HintToken = "$friendlyskeletonwand_wait",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, FriendlySkeletonWandWaitButton);
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
            KeyHintConfig KHC = new KeyHintConfig
            {
                Item = "FriendlySkeletonWand",
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$friendlyskeletonwand_attack" },
                    // User our custom button defined earlier, syncs with the backing config value
                    FriendlySkeletonWandSpecialButton,
                    FriendlySkeletonWandFollowButton,
                    FriendlySkeletonWandWaitButton,
                }
            };
            KeyHintManager.Instance.AddKeyHint(KHC);
        }

        private void AdjustSkeletonStatsToNecromancyLevel(GameObject skeletonInstance, float necromancyLevel, float skeletonHealthMultiplier)
        {
            Character character = skeletonInstance.GetComponent<Character>();
            if (character == null)
            {
                Jotunn.Logger.LogError("FriendlySkeletonMod: error -> failed to scale skeleton to player necromancy level -> Character component is null!");
                return;
            }
            float health = necromancyLevel * skeletonHealthMultiplier;
            character.SetMaxHealth(health);
            character.SetHealth(health);

            // tried to customize the skeleton's weaponry - didn't work
            //Humanoid humanoid = skeletonInstance.GetComponent<Humanoid>();
            //if (humanoid == null)
            //{
            //    Jotunn.Logger.LogError("FriendlySkeletonMod: error -> failed to scale skeleton to player necromancy level -> Humanoid component is null!");
            //    return;
            //}
            ////ItemDrop.ItemData itemData = humanoid.GetRightItem().Clone();
            ////if (itemData == null)
            ////{
            ////    Jotunn.Logger.LogError("FriendlySkeletonMod: error -> failed to scale skeleton to player necromancy level -> failed to get weapon!");
            ////    return;
            ////}

            //humanoid.UnequipAllItems();
            //humanoid.GetInventory().RemoveAll();

            //GameObject weaponPrefab = ZNetScene.instance.GetPrefab("Club");
            //if (weaponPrefab == null)
            //{
            //    Jotunn.Logger.LogError("FriendlySkeletonMod: error -> failed to scale skeleton to player necromancy level -> failed to get weapon!");
            //    return;
            //}
            ////GameObject club = Instantiate(weaponPrefab);
            //humanoid.EquipItem(weaponPrefab.GetComponent<ItemDrop.ItemData>());
        }

        public void MakeNearbySkeletonsFollow(Player player, float radius, bool follow)
        {
            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                if (item.name.Equals(friendlySkeletonName))
                {
                    float distance = Vector3.Distance(item.transform.position, player.transform.position);
                    Jotunn.Logger.LogInfo("Found skeleton minion at distance " + distance.ToString());
                    if (distance < radius)
                    {
                        MonsterAI monsterAI = item.GetComponent<MonsterAI>();
                        if (monsterAI == null)
                        {
                            Jotunn.Logger.LogError("MakeNearbySkeletonsFollow: failed to make skeleton follow -> MonsterAI is null!");
                        }
                        else
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, 
                                follow ? "$friendlyskeletonwand_skeletonfollowing" : "$friendlyskeletonwand_skeletonwaiting");
                            monsterAI.SetFollowTarget(follow ? player.gameObject : null);
                        }
                    }
                }
            }
        }

        public void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, float necromancyLevelIncrease, int amount)
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
                Debug.Log("SpawnFriendlySkeleton: spawning Skeleton_Friendly failed");
            }

            List<GameObject> spawnedObjects = new List<GameObject>();
            for (int i = 0; i < amount; i++)
            {
                GameObject spawnedChar = Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                spawnedChar.name = friendlySkeletonName;
                Character character = spawnedChar.GetComponent<Character>();
                character.SetLevel(quality);
                AdjustSkeletonStatsToNecromancyLevel(spawnedChar, playerNecromancyLevel, skeletonHealthMultiplier.Value);
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

    [HarmonyPatch(typeof(CharacterDrop), "GenerateDropList")]
    class CharacterDrop_Patches
    {
        [HarmonyPrefix]
        static void addBonesToDropList(ref List<CharacterDrop.Drop> ___m_drops)
        {
            CharacterDrop.Drop bones = new CharacterDrop.Drop();
            bones.m_prefab = ZNetScene.instance.GetPrefab("BoneFragments");
            bones.m_onePerPlayer = true;
            bones.m_amountMin = 1;
            bones.m_amountMax = 2;
            bones.m_chance = 1f;
            ___m_drops.Add(bones);
        }
    }
}

