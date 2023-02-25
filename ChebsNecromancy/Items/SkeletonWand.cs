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
        // #region Consts
        // public const string SkeletonWarriorPrefabName = "ChebGonaz_SkeletonWarrior";
        // public const string SkeletonWarriorTier2PrefabName = "ChebGonaz_SkeletonWarriorTier2";
        // public const string SkeletonWarriorTier3PrefabName = "ChebGonaz_SkeletonWarriorTier3";
        //
        // public const string SkeletonArcherPrefabName = "ChebGonaz_SkeletonArcher";
        // public const string SkeletonArcherTier2PrefabName = "ChebGonaz_SkeletonArcherTier2";
        // public const string SkeletonArcherTier3PrefabName = "ChebGonaz_SkeletonArcherTier3";
        //
        // public const string SkeletonMagePrefabName = "ChebGonaz_SkeletonMage";
        // public const string SkeletonMageTier2PrefabName = "ChebGonaz_SkeletonMageTier2";
        // public const string SkeletonMageTier3PrefabName = "ChebGonaz_SkeletonMageTier3";
        //
        // public const string PoisonSkeletonPrefabName = "ChebGonaz_PoisonSkeleton";
        // public const string PoisonSkeleton2PrefabName = "ChebGonaz_PoisonSkeleton2";
        // public const string PoisonSkeleton3PrefabName = "ChebGonaz_PoisonSkeleton3";
        //
        // public const string SkeletonWoodcutterPrefabName = "ChebGonaz_SkeletonWoodcutter";
        //
        // public const string SkeletonMinerPrefabName = "ChebGonaz_SkeletonMiner";
        // #endregion
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

        private static ConfigEntry<float> _necromancyLevelIncrease;

        public static ConfigEntry<int> PoisonSkeletonLevelRequirementConfig;
        public static ConfigEntry<float> PoisonSkeletonBaseHealth;
        public static ConfigEntry<int> PoisonSkeletonGuckRequiredConfig;
        public static ConfigEntry<float> PoisonSkeletonNecromancyLevelIncrease;
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
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Skeleton Wand is available", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Skeleton Wand", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft Skeleton Wand. None or Blank will use Default settings.", null,
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

            _necromancyLevelIncrease = plugin.Config.Bind("SkeletonWand (Server Synced)", "NecromancyLevelIncrease",
                .75f, new ConfigDescription("How much crafting a skeleton contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxSkeletons = plugin.Config.Bind("SkeletonWand (Server Synced)", "MaximumSkeletons",
                0, new ConfigDescription("The maximum amount of skeletons that can be made (0 = unlimited).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonBaseHealth = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonBaseHealth",
                100f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonLevelRequirementConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonLevelRequired",
                50, new ConfigDescription("The Necromancy level needed to summon a Poison Skeleton.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonGuckRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonGuckRequired",
                1, new ConfigDescription("The amount of Guck required to craft a Poison Skeleton.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            WoodcutterSkeletonFlintRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "WoodcutterSkeletonFlintRequired",
                1, new ConfigDescription("The amount of Flint required to craft a Woodcutter Skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            MinerSkeletonAntlerRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "MinerSkeletonAntlerRequired",
                1, new ConfigDescription("The amount of HardAntler required to craft a Miner Skeleton.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            PoisonSkeletonNecromancyLevelIncrease = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonNecromancyLevelIncrease",
                3f, new ConfigDescription("How much crafting a Poison Skeleton contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonArmorValueMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonArmorValueMultiplier",
                1f, new ConfigDescription("If you find the armor value for skeletons to be too low, you can multiply it here. By default, a skeleton wearing iron armor will have an armor value of 42 (14+14+14). A multiplier of 1.5 will cause this armor value to increase to 63.", null,
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
            
            var skeletonType = SkeletonMinion.SkeletonType.None;
            var player = Player.m_localPlayer;

            // check for arrows
            var woodArrowsInInventory = player.GetInventory().CountItems("$item_arrow_wood");
            var bronzeArrowsInInventory = player.GetInventory().CountItems("$item_arrow_bronze");
            var ironArrowsInInventory = player.GetInventory().CountItems("$item_arrow_iron");
                
            if (BasePlugin.ArcherTier3ArrowsRequiredConfig.Value <= 0
                || BasePlugin.ArcherTier3ArrowsRequiredConfig.Value >= ironArrowsInInventory)
            {
                skeletonType = SkeletonMinion.SkeletonType.ArcherTier3;
            }
            else if (BasePlugin.ArcherTier2ArrowsRequiredConfig.Value <= 0
                     || BasePlugin.ArcherTier2ArrowsRequiredConfig.Value >= bronzeArrowsInInventory)
            {
                skeletonType = SkeletonMinion.SkeletonType.ArcherTier2;
            }
            else if (BasePlugin.ArcherTier1ArrowsRequiredConfig.Value <= 0
                     || BasePlugin.ArcherTier1ArrowsRequiredConfig.Value >= woodArrowsInInventory)
            {
                skeletonType = SkeletonMinion.SkeletonType.ArcherTier1;
            }

            if (skeletonType is SkeletonMinion.SkeletonType.None)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenougharrows");
                return skeletonType;
            }

            // check for bones
            if (BoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < BoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenoughbones");
                    return skeletonType;
                }
            }

            // consume arrows
            if (skeletonType is SkeletonMinion.SkeletonType.ArcherTier3
                && BasePlugin.ArcherTier3ArrowsRequiredConfig.Value > 0)
            {
                player.GetInventory().RemoveItem("$item_arrow_iron", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
            }
            else if (skeletonType is SkeletonMinion.SkeletonType.ArcherTier2
                     && BasePlugin.ArcherTier1ArrowsRequiredConfig.Value > 0)
            {
                player.GetInventory().RemoveItem("$item_arrow_bronze", BasePlugin.ArcherTier2ArrowsRequiredConfig.Value);
            }
            else if (skeletonType is SkeletonMinion.SkeletonType.ArcherTier1
                     && BasePlugin.ArcherTier1ArrowsRequiredConfig.Value > 0)
            {
                player.GetInventory().RemoveItem("$item_arrow_wood", BasePlugin.ArcherTier1ArrowsRequiredConfig.Value);
            }

            return skeletonType;
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

        private SkeletonMinion.SkeletonType SpawnSkeletonMageMinion(UndeadMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            Player player = Player.m_localPlayer;

            // consume requirements
            if (ExtraResourceConsumptionUnlocked
                && BasePlugin.SurtlingCoresRequiredConfig.Value > 0)
            {
                int surtlingCoresInInventory = player.GetInventory().CountItems("$item_surtlingcore");
                if (surtlingCoresInInventory < BasePlugin.SurtlingCoresRequiredConfig.Value)
                {
                    return SkeletonMinion.SkeletonType.None;;
                }
            }
            
            // determine quality

            // mages require bronze or better to be created
            return armorType switch
            {
                UndeadMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.MageTier1,
                UndeadMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.MageTier2,
                UndeadMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.MageTier3,
                _ => SkeletonMinion.SkeletonType.None
            };
        }
        
        private SkeletonMinion.SkeletonType SpawnPoisonSkeletonMinion(float playerNecromancyLevel, UndeadMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            Player player = Player.m_localPlayer;

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

        private void ConsumeResources(SkeletonMinion.SkeletonType skeletonType, UndeadMinion.ArmorType armorType)
        {
            Player player = Player.m_localPlayer;
            
            // consume bones
            player.GetInventory().RemoveItem("$item_bonefragments", BoneFragmentsRequiredConfig.Value);
            
            // consume other
            switch (skeletonType)
            {
                case SkeletonMinion.SkeletonType.Miner:
                    player.GetInventory().RemoveItem("$item_hardantler", MinerSkeletonAntlerRequiredConfig.Value);
                    break;
                case SkeletonMinion.SkeletonType.Woodcutter:
                    player.GetInventory().RemoveItem("$item_flint", WoodcutterSkeletonFlintRequiredConfig.Value);
                    break;
                
                case SkeletonMinion.SkeletonType.ArcherTier1:
                    player.GetInventory().RemoveItem("$item_arrow_wood", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                case SkeletonMinion.SkeletonType.ArcherTier2:
                    player.GetInventory().RemoveItem("$item_arrow_bronze", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                case SkeletonMinion.SkeletonType.ArcherTier3:
                    player.GetInventory().RemoveItem("$item_arrow_iron", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                
                case SkeletonMinion.SkeletonType.MageTier1:
                case SkeletonMinion.SkeletonType.MageTier2:
                case SkeletonMinion.SkeletonType.MageTier3:
                    player.GetInventory().RemoveItem("$item_surtlingcore", BasePlugin.SurtlingCoresRequiredConfig.Value);
                    break;
                
                case SkeletonMinion.SkeletonType.PoisonTier1:
                case SkeletonMinion.SkeletonType.PoisonTier2:
                case SkeletonMinion.SkeletonType.PoisonTier3:
                    player.GetInventory().RemoveItem("$item_guck", PoisonSkeletonGuckRequiredConfig.Value);
                    break;
            }
            
            // consume armor materials
            switch (armorType)
            {
                case UndeadMinion.ArmorType.Leather:
                    player.GetInventory().RemoveItem("$item_leatherscraps", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                    case UndeadMinion.ArmorType.Bronze:
                        player.GetInventory().RemoveItem("$item_bronze", BasePlugin.ArmorBronzeRequiredConfig.Value);
                        break;
                    case UndeadMinion.ArmorType.Iron:
                        player.GetInventory().RemoveItem("$item_iron", BasePlugin.ArmorIronRequiredConfig.Value);
                        break;
                    case UndeadMinion.ArmorType.BlackMetal:
                        player.GetInventory().RemoveItem("$item_blackmetal", BasePlugin.ArmorBlackIronRequiredConfig.Value);
                        break;
            }
        }
        
        private void SpawnRangedSkeleton()
        {
            if (!SkeletonsAllowed.Value) return;
            
            var player = Player.m_localPlayer;
            var playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = UndeadMinion.DetermineArmorType();
            
            var skeletonType = SpawnSkeletonArcher();
            
            if (skeletonType is SkeletonMinion.SkeletonType.None)
                skeletonType = SpawnSkeletonMageMinion(armorType);

            if (skeletonType is SkeletonMinion.SkeletonType.None)
            {
                return;
            }
            
            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (MaxSkeletons.Value > 0)
            {
                // re-count the current active skeletons
                CountActiveSkeletonMinions();
            }

            // scale according to skill
            int quality = SkeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= SkeletonTierThreeLevelReq.Value) { quality = SkeletonTierThreeQuality.Value; }
            else if (playerNecromancyLevel >= SkeletonTierTwoLevelReq.Value) { quality = SkeletonTierTwoQuality.Value; }
            
            ConsumeResources(skeletonType, armorType);
            
            InstantiateSkeleton(quality, playerNecromancyLevel, skeletonType, armorType);
        }

        private void SpawnSkeleton()
        {
            if (!SkeletonsAllowed.Value) return;
            
            var player = Player.m_localPlayer;
            var playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = UndeadMinion.DetermineArmorType();

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
                CountActiveSkeletonMinions();
            }

            // scale according to skill
            int quality = SkeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= SkeletonTierThreeLevelReq.Value) { quality = SkeletonTierThreeQuality.Value; }
            else if (playerNecromancyLevel >= SkeletonTierTwoLevelReq.Value) { quality = SkeletonTierTwoQuality.Value; }
            
            ConsumeResources(skeletonType, armorType);
            
            InstantiateSkeleton(quality, playerNecromancyLevel, skeletonType, armorType);
        }

        private void InstantiateSkeleton(int quality, float playerNecromancyLevel, SkeletonMinion.SkeletonType skeletonType, UndeadMinion.ArmorType armorType)
        {
            Player player = Player.m_localPlayer;
            string prefabName = InternalName.GetName(skeletonType);
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"InstantiateSkeleton: spawning {prefabName} failed");
                return;
            }

            var transform = player.transform;
            GameObject spawnedChar = GameObject.Instantiate(prefab, transform.position + transform.forward * 2f + Vector3.up, Quaternion.identity);
            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            spawnedChar.AddComponent<FreshMinion>();

            SkeletonMinion minion = skeletonType switch
            {
                SkeletonMinion.SkeletonType.PoisonTier1 
                    or SkeletonMinion.SkeletonType.PoisonTier2 
                    or SkeletonMinion.SkeletonType.PoisonTier3 => spawnedChar.AddComponent<PoisonSkeletonMinion>(),
                SkeletonMinion.SkeletonType.Woodcutter => spawnedChar.AddComponent<SkeletonWoodcutterMinion>(),
                SkeletonMinion.SkeletonType.Miner => spawnedChar.AddComponent<SkeletonMinerMinion>(),
                _ => spawnedChar.AddComponent<SkeletonMinion>()
            };
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, skeletonType, armorType);
            minion.ScaleStats(playerNecromancyLevel); 

            if (skeletonType != SkeletonMinion.SkeletonType.Woodcutter
                && skeletonType != SkeletonMinion.SkeletonType.Miner)
            {
                if (FollowByDefault.Value)
                {
                    minion.Follow(player.gameObject);
                }
                else
                {
                    minion.Wait(player.transform.position);
                }                
            }

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill, _necromancyLevelIncrease.Value);

            minion.UndeadMinionMaster = player.GetPlayerName();

            // handle refunding of resources on death
            if (SkeletonMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing)
            {
                CharacterDrop characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (SkeletonMinion.DropOnDeath.Value == UndeadMinion.DropType.Everything
                    && BoneFragmentsRequiredConfig.Value > 0)
                {
                    // bones
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                        m_onePerPlayer = true,
                        m_amountMin = BoneFragmentsRequiredConfig.Value,
                        m_amountMax = BoneFragmentsRequiredConfig.Value,
                        m_chance = 1f
                    });
                }

                if (skeletonType == SkeletonMinion.SkeletonType.Miner)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("HardAntler"),
                        m_onePerPlayer = true,
                        m_amountMin = MinerSkeletonAntlerRequiredConfig.Value,
                        m_amountMax = MinerSkeletonAntlerRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                
                if (skeletonType == SkeletonMinion.SkeletonType.Woodcutter)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Flint"),
                        m_onePerPlayer = true,
                        m_amountMin = WoodcutterSkeletonFlintRequiredConfig.Value,
                        m_amountMax = WoodcutterSkeletonFlintRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                if (skeletonType is SkeletonMinion.SkeletonType.MageTier1 
                    or SkeletonMinion.SkeletonType.MageTier2 
                    or SkeletonMinion.SkeletonType.MageTier3)
                {
                    // surtling core
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("SurtlingCore"),
                        m_onePerPlayer = true,
                        m_amountMin = BasePlugin.SurtlingCoresRequiredConfig.Value,
                        m_amountMax = BasePlugin.SurtlingCoresRequiredConfig.Value,
                        m_chance = 1f
                    });
                }

                switch (armorType)
                {
                    case UndeadMinion.ArmorType.Leather:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            // flip a coin for deer or scraps
                            m_prefab = Random.value > .5f 
                                ? ZNetScene.instance.GetPrefab("DeerHide")
                                : ZNetScene.instance.GetPrefab("LeatherScraps") 
                            ,
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorLeatherScrapsRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorLeatherScrapsRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                    case UndeadMinion.ArmorType.Bronze:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("Bronze"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorBronzeRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorBronzeRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                    case UndeadMinion.ArmorType.Iron:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("Iron"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorIronRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorIronRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                    case UndeadMinion.ArmorType.BlackMetal:
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("BlackMetal"),
                            m_onePerPlayer = true,
                            m_amountMin = BasePlugin.ArmorBlackIronRequiredConfig.Value,
                            m_amountMax = BasePlugin.ArmorBlackIronRequiredConfig.Value,
                            m_chance = 1f
                        });
                        break;
                }

                // the component won't be remembered by the game on logout because
                // only what is on the prefab is remembered. Even changes to the prefab
                // aren't remembered. So we must write what we're dropping into
                // the ZDO as well and then read & restore this on Awake
                minion.RecordDrops(characterDrop);
            }
        }

        public void CountActiveSkeletonMinions()
        {
            //todo: this function is poorly designed. Return value is not
            // important to its function; function has side effects, etc.
            // Refactor sometime

            int result = 0;
            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            List<Tuple<int, Character>> minionsFound = new List<Tuple<int, Character>>();

            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                SkeletonMinion minion = item.GetComponent<SkeletonMinion>();
                if (minion != null
                    && minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
                {
                    minionsFound.Add(new Tuple<int, Character>(minion.createdOrder, item));
                }
            }

            // reverse so that we get newest first, oldest last. This means
            // when we kill off surplus, the oldest things are getting killed
            // not the newest things
            minionsFound = minionsFound.OrderByDescending((arg) => arg.Item1).ToList();

            foreach (var t in minionsFound)
            {
                // kill off surplus
                if (result >= MaxSkeletons.Value - 1)
                {
                    Tuple<int, Character> tuple = t;
                    tuple.Item2.SetHealth(0);
                    continue;
                }

                result++;
            }
        }
    }
}
