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

namespace ChebsNecromancy.Items
{
    internal class SkeletonWand : Wand
    {
        #region Consts
        public const string SkeletonWarriorPrefabName = "ChebGonaz_SkeletonWarrior";
        public const string SkeletonWarriorTier2PrefabName = "ChebGonaz_SkeletonWarriorTier2";
        public const string SkeletonWarriorTier3PrefabName = "ChebGonaz_SkeletonWarriorTier3";

        public const string SkeletonArcherPrefabName = "ChebGonaz_SkeletonArcher";
        public const string SkeletonArcherTier2PrefabName = "ChebGonaz_SkeletonArcherTier2";
        public const string SkeletonArcherTier3PrefabName = "ChebGonaz_SkeletonArcherTier3";

        public const string SkeletonMagePrefabName = "ChebGonaz_SkeletonMage";
        public const string SkeletonMageTier2PrefabName = "ChebGonaz_SkeletonMageTier2";
        public const string SkeletonMageTier3PrefabName = "ChebGonaz_SkeletonMageTier3";

        public const string PoisonSkeletonPrefabName = "ChebGonaz_PoisonSkeleton";
        public const string PoisonSkeleton2PrefabName = "ChebGonaz_PoisonSkeleton2";
        public const string PoisonSkeleton3PrefabName = "ChebGonaz_PoisonSkeleton3";

        public const string SkeletonWoodcutterPrefabName = "ChebGonaz_SkeletonWoodcutter";
        #endregion
        #region ConfigEntries
        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;
        public static ConfigEntry<string> CraftingCost;

        public static ConfigEntry<bool> SkeletonsAllowed;

        public static ConfigEntry<int> MaxSkeletons;

        public static ConfigEntry<float> SkeletonBaseHealth;
        public static ConfigEntry<float> SkeletonHealthMultiplier;
        public static ConfigEntry<int> SkeletonTierOneQuality;
        public static ConfigEntry<int> SkeletonTierTwoQuality;
        public static ConfigEntry<int> SkeletonTierTwoLevelReq;
        public static ConfigEntry<int> SkeletonTierThreeQuality;
        public static ConfigEntry<int> SkeletonTierThreeLevelReq;
        public static ConfigEntry<float> SkeletonSetFollowRange;

        private static ConfigEntry<float> _necromancyLevelIncrease;

        public static ConfigEntry<int> BoneFragmentsRequiredConfig;
        public static ConfigEntry<int> BoneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> BoneFragmentsDroppedAmountMax;
        public static ConfigEntry<float> BoneFragmentsDroppedChance;

        public static ConfigEntry<int> ArmorLeatherScrapsRequiredConfig;
        public static ConfigEntry<int> ArmorBronzeRequiredConfig;
        public static ConfigEntry<int> ArmorIronRequiredConfig;
        public static ConfigEntry<int> ArmorBlackIronRequiredConfig;
        public static ConfigEntry<int> SurtlingCoresRequiredConfig;

        public static ConfigEntry<int> PoisonSkeletonLevelRequirementConfig;
        public static ConfigEntry<float> PoisonSkeletonBaseHealth;
        public static ConfigEntry<int> PoisonSkeletonGuckRequiredConfig;
        public static ConfigEntry<float> PoisonSkeletonNecromancyLevelIncrease;
        public static ConfigEntry<float> SkeletonArmorValueMultiplier;
        public static ConfigEntry<int> WoodcutterSkeletonFlintRequiredConfig;

        public static ConfigEntry<int> ArcherArrowsRequiredConfig;

        public static ConfigEntry<bool> DurabilityDamage;
        public static ConfigEntry<float> DurabilityDamageWarrior;
        public static ConfigEntry<float> DurabilityDamageMage;
        public static ConfigEntry<float> DurabilityDamageArcher;
        public static ConfigEntry<float> DurabilityDamagePoison;
        public static ConfigEntry<float> DurabilityDamageLeather;
        public static ConfigEntry<float> DurabilityDamageBronze;
        public static ConfigEntry<float> DurabilityDamageIron;
        public static ConfigEntry<float> DurabilityDamageBlackIron;
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

            SkeletonBaseHealth = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonBaseHealth",
                20f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonHealthMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonHealthMultiplier",
                2.5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
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
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Skeleton", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            BoneFragmentsRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsRequired",
                3, new ConfigDescription("The amount of Bone Fragments required to craft a skeleton.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            BoneFragmentsDroppedAmountMin = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsDroppedAmountMin",
                1, new ConfigDescription("The minimum amount of bones dropped by creatures.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            BoneFragmentsDroppedAmountMax = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsDroppedAmountMax",
                3, new ConfigDescription("The maximum amount of bones dropped by creatures.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            BoneFragmentsDroppedChance = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsDroppedChance",
                1f, new ConfigDescription("The chance of bones dropped by creatures (0 = 0%, 1 = 100%).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            _necromancyLevelIncrease = plugin.Config.Bind("SkeletonWand (Server Synced)", "NecromancyLevelIncrease",
                1f, new ConfigDescription("How much crafting a skeleton contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxSkeletons = plugin.Config.Bind("SkeletonWand (Server Synced)", "MaximumSkeletons",
                0, new ConfigDescription("The maximum amount of skeletons that can be made (0 = unlimited).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorLeatherScrapsRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonLeatherScrapsRequired",
                5, new ConfigDescription("The amount of LeatherScraps required to craft a skeleton in leather armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBronzeRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonBronzeRequired",
                1, new ConfigDescription("The amount of Bronze required to craft a skeleton in bronze armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorIronRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonIronRequired",
                1, new ConfigDescription("The amount of Iron required to craft a skeleton in iron armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SurtlingCoresRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonMageSurtlingCoresRequired",
                1, new ConfigDescription("The amount of surtling cores required to craft a skeleton mage.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            ArmorBlackIronRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonBlackIronRequired",
                1, new ConfigDescription("The amount of Black Metal required to craft a skeleton in black iron armor.", null,
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

            PoisonSkeletonNecromancyLevelIncrease = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonNecromancyLevelIncrease",
                3f, new ConfigDescription("How much crafting a Poison Skeleton contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonArmorValueMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonArmorValueMultiplier",
                1f, new ConfigDescription("If you find the armor value for skeletons to be too low, you can multiply it here. By default, a skeleton wearing iron armor will have an armor value of 42 (14+14+14). A multiplier of 1.5 will cause this armor value to increase to 63.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamage = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamage",
                true, new ConfigDescription("Whether using a Skeleton Wand damages its durability.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageWarrior = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageWarrior",
                1f, new ConfigDescription("How much creating a warrior damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageArcher = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageArcher",
                3f, new ConfigDescription("How much creating an archer damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageMage = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageMage",
                5f, new ConfigDescription("How much creating a mage damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamagePoison = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamagePoison",
                5f, new ConfigDescription("How much creating a poison skeleton damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageLeather = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageLeather",
                1f, new ConfigDescription("How much armoring the minion in leather damages the wand (value is added on top of damage from minion type).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageBronze = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageBronze",
                1f, new ConfigDescription("How much armoring the minion in bronze damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageIron = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageIron",
                1f, new ConfigDescription("How much armoring the minion in iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DurabilityDamageBlackIron = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageBlackIron",
                1f, new ConfigDescription("How much armoring the minion in black iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            ArcherArrowsRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArcherArrowsRequired",
                20, new ConfigDescription("The amount of wood arrows required to craft a skeleton archer.", null,
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
                SpawnFriendlySkeleton(Player.m_localPlayer,
                    BoneFragmentsRequiredConfig.Value,
                    false);
                return true;
            }

            if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
            {
                SpawnFriendlySkeleton(Player.m_localPlayer,
                    BoneFragmentsRequiredConfig.Value,
                    true);
                return true;
            }
            if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
            {
                MakeNearbyMinionsFollow(Player.m_localPlayer, SkeletonSetFollowRange.Value, true);
                return true;
            }
            if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
            {
                if (ExtraResourceConsumptionUnlocked)
                {
                    MakeNearbyMinionsRoam(Player.m_localPlayer, SkeletonSetFollowRange.Value);
                }
                else
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, SkeletonSetFollowRange.Value, false);   
                }
                return true;
            }
            if (TeleportButton != null && ZInput.GetButton(TeleportButton.Name))
            {
                TeleportFollowingMinionsToPlayer(Player.m_localPlayer);
                return true;
            }

            return false;
        }

        private void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, bool archer)
        {
            if (!SkeletonsAllowed.Value) return;
            
            if (archer && ArcherArrowsRequiredConfig.Value > 0)
            {
                var arrowsInInventory = player.GetInventory().CountItems("$item_arrow_wood");

                if (arrowsInInventory < ArcherArrowsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenougharrows");
                    return;
                }
            }

            // check player inventory for requirements
            if (boneFragmentsRequired > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < boneFragmentsRequired)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenoughbones");
                    return;
                }
            }
            
            if (archer && ArcherArrowsRequiredConfig.Value > 0) player.GetInventory().RemoveItem("$item_arrow_wood", ArcherArrowsRequiredConfig.Value);
            player.GetInventory().RemoveItem("$item_bonefragments", boneFragmentsRequired);
            
            bool createWoodcutter = false;
            if (ExtraResourceConsumptionUnlocked
                && !archer
                && WoodcutterSkeletonFlintRequiredConfig.Value > 0)
            {
                int flintInInventory = player.GetInventory().CountItems("$item_flint");
                if (flintInInventory >= WoodcutterSkeletonFlintRequiredConfig.Value)
                {
                    createWoodcutter = true;
                    player.GetInventory().RemoveItem("$item_flint", WoodcutterSkeletonFlintRequiredConfig.Value);
                }
            }

            bool createArmoredLeather = false;
            if (ExtraResourceConsumptionUnlocked
                && !createWoodcutter
                && ArmorLeatherScrapsRequiredConfig.Value > 0)
            {
                int leatherScrapsInInventory = player.GetInventory().CountItems("$item_leatherscraps");
                if (leatherScrapsInInventory >= ArmorLeatherScrapsRequiredConfig.Value)
                {
                    createArmoredLeather = true;
                    player.GetInventory().RemoveItem("$item_leatherscraps", ArmorLeatherScrapsRequiredConfig.Value);
                }
                else
                {
                    // no leather scraps? Try some deer hide
                    int deerHideInInventory = player.GetInventory().CountItems("$item_deerhide");
                    if (deerHideInInventory >= ArmorLeatherScrapsRequiredConfig.Value)
                    {
                        createArmoredLeather = true;
                        player.GetInventory().RemoveItem("$item_deerhide", ArmorLeatherScrapsRequiredConfig.Value);
                    }
                }
            }

            bool createArmoredBronze = false;
            if (ExtraResourceConsumptionUnlocked 
                && !createWoodcutter
                && !createArmoredLeather 
                && ArmorBronzeRequiredConfig.Value > 0)
            {
                int bronzeInInventory = player.GetInventory().CountItems("$item_bronze");
                if (bronzeInInventory >= ArmorBronzeRequiredConfig.Value)
                {
                    createArmoredBronze = true;
                    player.GetInventory().RemoveItem("$item_bronze", ArmorBronzeRequiredConfig.Value);
                }
            }

            bool createArmoredIron = false;
            if (ExtraResourceConsumptionUnlocked
                && !createWoodcutter
                && !createArmoredLeather 
                && !createArmoredBronze
                && ArmorIronRequiredConfig.Value > 0)
            {
                int ironInInventory = player.GetInventory().CountItems("$item_iron");
                if (ironInInventory >= ArmorIronRequiredConfig.Value)
                {
                    createArmoredIron = true;
                    player.GetInventory().RemoveItem("$item_iron", ArmorIronRequiredConfig.Value);
                }
            }

            bool createArmoredBlackIron = false;
            if (ExtraResourceConsumptionUnlocked
                && !createWoodcutter
                && !createArmoredLeather
                && !createArmoredBronze
                && !createArmoredIron
                && ArmorBlackIronRequiredConfig.Value > 0)
            {
                int blackIronInInventory = player.GetInventory().CountItems("$item_blackmetal");
                if (blackIronInInventory >= ArmorBlackIronRequiredConfig.Value)
                {
                    createArmoredBlackIron = true;
                    player.GetInventory().RemoveItem("$item_blackmetal", ArmorBlackIronRequiredConfig.Value);
                }
            }

            bool createMage = false;
            if (ExtraResourceConsumptionUnlocked
                && !createWoodcutter
                && !archer
                && SurtlingCoresRequiredConfig.Value > 0)
            {
                int surtlingCoresInInventory = player.GetInventory().CountItems("$item_surtlingcore");
                if (surtlingCoresInInventory >= SurtlingCoresRequiredConfig.Value)
                {
                    createMage = true;
                    player.GetInventory().RemoveItem("$item_surtlingcore", SurtlingCoresRequiredConfig.Value);
                }
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (MaxSkeletons.Value > 0)
            {
                // re-count the current active skeletons
                CountActiveSkeletonMinions();
            }

            // scale according to skill
            float playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);

            int quality = SkeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= SkeletonTierThreeLevelReq.Value) { quality = SkeletonTierThreeQuality.Value; }
            else if (playerNecromancyLevel >= SkeletonTierTwoLevelReq.Value) { quality = SkeletonTierTwoQuality.Value; }

            SkeletonMinion.SkeletonType skeletonType = SkeletonMinion.SkeletonType.Warrior;
            if (archer) { skeletonType = SkeletonMinion.SkeletonType.Archer; }
            else if (createWoodcutter) { skeletonType = SkeletonMinion.SkeletonType.Woodcutter; }
            else if (createMage) { skeletonType = SkeletonMinion.SkeletonType.Mage; }
            else if (playerNecromancyLevel >= PoisonSkeletonLevelRequirementConfig.Value
                && ConsumeGuckIfAvailable(player))
            {
                skeletonType = SkeletonMinion.SkeletonType.Poison;
            }

            InstantiateSkeleton(player, quality, playerNecromancyLevel,
                skeletonType,
                createArmoredLeather, createArmoredBronze,
                createArmoredIron, createArmoredBlackIron);
        }

        private void InstantiateSkeleton(Player player, int quality, float playerNecromancyLevel, SkeletonMinion.SkeletonType skeletonType, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor)
        {
            string prefabName = PrefabFromNecromancyLevel(playerNecromancyLevel, skeletonType);
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
                SkeletonMinion.SkeletonType.Poison => spawnedChar.AddComponent<PoisonSkeletonMinion>(),
                SkeletonMinion.SkeletonType.Woodcutter => spawnedChar.AddComponent<SkeletonWoodcutterMinion>(),
                _ => spawnedChar.AddComponent<SkeletonMinion>()
            };
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, skeletonType, leatherArmor, bronzeArmor, ironArmor, blackIronArmor);
            minion.ScaleStats(playerNecromancyLevel); 

            if (skeletonType != SkeletonMinion.SkeletonType.Woodcutter)
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
                if (skeletonType == SkeletonMinion.SkeletonType.Mage)
                {
                    // surtling core
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("SurtlingCore"),
                        m_onePerPlayer = true,
                        m_amountMin = SurtlingCoresRequiredConfig.Value,
                        m_amountMax = SurtlingCoresRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                if (leatherArmor)
                {
                    // for now, assume deerhide
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("DeerHide"),
                        m_onePerPlayer = true,
                        m_amountMin = ArmorLeatherScrapsRequiredConfig.Value,
                        m_amountMax = ArmorLeatherScrapsRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (bronzeArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Bronze"),
                        m_onePerPlayer = true,
                        m_amountMin = ArmorBronzeRequiredConfig.Value,
                        m_amountMax = ArmorBronzeRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (ironArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Iron"),
                        m_onePerPlayer = true,
                        m_amountMin = ArmorIronRequiredConfig.Value,
                        m_amountMax = ArmorIronRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (blackIronArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BlackMetal"),
                        m_onePerPlayer = true,
                        m_amountMin = ArmorBlackIronRequiredConfig.Value,
                        m_amountMax = ArmorBlackIronRequiredConfig.Value,
                        m_chance = 1f
                    });
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

        private bool ConsumeGuckIfAvailable(Player player)
        {
            if (!ExtraResourceConsumptionUnlocked) return false;

            // return true if guck is available and got consumed
            int guckInInventory = player.GetInventory().CountItems("$item_guck");
            if (guckInInventory >= PoisonSkeletonGuckRequiredConfig.Value)
            {
                player.GetInventory().RemoveItem("$item_guck", PoisonSkeletonGuckRequiredConfig.Value);
                return true;
            }
            return false;
        }

        private string PrefabFromNecromancyLevel(float necromancyLevel, SkeletonMinion.SkeletonType skeletonType)
        {
            string result = "";
            switch (skeletonType)
            {
                case SkeletonMinion.SkeletonType.Woodcutter:
                    result = SkeletonWoodcutterPrefabName;
                    break;
                
                case SkeletonMinion.SkeletonType.Archer:
                    if (necromancyLevel >= 75)
                    {
                        result = SkeletonArcherTier3PrefabName;
                    }
                    else if (necromancyLevel >= 35)
                    {
                        result = SkeletonArcherTier2PrefabName;
                    }
                    else
                    {
                        result = SkeletonArcherPrefabName;
                    }
                    break;
                case SkeletonMinion.SkeletonType.Mage:
                    if (necromancyLevel >= 75)
                    {
                        result = SkeletonMageTier3PrefabName;
                    }
                    else if (necromancyLevel >= 35)
                    {
                        result = SkeletonMageTier2PrefabName;
                    }
                    else
                    {
                        result = SkeletonMagePrefabName;
                    }
                    break;
                case SkeletonMinion.SkeletonType.Poison:
                    if (necromancyLevel >= 85)
                    {
                        result = PoisonSkeleton3PrefabName;
                    }
                    else if (necromancyLevel >= 65)
                    {
                        result = PoisonSkeleton2PrefabName;
                    }
                    else
                    {
                        result = PoisonSkeletonPrefabName;
                    }
                    break;
                default:
                    if (necromancyLevel >= 60)
                    {
                        result = SkeletonWarriorTier3PrefabName;
                    }
                    else if (necromancyLevel >= 30)
                    {
                        result = SkeletonWarriorTier2PrefabName;
                    }
                    else
                    {
                        result = SkeletonWarriorPrefabName;
                    }
                    break;
            }
            return result;
        }
    }
}
