using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class SkeletonWand : Wand
    {
        public ConfigEntry<KeyCode> CreateSkeletonConfig;
        public ConfigEntry<InputManager.GamepadButton> CreateSkeletonGamepadConfig;
        public ButtonConfig CreateSkeletonButton;

        public ConfigEntry<KeyCode> FollowConfig;
        public ConfigEntry<InputManager.GamepadButton> FollowGamepadConfig;
        public ButtonConfig FollowButton;

        public ConfigEntry<KeyCode> WaitConfig;
        public ConfigEntry<InputManager.GamepadButton> WaitGamepadConfig;
        public ButtonConfig WaitButton;

        public ConfigEntry<KeyCode> TeleportConfig;
        public ConfigEntry<InputManager.GamepadButton> TeleportGamepadConfig;
        public ButtonConfig TeleportButton;

        public ConfigEntry<KeyCode> AttackTargetConfig;
        public ConfigEntry<InputManager.GamepadButton> AttackTargetGamepadConfig;
        public ButtonConfig AttackTargetButton;

        public ConfigEntry<int> skeletonsPerSummon;
        public ConfigEntry<float> skeletonHealthMultiplier;
        public ConfigEntry<float> skeletonSetFollowRange;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> boneFragmentsRequiredConfig;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMax;

        private GameObject targetObject;

        public void Awake()
        {
            ItemName = "FriendlySkeletonWand";
        }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            CreateSkeletonConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_special_attack",
                KeyCode.B, new ConfigDescription("$friendlyskeletonwand_config_special_attack_desc"));
            CreateSkeletonGamepadConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_special_attack_gamepad",
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("$friendlyskeletonwand_config_special_attack_gamepad_desc"));

            skeletonHealthMultiplier = plugin.Config.Bind("Client config", "SkeletonHealthMultiplier",
                15f, new ConfigDescription("$friendlyskeletonwand_config_skeletonhealthmultiplier_desc"));

            skeletonSetFollowRange = plugin.Config.Bind("Client config", "SkeletonSetFollowRange",
                10f, new ConfigDescription("$friendlyskeletonwand_config_skeletonsetfollowrange_desc"));

            skeletonsPerSummon = plugin.Config.Bind("Client config", "SkeletonsPerSummon",
                1, new ConfigDescription("$friendlyskeletonwand_config_skeletonspersummon_desc"));

            boneFragmentsRequiredConfig = plugin.Config.Bind("Client config", "BoneFragmentsRequired",
                3, new ConfigDescription("$friendlyskeletonwand_config_bonefragmentsrequired_desc"));

            boneFragmentsDroppedAmountMin = plugin.Config.Bind("Client config", "BoneFragmentsDroppedAmountMin",
                1, new ConfigDescription("$friendlyskeletonwand_config_bonefragmentsdroppedamountmin_desc"));
            boneFragmentsDroppedAmountMax = plugin.Config.Bind("Client config", "BoneFragmentsDroppedAmountMax",
                3, new ConfigDescription("$friendlyskeletonwand_config_bonefragmentsdroppedamountmax_desc"));

            necromancyLevelIncrease = plugin.Config.Bind("Client config", "NecromancyLevelIncrease",
                1f, new ConfigDescription("$friendlyskeletonwand_config_necromancylevelincrease_desc"));

            FollowConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_follow",
                KeyCode.F, new ConfigDescription("$friendlyskeletonwand_config_follow_desc"));
            FollowGamepadConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_follow_gamepad",
                InputManager.GamepadButton.ButtonWest,
                new ConfigDescription("$friendlyskeletonwand_config_follow_gamepad_desc"));

            WaitConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_wait",
                KeyCode.T, new ConfigDescription("$friendlyskeletonwand_config_wait_desc"));
            WaitGamepadConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_wait_gamepad",
                InputManager.GamepadButton.ButtonEast,
                new ConfigDescription("$friendlyskeletonwand_config_wait_gamepad_desc"));

            TeleportConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_teleport",
                KeyCode.G, new ConfigDescription("$friendlyskeletonwand_config_teleport_desc"));
            TeleportGamepadConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_teleport_gamepad",
                InputManager.GamepadButton.SelectButton,
                new ConfigDescription("$friendlyskeletonwand_config_teleport_gamepad_desc"));

            AttackTargetConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_attacktarget",
                KeyCode.R, new ConfigDescription("$friendlyskeletonwand_config_attacktarget_desc"));
            AttackTargetGamepadConfig = plugin.Config.Bind("Client config", "$friendlyskeletonwand_config_attacktarget_gamepad",
                InputManager.GamepadButton.StartButton,
                new ConfigDescription("$friendlyskeletonwand_config_attacktarget_gamepad_desc"));

            CreateSkeletonButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandCreateSkeleton",
                Config = CreateSkeletonConfig,
                GamepadConfig = CreateSkeletonGamepadConfig,
                HintToken = "$friendlyskeletonwand_create",
                BlockOtherInputs = true
            };

            FollowButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandFollow",
                Config = FollowConfig,
                GamepadConfig = FollowGamepadConfig,
                HintToken = "$friendlyskeletonwand_follow",
                BlockOtherInputs = true
            };

            WaitButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandWait",
                Config = WaitConfig,
                GamepadConfig = WaitGamepadConfig,
                HintToken = "$friendlyskeletonwand_wait",
                BlockOtherInputs = true
            };

            TeleportButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandTeleport",
                Config = TeleportConfig,
                GamepadConfig = TeleportGamepadConfig,
                HintToken = "$friendlyskeletonwand_teleport",
                BlockOtherInputs = true
            };

            AttackTargetButton = new ButtonConfig
            {
                Name = "FriendlySkeletonWandAttackTarget",
                Config = AttackTargetConfig,
                GamepadConfig = AttackTargetGamepadConfig,
                HintToken = "$friendlyskeletonwand_attacktarget",
                BlockOtherInputs = true
            };

            buttonConfigs = new List<ButtonConfig>()
            {
                CreateSkeletonButton,
                FollowButton,
                WaitButton,
                TeleportButton,
                AttackTargetButton,
            };
        }

        public override CustomItem GetCustomItem()
        {
            // Create and add a custom item based on Club
            ItemConfig friendlySkeletonWandConfig = new ItemConfig();
            friendlySkeletonWandConfig.Name = "$item_friendlyskeletonwand";
            friendlySkeletonWandConfig.Description = "$item_friendlyskeletonwand_desc";
            friendlySkeletonWandConfig.CraftingStation = "piece_workbench";
            friendlySkeletonWandConfig.AddRequirement(new RequirementConfig("Wood", 5));

            return new CustomItem("FriendlySkeletonWand", "Club", friendlySkeletonWandConfig);
        }

        public override KeyHintConfig GetKeyHint()
        {
            return new KeyHintConfig
            {
                Item = "FriendlySkeletonWand",
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = "Attack", HintToken = "$friendlyskeletonwand_attack" },
                    CreateSkeletonButton,
                    FollowButton,
                    WaitButton,
                    TeleportButton,
                    AttackTargetButton,
                }
            };
        }

        public override bool HandleInputs()
        {
            if (MessageHud.instance != null
                    && Player.m_localPlayer != null
                    && Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                        equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand")
                        ) != null
                    )
            {
                if (CreateSkeletonButton != null && ZInput.GetButton(CreateSkeletonButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        skeletonsPerSummon.Value
                        );
                    return true;
                }
                else if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbySkeletonsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, true);
                    return true;
                }
                else if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    MakeNearbySkeletonsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, false);
                    return true;
                }
                else if (TeleportButton != null && ZInput.GetButton(TeleportButton.Name))
                {
                    TeleportFollowingSkeletonsToPlayer(Player.m_localPlayer);
                    return true;
                }
                else if (AttackTargetButton != null && ZInput.GetButton(AttackTargetButton.Name))
                {
                    MakeFollowingSkeletonsAttackTarget(Player.m_localPlayer);
                    return true;
                }
            }
            return false;
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

                UndeadMinion friendlySkeletonWandMinion = item.GetComponent<UndeadMinion>();
                if (friendlySkeletonWandMinion != null && friendlySkeletonWandMinion.canBeCommanded)
                {
                    float distance = Vector3.Distance(item.transform.position, player.transform.position);
                    //Jotunn.Logger.LogInfo("Found skeleton minion at distance " + distance.ToString());
                    if (distance < radius || item.GetComponent<MonsterAI>().GetFollowTarget() == targetObject)
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

                if (item.GetComponent<UndeadMinion>() != null
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
                targetObject = Instantiate(ZNetScene.instance.GetPrefab("Stone"));
                targetObject.transform.position = hit.transform.position;
                targetObject.transform.position += new Vector3(0, 10, 0);
                targetObject.transform.localScale = Vector3.one * 5;
                targetObject.name = "FriendlySkeletonWandTarget";
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

                if (item.GetComponent<UndeadMinion>() != null
                    && item.GetComponent<MonsterAI>().GetFollowTarget() == player.gameObject)
                {
                    item.GetComponent<MonsterAI>().SetFollowTarget(targetObject);
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
                playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);
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
                spawnedChar.AddComponent<UndeadMinion>();
                Character character = spawnedChar.GetComponent<Character>();
                character.SetLevel(quality);
                AdjustSkeletonStatsToNecromancyLevel(spawnedChar, playerNecromancyLevel, skeletonHealthMultiplier.Value);
                spawnedObjects.Add(spawnedChar);

                try
                {
                    player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill, necromancyLevelIncrease);
                }
                catch (Exception e)
                {
                    Jotunn.Logger.LogError("Failed to raise player necromancy level:" + e.ToString());
                }
            }
        }
    }
}
