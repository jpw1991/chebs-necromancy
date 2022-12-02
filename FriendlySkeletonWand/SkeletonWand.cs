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
        public ConfigEntry<int> skeletonsPerSummon;
        public ConfigEntry<float> skeletonHealthMultiplier;
        public ConfigEntry<float> skeletonSetFollowRange;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> boneFragmentsRequiredConfig;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMax;

        public SkeletonWand()
        {
            ItemName = "FriendlySkeletonWand";
        }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

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
        }

        public override void CreateButtons()
        {
            // call the base to add the basic generic buttons -> create, attack, follow, wait, etc.
            base.CreateButtons();

            // add any extra buttons
        }

        public override CustomItem GetCustomItem()
        {
            ItemConfig friendlySkeletonWandConfig = new ItemConfig();
            friendlySkeletonWandConfig.Name = "$item_friendlyskeletonwand";
            friendlySkeletonWandConfig.Description = "$item_friendlyskeletonwand_desc";
            friendlySkeletonWandConfig.CraftingStation = "piece_workbench";
            friendlySkeletonWandConfig.AddRequirement(new RequirementConfig("Wood", 5));

            return new CustomItem(ItemName, "Club", friendlySkeletonWandConfig);

        }

        public override KeyHintConfig GetKeyHint()
        {
            return new KeyHintConfig
            {
                Item = ItemName,
                ButtonConfigs = new[]
                {
                    //new ButtonConfig { Name = "Attack", HintToken = "$friendlyskeletonwand_attack" },
                    CreateMinionButton,
                    CreateArcherMinionButton,
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
                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        skeletonsPerSummon.Value,
                        false
                        );
                    return true;
                }
                else if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        skeletonsPerSummon.Value,
                        true
                        );
                    return true;
                }
                else if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, true);
                    return true;
                }
                else if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, false);
                    return true;
                }
                else if (TeleportButton != null && ZInput.GetButton(TeleportButton.Name))
                {
                    TeleportFollowingMinionsToPlayer(Player.m_localPlayer);
                    return true;
                }
                else if (AttackTargetButton != null && ZInput.GetButton(AttackTargetButton.Name))
                {
                    MakeFollowingMinionsAttackTarget(Player.m_localPlayer);
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

        public void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, float necromancyLevelIncrease, int amount, bool archer)
        {
            // check player inventory for requirements
            if (boneFragmentsRequired > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                Jotunn.Logger.LogInfo($"BoneFragments in inventory: {boneFragmentsInInventory}");
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
                Jotunn.Logger.LogError($"Failed to get player necromancy level: {e}");
            }
            Jotunn.Logger.LogInfo($"Player necromancy level: {playerNecromancyLevel}");

            int quality = 1;
            if (playerNecromancyLevel >= 70) { quality = 3; }
            else if (playerNecromancyLevel >= 35) { quality = 2; }

            // go on to spawn skeleton
            string prefabName = archer ? "ChebGonaz_SkeletonArcher" : "ChebGonaz_SkeletonWarrior";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Jotunn.Logger.LogError($"SpawnFriendlySkeleton: spawning {prefabName} failed");
            }

            List<GameObject> spawnedObjects = new List<GameObject>();
            for (int i = 0; i < amount; i++)
            {
                GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
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
                    Jotunn.Logger.LogError($"Failed to raise player necromancy level: {e}");
                }
            }
        }
    }
}
