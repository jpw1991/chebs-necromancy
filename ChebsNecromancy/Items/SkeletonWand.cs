using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;
using Random = UnityEngine.Random;

namespace ChebsNecromancy.Items
{
    internal class SkeletonWand : Wand
    {
        #region ConfigEntries

        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;
        public static ConfigEntry<string> CraftingCost;

        public static ConfigEntry<bool> SkeletonsAllowed;

        public static ConfigEntry<int> MaxSkeletons;

        public static ConfigEntry<int> BoneFragmentsRequiredConfig;
        public static ConfigEntry<float> SkeletonBaseHealth;
        public static ConfigEntry<float> SkeletonHealthMultiplier;
        public static ConfigEntry<int> SkeletonTierOneQuality;
        public static ConfigEntry<int> SkeletonTierTwoQuality;
        public static ConfigEntry<int> SkeletonTierTwoLevelReq;
        public static ConfigEntry<int> SkeletonTierThreeQuality;
        public static ConfigEntry<int> SkeletonTierThreeLevelReq;
        public static ConfigEntry<float> SkeletonSetFollowRange;

        public static ConfigEntry<int> PoisonSkeletonLevelRequirementConfig;
        public static ConfigEntry<float> PoisonSkeletonBaseHealth;
        public static ConfigEntry<int> PoisonSkeletonGuckRequiredConfig;
        public static ConfigEntry<float> SkeletonArmorValueMultiplier;
        public static ConfigEntry<int> WoodcutterSkeletonFlintRequiredConfig;
        public static ConfigEntry<int> MinerSkeletonAntlerRequiredConfig;

        #endregion

        public override string ItemName => "ChebGonaz_SkeletonWand";
        public override string PrefabName => "ChebGonaz_SkeletonWand.prefab";
        protected override string DefaultRecipe => "Wood:5,Stone:1";

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            SkeletonSetFollowRange = plugin.Config.Bind("SkeletonWand (Client)", "SkeletonCommandRange",
                20f, new ConfigDescription("The distance which nearby skeletons will hear your commands."));

            Allowed = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandAllowed",
                true, new ConfigDescription("Whether crafting a Skeleton Wand is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Skeleton Wand is available",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind("SkeletonWand (Server Synced)",
                "SkeletonWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Skeleton Wand", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingCosts",
                DefaultRecipe, new ConfigDescription(
                    "Materials needed to craft Skeleton Wand. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonsAllowed = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonsAllowed",
                true, new ConfigDescription("If false, skeletons aren't loaded at all and can't be summoned.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            BoneFragmentsRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsRequired",
                6, new ConfigDescription("The amount of Bone Fragments required to craft a skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonBaseHealth = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonBaseHealth",
                20f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonHealthMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonHealthMultiplier",
                1.25f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierOneQuality = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Skeleton minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierTwoQuality = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Skeleton minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierTwoLevelReq = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Skeleton", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierThreeQuality = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Skeleton minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierThreeLevelReq = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierThreeLevelReq",
                90, new ConfigDescription("Necromancy skill level required to summon Tier 3 Skeleton", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxSkeletons = plugin.Config.Bind("SkeletonWand (Server Synced)", "MaximumSkeletons",
                0, new ConfigDescription("The maximum amount of skeletons that can be made (0 = unlimited).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonBaseHealth = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonBaseHealth",
                100f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonLevelRequirementConfig = plugin.Config.Bind("SkeletonWand (Server Synced)",
                "PoisonSkeletonLevelRequired",
                50, new ConfigDescription("The Necromancy level needed to summon a Poison Skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonGuckRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)",
                "PoisonSkeletonGuckRequired",
                1, new ConfigDescription("The amount of Guck required to craft a Poison Skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            WoodcutterSkeletonFlintRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)",
                "WoodcutterSkeletonFlintRequired",
                1, new ConfigDescription("The amount of Flint required to craft a Woodcutter Skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MinerSkeletonAntlerRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)",
                "MinerSkeletonAntlerRequired",
                1, new ConfigDescription("The amount of HardAntler required to craft a Miner Skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonArmorValueMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)",
                "SkeletonArmorValueMultiplier",
                1f, new ConfigDescription(
                    "If you find the armor value for skeletons to be too low, you can multiply it here. By default, a skeleton wearing iron armor will have an armor value of 42 (14+14+14). A multiplier of 1.5 will cause this armor value to increase to 63.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand";
            config.Description = "$item_friendlyskeletonwand_desc";

            if (Allowed.Value)
            {
                if (string.IsNullOrEmpty(CraftingCost.Value))
                {
                    CraftingCost.Value = DefaultRecipe;
                }

                // set recipe requirements
                this.SetRecipeReqs(
                    config,
                    CraftingCost,
                    CraftingStationRequired,
                    CraftingStationLevel
                );
            }
            else
            {
                config.Enabled = false;
            }

            CustomItem customItem = new CustomItem(prefab, false, config);
            if (customItem == null)
            {
                Logger.LogError($"AddCustomItems: {PrefabName}'s CustomItem is null!");
                return null;
            }

            if (customItem.ItemPrefab == null)
            {
                Logger.LogError($"AddCustomItems: {PrefabName}'s ItemPrefab is null!");
                return null;
            }

            // make sure the set effect is applied
            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect = BasePlugin.SetEffectNecromancyArmor;

            return customItem;
        }

        public override KeyHintConfig GetKeyHint()
        {
            List<ButtonConfig> buttonConfigs = new List<ButtonConfig>();

            if (CreateMinionButton != null) buttonConfigs.Add(CreateMinionButton);
            if (CreateArcherMinionButton != null) buttonConfigs.Add(CreateArcherMinionButton);
            if (FollowButton != null) buttonConfigs.Add(FollowButton);
            if (WaitButton != null) buttonConfigs.Add(WaitButton);
            if (TeleportButton != null) buttonConfigs.Add(TeleportButton);

            return new KeyHintConfig
            {
                Item = ItemName,
                ButtonConfigs = buttonConfigs.ToArray()
            };
        }

        public override bool HandleInputs()
        {
            if (MessageHud.instance == null
                || Player.m_localPlayer == null
                || Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                    equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand")
                ) == null) return false;

            ExtraResourceConsumptionUnlocked =
                UnlockExtraResourceConsumptionButton == null
                || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

            if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
            {
                SpawnSkeleton();
                return true;
            }

            if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
            {
                SpawnRangedSkeleton();
                return true;
            }

            if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
            {
                MakeNearbyMinionsFollow(SkeletonSetFollowRange.Value, true);
                return true;
            }

            if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
            {
                if (ExtraResourceConsumptionUnlocked)
                {
                    MakeNearbyMinionsRoam(SkeletonSetFollowRange.Value);
                }
                else
                {
                    MakeNearbyMinionsFollow(SkeletonSetFollowRange.Value, false);
                }

                return true;
            }

            if (TeleportButton != null && ZInput.GetButton(TeleportButton.Name))
            {
                TeleportFollowingMinionsToPlayer();
                return true;
            }

            return false;
        }

        private SkeletonMinion.SkeletonType SpawnSkeletonArcher()
        {
            // Determine type of archer to spawn and consume resources.
            // Return None if unable to determine archer type, or if necessary resources are missing.

            var player = Player.m_localPlayer;

            // check for bones
            if (BoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < BoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughbones");
                    return SkeletonMinion.SkeletonType.None;
                }
            }

            // check for arrows
            var woodArrowsInInventory = player.GetInventory().CountItems("$item_arrow_wood");
            var bronzeArrowsInInventory = player.GetInventory().CountItems("$item_arrow_bronze");
            var ironArrowsInInventory = player.GetInventory().CountItems("$item_arrow_iron");

            if (BasePlugin.ArcherTier3ArrowsRequiredConfig.Value <= 0
                || ironArrowsInInventory >= BasePlugin.ArcherTier3ArrowsRequiredConfig.Value)
            {
                return SkeletonMinion.SkeletonType.ArcherTier3;
            }

            if (BasePlugin.ArcherTier2ArrowsRequiredConfig.Value <= 0
                || bronzeArrowsInInventory >= BasePlugin.ArcherTier2ArrowsRequiredConfig.Value)
            {
                return SkeletonMinion.SkeletonType.ArcherTier2;
            }

            if (BasePlugin.ArcherTier1ArrowsRequiredConfig.Value <= 0
                || woodArrowsInInventory >= BasePlugin.ArcherTier1ArrowsRequiredConfig.Value)
            {
                return SkeletonMinion.SkeletonType.ArcherTier1;
            }

            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenougharrows");
            return SkeletonMinion.SkeletonType.None;
        }

        private SkeletonMinion.SkeletonType SpawnSkeletonWorkerMinion()
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            Player player = Player.m_localPlayer;

            if (ExtraResourceConsumptionUnlocked
                && MinerSkeletonAntlerRequiredConfig.Value > 0)
            {
                int antlerInInventory = player.GetInventory().CountItems("$item_hardantler");
                if (antlerInInventory >= MinerSkeletonAntlerRequiredConfig.Value)
                {
                    return SkeletonMinion.SkeletonType.Miner;
                }
            }
            else if (ExtraResourceConsumptionUnlocked
                     && WoodcutterSkeletonFlintRequiredConfig.Value > 0)
            {
                int flintInInventory = player.GetInventory().CountItems("$item_flint");
                if (flintInInventory >= WoodcutterSkeletonFlintRequiredConfig.Value)
                {
                    return SkeletonMinion.SkeletonType.Woodcutter;
                }
            }

            return SkeletonMinion.SkeletonType.None;
        }
        
        private SkeletonMinion.SkeletonType SpawnPoisonSkeletonMinion(float playerNecromancyLevel,
            UndeadMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            Player player = Player.m_localPlayer;
            
            // check for bones
            if (BoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < BoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughbones");
                    return SkeletonMinion.SkeletonType.None;
                }
            }

            if (playerNecromancyLevel < PoisonSkeletonLevelRequirementConfig.Value)
                return SkeletonMinion.SkeletonType.None;

            // consume requirements
            if (ExtraResourceConsumptionUnlocked)
            {
                int guckInInventory = player.GetInventory().CountItems("$item_guck");
                if (guckInInventory < PoisonSkeletonGuckRequiredConfig.Value)
                {
                    return SkeletonMinion.SkeletonType.None;
                }
            }

            // determine quality
            return armorType switch
            {
                UndeadMinion.ArmorType.Leather => SkeletonMinion.SkeletonType.PoisonTier1,
                UndeadMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.PoisonTier2,
                UndeadMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.PoisonTier3,
                UndeadMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.PoisonTier3,
                _ => SkeletonMinion.SkeletonType.PoisonTier1
            };
        }

        private SkeletonMinion.SkeletonType SpawnSkeletonWarriorMinion(UndeadMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.
            
            Player player = Player.m_localPlayer;
            
            // check for bones
            if (BoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < BoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughbones");
                    return SkeletonMinion.SkeletonType.None;
                }
            }

            // determine quality
            return armorType switch
            {
                UndeadMinion.ArmorType.Leather => SkeletonMinion.SkeletonType.WarriorTier1,
                UndeadMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.WarriorTier2,
                UndeadMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.WarriorTier3,
                UndeadMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.WarriorTier4,
                _ => SkeletonMinion.SkeletonType.WarriorTier1
            };
        }

        private void SpawnRangedSkeleton()
        {
            if (!SkeletonsAllowed.Value) return;

            var player = Player.m_localPlayer;
            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = ExtraResourceConsumptionUnlocked
                ? UndeadMinion.DetermineArmorType()
                : UndeadMinion.ArmorType.None;

            var skeletonType = SpawnSkeletonArcher();

            if (skeletonType is SkeletonMinion.SkeletonType.None)
            {
                return;
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (MaxSkeletons.Value > 0)
            {
                // re-count the current active skeletons
                SkeletonMinion.CountActiveSkeletonMinions();
            }

            // scale according to skill
            int quality = SkeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= SkeletonTierThreeLevelReq.Value)
            {
                quality = SkeletonTierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= SkeletonTierTwoLevelReq.Value)
            {
                quality = SkeletonTierTwoQuality.Value;
            }

            SkeletonMinion.ConsumeResources(skeletonType, armorType);

            SkeletonMinion.InstantiateSkeleton(quality, playerNecromancyLevel, skeletonType, armorType);
        }

        private void SpawnSkeleton()
        {
            if (!SkeletonsAllowed.Value) return;

            var player = Player.m_localPlayer;
            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = ExtraResourceConsumptionUnlocked
                ? UndeadMinion.DetermineArmorType()
                : UndeadMinion.ArmorType.None;

            var skeletonType = SpawnSkeletonWorkerMinion();

            if (skeletonType is SkeletonMinion.SkeletonType.None)
                skeletonType = SpawnPoisonSkeletonMinion(playerNecromancyLevel, armorType);

            if (skeletonType is SkeletonMinion.SkeletonType.None)
                skeletonType = SpawnSkeletonWarriorMinion(armorType);

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (MaxSkeletons.Value > 0)
            {
                // re-count the current active skeletons
                SkeletonMinion.CountActiveSkeletonMinions();
            }

            // scale according to skill
            int quality = SkeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= SkeletonTierThreeLevelReq.Value)
            {
                quality = SkeletonTierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= SkeletonTierTwoLevelReq.Value)
            {
                quality = SkeletonTierTwoQuality.Value;
            }

            SkeletonMinion.ConsumeResources(skeletonType, armorType);

            SkeletonMinion.InstantiateSkeleton(quality, playerNecromancyLevel, skeletonType, armorType);
        }
    }
}