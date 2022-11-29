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
    internal class FriendlySkeletonWand : BaseUnityPlugin
    {
        public const string PluginGUID = "com.chebgonaz.FriendlySkeletonWand";
        public const string PluginName = "FriendlySkeletonWand";
        public const string PluginVersion = "1.0.7";
        private readonly Harmony harmony = new Harmony(PluginGUID);

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

        private ConfigEntry<KeyCode> FriendlySkeletonWandTeleportConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandTeleportGamepadConfig;
        private ButtonConfig FriendlySkeletonWandTeleportButton;

        private ConfigEntry<KeyCode> FriendlySkeletonWandAttackTargetConfig;
        private ConfigEntry<InputManager.GamepadButton> FriendlySkeletonWandAttackTargetGamepadConfig;
        private ButtonConfig FriendlySkeletonWandAttackTargetButton;

        private ConfigEntry<int> boneFragmentsRequiredConfig;
        private ConfigEntry<float> necromancyLevelIncrease;
        private ConfigEntry<int> skeletonsPerSummon;
        private ConfigEntry<float> skeletonHealthMultiplier;
        private ConfigEntry<float> skeletonSetFollowRange;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMax;
        private ConfigEntry<int> guardianWraithLevelRequirement;
        private ConfigEntry<float> guardianWraithTetherDistance;

        private GameObject invisibleTargetObject;
        private GameObject guardianWraith;

        private float inputDelay = 0;

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("FriendlySkeletonWand has landed");

            CreateConfigValues();

            harmony.PatchAll();

            AddInputs();
            AddNecromancy();

            PrefabManager.OnVanillaPrefabsAvailable += AddClonedItems;

            StartCoroutine(GuardianWraithCoroutine());
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
                1f, new ConfigDescription("$friendlyskeletonwand_config_necromancylevelincrease_desc"));
            skeletonsPerSummon = Config.Bind("Client config", "SkeletonsPerSummon",
                1, new ConfigDescription("$friendlyskeletonwand_config_skeletonspersummon_desc"));

            skeletonHealthMultiplier = Config.Bind("Client config", "SkeletonHealthMultiplier",
                15f, new ConfigDescription("$friendlyskeletonwand_config_skeletonhealthmultiplier_desc"));
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

            boneFragmentsDroppedAmountMin = Config.Bind("Client config", "BoneFragmentsDroppedAmountMin",
                1, new ConfigDescription("$friendlyskeletonwand_config_bonefragmentsdroppedamountmin_desc"));
            boneFragmentsDroppedAmountMax = Config.Bind("Client config", "BoneFragmentsDroppedAmountMax",
                3, new ConfigDescription("$friendlyskeletonwand_config_bonefragmentsdroppedamountmax_desc"));

            FriendlySkeletonWandTeleportConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_teleport",
                KeyCode.G, new ConfigDescription("$friendlyskeletonwand_config_teleport_desc"));
            FriendlySkeletonWandTeleportGamepadConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_teleport_gamepad",
                InputManager.GamepadButton.SelectButton,
                new ConfigDescription("$friendlyskeletonwand_config_teleport_gamepad_desc"));

            FriendlySkeletonWandAttackTargetConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_attacktarget",
                KeyCode.R, new ConfigDescription("$friendlyskeletonwand_config_attacktarget_desc"));
            FriendlySkeletonWandAttackTargetGamepadConfig = Config.Bind("Client config", "$friendlyskeletonwand_config_attacktarget_gamepad",
                InputManager.GamepadButton.StartButton,
                new ConfigDescription("$friendlyskeletonwand_config_attacktarget_gamepad_desc"));

            guardianWraithLevelRequirement = Config.Bind("Client config", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("$friendlyskeletonwand_config_guardianwraithlevelrequirement_desc"));
            guardianWraithTetherDistance = Config.Bind("Client config", "GuardianWraithTetherDistance",
                15f, new ConfigDescription("$friendlyskeletonwand_config_guardianwraithtetherdistance_desc"));
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
                    else if (ZInput.GetButton(FriendlySkeletonWandTeleportButton.Name) && Time.time > inputDelay)
                    {
                        TeleportFollowingSkeletonsToPlayer(Player.m_localPlayer);
                        inputDelay = Time.time + .5f;
                    }
                    else if (ZInput.GetButton(FriendlySkeletonWandAttackTargetButton.Name) && Time.time > inputDelay)
                    {
                        MakeFollowingSkeletonsAttackTarget(Player.m_localPlayer);
                        inputDelay = Time.time + .5f;
                    }
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

            FriendlySkeletonWandTeleportButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandTeleport",
                Config = FriendlySkeletonWandTeleportConfig,
                GamepadConfig = FriendlySkeletonWandTeleportGamepadConfig,
                HintToken = "$friendlyskeletonwand_teleport",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, FriendlySkeletonWandTeleportButton);

            FriendlySkeletonWandAttackTargetButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandAttackTarget",
                Config = FriendlySkeletonWandAttackTargetConfig,
                GamepadConfig = FriendlySkeletonWandAttackTargetGamepadConfig,
                HintToken = "$friendlyskeletonwand_attacktarget",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(PluginGUID, FriendlySkeletonWandAttackTargetButton);
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
                    FriendlySkeletonWandTeleportButton,
                    FriendlySkeletonWandAttackTargetButton,
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
            // if the necromancy level is 0, the skeleton has 0 HP and instantly dies. Fix that
            // by giving it the minimum health amount possible
            if (health <= 0) { health = skeletonHealthMultiplier; }
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

                FriendlySkeletonWandMinion friendlySkeletonWandMinion = item.GetComponent<FriendlySkeletonWandMinion>();
                if (friendlySkeletonWandMinion != null && friendlySkeletonWandMinion.canBeCommanded)
                {
                    float distance = Vector3.Distance(item.transform.position, player.transform.position);
                    //Jotunn.Logger.LogInfo("Found skeleton minion at distance " + distance.ToString());
                    if (distance < radius || item.GetComponent<MonsterAI>().GetFollowTarget() == invisibleTargetObject)
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

        public void TeleportFollowingSkeletonsToPlayer(Player player)
        {
            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                if (item.GetComponent<FriendlySkeletonWandMinion>() != null
                    && item.GetComponent<MonsterAI>().GetFollowTarget() == player.gameObject)
                {
                    item.transform.position = player.transform.position;
                }
            }
        }

        public void MakeFollowingSkeletonsAttackTarget(Player player)
        {
            // make raycast between player and whatever is being looked at
            // we also need to highlight the target somehow (dont know how yet)
            // eg. change its shader
            Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                                "$friendlyskeletonwand_targetfound");
                invisibleTargetObject = Instantiate(ZNetScene.instance.GetPrefab("Stone"));
                invisibleTargetObject.transform.position = hit.transform.position;
                invisibleTargetObject.transform.position += new Vector3(0, 10, 0);
                invisibleTargetObject.transform.localScale = Vector3.one * 5;
                invisibleTargetObject.name = "FriendlySkeletonWandTarget";
            }
            else
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                                "$friendlyskeletonwand_notargetfound");
                return;
            }

            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                if (item.GetComponent<FriendlySkeletonWandMinion>() != null
                    && item.GetComponent<MonsterAI>().GetFollowTarget() == player.gameObject)
                {
                    item.GetComponent<MonsterAI>().SetFollowTarget(invisibleTargetObject);
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
                spawnedChar.AddComponent<FriendlySkeletonWandMinion>();
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
            if (FriendlySkeletonWand.boneFragmentsDroppedAmountMin.Value != 0
                && FriendlySkeletonWand.boneFragmentsDroppedAmountMax.Value != 0)
            {
                CharacterDrop.Drop bones = new CharacterDrop.Drop();
                bones.m_prefab = ZNetScene.instance.GetPrefab("BoneFragments");
                bones.m_onePerPlayer = true;
                bones.m_amountMin = FriendlySkeletonWand.boneFragmentsDroppedAmountMin.Value;
                bones.m_amountMax = FriendlySkeletonWand.boneFragmentsDroppedAmountMax.Value;
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
                && __instance.gameObject.GetComponent<FriendlySkeletonWandMinion>() == null)
            {
                __instance.gameObject.AddComponent<FriendlySkeletonWandMinion>();
            }
            else if (__instance.name.Contains("Wraith")
                && __instance.gameObject.GetComponent<GuardianWraithMinion>() == null)
            {
                __instance.gameObject.AddComponent<GuardianWraithMinion>();
            }
        }
    }

    // todo: override collision so that skeletons collide with everything except the player
}

