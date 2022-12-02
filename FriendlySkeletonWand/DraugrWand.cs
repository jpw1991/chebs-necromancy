using BepInEx.Configuration;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class DraugrWand : Wand
    {
        public ConfigEntry<int> draugrPerSummon;
        public ConfigEntry<float> draugrHealthMultiplier;
        public ConfigEntry<float> draugrSetFollowRange;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> draugrBoneFragmentsRequiredConfig;
        public static ConfigEntry<int> draugrSinewRequiredConfig;
        public static ConfigEntry<int> sinewDroppedAmountMin;
        public static ConfigEntry<int> sinewDroppedAmountMax;

        public DraugrWand()
        {
            ItemName = "FriendlySkeletonWand_DraugrWand";
        }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            draugrHealthMultiplier = plugin.Config.Bind("Client config", "DraugrHealthMultiplier",
                30f, new ConfigDescription("$friendlyskeletonwand_config_draugrhealthmultiplier_desc"));

            draugrSetFollowRange = plugin.Config.Bind("Client config", "DraugrSetFollowRange",
                10f, new ConfigDescription("$friendlyskeletonwand_config_draugrsetfollowrange_desc"));

            draugrPerSummon = plugin.Config.Bind("Client config", "DraugrPerSummon",
                1, new ConfigDescription("$friendlyskeletonwand_config_draugrpersummon_desc"));

            draugrBoneFragmentsRequiredConfig = plugin.Config.Bind("Client config", "DraugrBoneFragmentsRequired",
                3, new ConfigDescription("$friendlyskeletonwand_config_draugrbonefragmentsrequired_desc"));
            draugrSinewRequiredConfig = plugin.Config.Bind("Client config", "DraugrSinewRequired",
                5, new ConfigDescription("$friendlyskeletonwand_config_draugrsinewrequired_desc"));

            sinewDroppedAmountMin = plugin.Config.Bind("Client config", "SinewDroppedAmountMin",
                1, new ConfigDescription("$friendlyskeletonwand_config_sinewdroppedamountmin_desc"));
            sinewDroppedAmountMax = plugin.Config.Bind("Client config", "SinewDroppedAmountMax",
                3, new ConfigDescription("$friendlyskeletonwand_config_sinewdroppedamountmax_desc"));

            necromancyLevelIncrease = plugin.Config.Bind("Client config", "DraugrNecromancyLevelIncrease",
                1.5f, new ConfigDescription("$friendlyskeletonwand_config_necromancylevelincrease_desc"));
        }

        public override void CreateButtons()
        {
            // call the base to add the basic generic buttons -> create, attack, follow, wait, etc.
            base.CreateButtons();

            // add any extra buttons
            
        }

        public override CustomItem GetCustomItem()
        {
            // Create and add a custom item based on Club
            ItemConfig draugrWandConfig = new ItemConfig();
            draugrWandConfig.Name = "$item_friendlyskeletonwand_draugrwand";
            draugrWandConfig.Description = "$item_friendlyskeletonwand_draugrwand_desc";
            draugrWandConfig.CraftingStation = "piece_workbench";
            draugrWandConfig.AddRequirement(new RequirementConfig("ElderBark", 5));
            draugrWandConfig.AddRequirement(new RequirementConfig("FineWood", 5));
            draugrWandConfig.AddRequirement(new RequirementConfig("Bronze", 5));
            draugrWandConfig.AddRequirement(new RequirementConfig("TrophyDraugr", 1));

            return new CustomItem(ItemName, "Club", draugrWandConfig);
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
                        equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_draugrwand")
                        ) != null
                    )
            {
                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnFriendlyDraugr(Player.m_localPlayer,
                        draugrBoneFragmentsRequiredConfig.Value,
                        draugrSinewRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        draugrPerSummon.Value,
                        false
                        );
                    return true;
                }
                else if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnFriendlyDraugr(Player.m_localPlayer,
                        draugrBoneFragmentsRequiredConfig.Value,
                        draugrSinewRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        draugrPerSummon.Value,
                        true
                        );
                    return true;
                }
                else if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, draugrSetFollowRange.Value, true);
                    return true;
                }
                else if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, draugrSetFollowRange.Value, false);
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

        private void AdjustDraugrStatsToNecromancyLevel(GameObject draugrInstance, float necromancyLevel, float healthMultiplier)
        {
            Character character = draugrInstance.GetComponent<Character>();
            if (character == null)
            {
                Jotunn.Logger.LogError("AdjustDraugrStatsToNecromancyLevel: error -> failed to scale minion to player necromancy level -> Character component is null!");
                return;
            }
            float health = necromancyLevel * healthMultiplier;
            // if the necromancy level is 0, the minion has 0 HP and instantly dies. Fix that
            // by giving it the minimum health amount possible
            if (health <= 0) { health = healthMultiplier; }
            character.SetMaxHealth(health);
            character.SetHealth(health);
        }

        public void SpawnFriendlyDraugr(Player player, int boneFragmentsRequired, int sinewRequired, float necromancyLevelIncrease, int amount, bool archer)
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
            string prefabName = archer ? "ChebGonaz_DraugrArcher" : "ChebGonaz_DraugrWarrior";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Jotunn.Logger.LogError($"SpawnFriendlyDraugr: spawning {prefabName} failed");
            }

            List<GameObject> spawnedObjects = new List<GameObject>();
            for (int i = 0; i < amount; i++)
            {
                Jotunn.Logger.LogInfo($"Spawning {prefabName}");
                GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                spawnedChar.AddComponent<UndeadMinion>();
                Character character = spawnedChar.GetComponent<Character>();
                character.m_faction = Character.Faction.Players;
                character.SetLevel(quality);
                AdjustDraugrStatsToNecromancyLevel(spawnedChar, playerNecromancyLevel, draugrHealthMultiplier.Value);
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
