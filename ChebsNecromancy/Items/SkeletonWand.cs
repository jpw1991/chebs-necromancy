using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChebsNecromancy
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
        #endregion
        #region ConfigEntries
        public static ConfigEntry<CraftingTable> craftingStationRequired;
        public static ConfigEntry<int> craftingStationLevel;
        public static ConfigEntry<string> craftingCost;

        public static ConfigEntry<bool> skeletonsAllowed;

        public static ConfigEntry<int> maxSkeletons;

        public static ConfigEntry<float> skeletonBaseHealth;
        public static ConfigEntry<float> skeletonHealthMultiplier;
        public static ConfigEntry<int> skeletonTierOneQuality;
        public static ConfigEntry<int> skeletonTierTwoQuality;
        public static ConfigEntry<int> skeletonTierTwoLevelReq;
        public static ConfigEntry<int> skeletonTierThreeQuality;
        public static ConfigEntry<int> skeletonTierThreeLevelReq;
        public static ConfigEntry<float> skeletonSetFollowRange;

        private static ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> boneFragmentsRequiredConfig;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMax;

        public static ConfigEntry<int> armorLeatherScrapsRequiredConfig;
        public static ConfigEntry<int> armorBronzeRequiredConfig;
        public static ConfigEntry<int> armorIronRequiredConfig;
        public static ConfigEntry<int> armorBlackIronRequiredConfig;
        public static ConfigEntry<int> surtlingCoresRequiredConfig;

        public static ConfigEntry<int> poisonSkeletonLevelRequirementConfig;
        public static ConfigEntry<float> poisonSkeletonBaseHealth;
        public static ConfigEntry<int> poisonSkeletonGuckRequiredConfig;
        public static ConfigEntry<float> poisonSkeletonNecromancyLevelIncrease;
        public static ConfigEntry<float> skeletonArmorValueMultiplier;

        public static ConfigEntry<bool> durabilityDamage;
        public static ConfigEntry<float> durabilityDamageWarrior;
        public static ConfigEntry<float> durabilityDamageMage;
        public static ConfigEntry<float> durabilityDamageArcher;
        public static ConfigEntry<float> durabilityDamagePoison;
        public static ConfigEntry<float> durabilityDamageLeather;
        public static ConfigEntry<float> durabilityDamageBronze;
        public static ConfigEntry<float> durabilityDamageIron;
        public static ConfigEntry<float> durabilityDamageBlackIron;
        #endregion

        public override string ItemName { get { return "ChebGonaz_SkeletonWand"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonWand.prefab"; } }
        protected override string DefaultRecipe { get { return "Wood:5,Stone:1"; } }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            skeletonSetFollowRange = plugin.Config.Bind("SkeletonWand (Client)", "SkeletonCommandRange",
                20f, new ConfigDescription("The distance which nearby skeletons will hear your commands."));

            allowed = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandAllowed",
                true, new ConfigDescription("Whether crafting a Skeleton Wand is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingStationRequired = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Skeleton Wand is available", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingStationLevel = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Skeleton Wand", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingCost = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonWandCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft Skeleton Wand. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonsAllowed = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonsAllowed",
                true, new ConfigDescription("If false, skeletons aren't loaded at all and can't be summoned.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonBaseHealth = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonBaseHealth",
                20f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonHealthMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonHealthMultiplier",
                2.5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonTierOneQuality = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Skeleton minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonTierTwoQuality = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Skeleton minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonTierTwoLevelReq = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Skeleton", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonTierThreeQuality = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Skeleton minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonTierThreeLevelReq = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Skeleton", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            boneFragmentsRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsRequired",
                3, new ConfigDescription("The amount of Bone Fragments required to craft a skeleton.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            boneFragmentsDroppedAmountMin = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsDroppedAmountMin",
                1, new ConfigDescription("The minimum amount of bones dropped by creatures.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            boneFragmentsDroppedAmountMax = plugin.Config.Bind("SkeletonWand (Server Synced)", "BoneFragmentsDroppedAmountMax",
                3, new ConfigDescription("The maximum amount of bones dropped by creautres.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancyLevelIncrease = plugin.Config.Bind("SkeletonWand (Server Synced)", "NecromancyLevelIncrease",
                1f, new ConfigDescription("How much crafting a skeleton contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            maxSkeletons = plugin.Config.Bind("SkeletonWand (Server Synced)", "MaximumSkeletons",
                0, new ConfigDescription("The maximum amount of skeletons that can be made (0 = unlimited).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            armorLeatherScrapsRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonLeatherScrapsRequired",
                5, new ConfigDescription("The amount of LeatherScraps required to craft a skeleton in leather armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            armorBronzeRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonBronzeRequired",
                1, new ConfigDescription("The amount of Bronze required to craft a skeleton in bronze armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            armorIronRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonIronRequired",
                1, new ConfigDescription("The amount of Iron required to craft a skeleton in iron armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            surtlingCoresRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonMageSurtlingCoresRequired",
                1, new ConfigDescription("The amount of surtling cores required to craft a skeleton mage.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            armorBlackIronRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "ArmoredSkeletonBlackIronRequired",
                1, new ConfigDescription("The amount of Black Metal required to craft a skeleton in black iron armor.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            poisonSkeletonBaseHealth = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonBaseHealth",
                100f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            poisonSkeletonLevelRequirementConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonLevelRequired",
                50, new ConfigDescription("The Necromancy level needed to summon a Poison Skeleton.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            poisonSkeletonGuckRequiredConfig = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonGuckRequired",
                1, new ConfigDescription("The amount of Guck required to craft a Poison Skeleton.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            poisonSkeletonNecromancyLevelIncrease = plugin.Config.Bind("SkeletonWand (Server Synced)", "PoisonSkeletonNecromancyLevelIncrease",
                3f, new ConfigDescription("How much crafting a Poison Skeleton contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            skeletonArmorValueMultiplier = plugin.Config.Bind("SkeletonWand (Server Synced)", "SkeletonArmorValueMultiplier",
                1f, new ConfigDescription("If you find the armor value for skeletons to be too low, you can multiply it here. By default, a skeleton wearing iron armor will have an armor value of 42 (14+14+14). A multiplier of 1.5 will cause this armor value to increase to 63.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamage = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamage",
                true, new ConfigDescription("Whether using a Skeleton Wand damages its durability.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageWarrior = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageWarrior",
                1f, new ConfigDescription("How much creating a warrior damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageArcher = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageArcher",
                3f, new ConfigDescription("How much creating an archer damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageMage = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageMage",
                5f, new ConfigDescription("How much creating a mage damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamagePoison = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamagePoison",
                5f, new ConfigDescription("How much creating a poison skeleton damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageLeather = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageLeather",
                1f, new ConfigDescription("How much armoring the minion in leather damages the wand (value is added on top of damage from minion type).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageBronze = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageBronze",
                1f, new ConfigDescription("How much armoring the minion in bronze damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageIron = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageIron",
                1f, new ConfigDescription("How much armoring the minion in iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageBlackIron = plugin.Config.Bind("SkeletonWand (Server Synced)", "DurabilityDamageBlackIron",
                1f, new ConfigDescription("How much armoring the minion in black iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override void CreateButtons()
        {
            // call the base to add the basic generic buttons -> create, attack, follow, wait, etc.
            base.CreateButtons();

            // add any extra buttons
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand";
            config.Description = "$item_friendlyskeletonwand_desc";

            if (allowed.Value)
            {
                if (string.IsNullOrEmpty(craftingCost.Value))
                {
                    craftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                this.SetRecipeReqs(
                    config,
                    craftingCost,
                    craftingStationRequired,
                    craftingStationLevel
                );
            }
            else
            {
                config.Enabled = false;
            }

            CustomItem customItem = new CustomItem(prefab, false, config);
            if (customItem == null)
            {
                Jotunn.Logger.LogError($"AddCustomItems: {PrefabName}'s CustomItem is null!");
                return null;
            }
            if (customItem.ItemPrefab == null)
            {
                Jotunn.Logger.LogError($"AddCustomItems: {PrefabName}'s ItemPrefab is null!");
                return null;
            }
            // make sure the set effect is applied
            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect = BasePlugin.setEffectNecromancyArmor;

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
            if (AttackTargetButton != null) buttonConfigs.Add(AttackTargetButton);

            return new KeyHintConfig
            {
                Item = ItemName,
                ButtonConfigs = buttonConfigs.ToArray()
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
                ExtraResourceConsumptionUnlocked =
                    UnlockExtraResourceConsumptionButton == null
                    || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        false
                        );
                    return true;
                }
                else if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
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

        public void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, float necromancyLevelIncrease, bool archer)
        {
            if (!skeletonsAllowed.Value) return;

            // check player inventory for requirements
            if (boneFragmentsRequired > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < boneFragmentsRequired)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenoughbones");
                    return;
                }

                // consume the fragments
                player.GetInventory().RemoveItem("$item_bonefragments", boneFragmentsRequired);
            }

            bool createArmoredLeather = false;
            if (ExtraResourceConsumptionUnlocked && armorLeatherScrapsRequiredConfig.Value > 0)
            {
                int leatherScrapsInInventory = player.GetInventory().CountItems("$item_leatherscraps");
                if (leatherScrapsInInventory >= armorLeatherScrapsRequiredConfig.Value)
                {
                    createArmoredLeather = true;
                    player.GetInventory().RemoveItem("$item_leatherscraps", armorLeatherScrapsRequiredConfig.Value);
                }
                else
                {
                    // no leather scraps? Try some deer hide
                    int deerHideInInventory = player.GetInventory().CountItems("$item_deerhide");
                    if (deerHideInInventory >= armorLeatherScrapsRequiredConfig.Value)
                    {
                        createArmoredLeather = true;
                        player.GetInventory().RemoveItem("$item_deerhide", armorLeatherScrapsRequiredConfig.Value);
                    }
                }
            }

            bool createArmoredBronze = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && armorBronzeRequiredConfig.Value > 0)
            {
                int bronzeInInventory = player.GetInventory().CountItems("$item_bronze");
                if (bronzeInInventory >= armorBronzeRequiredConfig.Value)
                {
                    createArmoredBronze = true;
                    player.GetInventory().RemoveItem("$item_bronze", armorBronzeRequiredConfig.Value);
                }
            }

            bool createArmoredIron = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && !createArmoredBronze && armorIronRequiredConfig.Value > 0)
            {
                int ironInInventory = player.GetInventory().CountItems("$item_iron");
                if (ironInInventory >= armorIronRequiredConfig.Value)
                {
                    createArmoredIron = true;
                    player.GetInventory().RemoveItem("$item_iron", armorIronRequiredConfig.Value);
                }
            }

            bool createArmoredBlackIron = false;
            if (ExtraResourceConsumptionUnlocked
                && !createArmoredLeather
                && !createArmoredBronze
                && !createArmoredIron
                && armorBlackIronRequiredConfig.Value > 0)
            {
                int blackIronInInventory = player.GetInventory().CountItems("$item_blackmetal");
                if (blackIronInInventory >= armorBlackIronRequiredConfig.Value)
                {
                    createArmoredBlackIron = true;
                    player.GetInventory().RemoveItem("$item_blackmetal", armorBlackIronRequiredConfig.Value);
                }
            }

            bool createMage = false;
            if (ExtraResourceConsumptionUnlocked
                && !archer
                && surtlingCoresRequiredConfig.Value > 0)
            {
                int surtlingCoresInInventory = player.GetInventory().CountItems("$item_surtlingcore");
                if (surtlingCoresInInventory >= surtlingCoresRequiredConfig.Value)
                {
                    createMage = true;
                    player.GetInventory().RemoveItem("$item_surtlingcore", surtlingCoresRequiredConfig.Value);
                }
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (maxSkeletons.Value > 0)
            {
                // re-count the current active skeletons
                CountActiveSkeletonMinions();
            }

            // scale according to skill
            float playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);

            int quality = skeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= skeletonTierThreeLevelReq.Value) { quality = skeletonTierThreeQuality.Value; }
            else if (playerNecromancyLevel >= skeletonTierTwoLevelReq.Value) { quality = skeletonTierTwoQuality.Value; }

            SkeletonMinion.SkeletonType skeletonType = SkeletonMinion.SkeletonType.Warrior;
            if (archer) { skeletonType = SkeletonMinion.SkeletonType.Archer; }
            else if (createMage) { skeletonType = SkeletonMinion.SkeletonType.Mage; }
            else if (playerNecromancyLevel >= poisonSkeletonLevelRequirementConfig.Value
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
                Jotunn.Logger.LogError($"InstantiateSkeleton: spawning {prefabName} failed");
            }

            GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            spawnedChar.AddComponent<FreshMinion>();

            SkeletonMinion minion = skeletonType == SkeletonMinion.SkeletonType.Poison
                ? spawnedChar.AddComponent<PoisonSkeletonMinion>()
                : spawnedChar.AddComponent<SkeletonMinion>();
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, skeletonType, leatherArmor, bronzeArmor, ironArmor, blackIronArmor);
            minion.ScaleStats(playerNecromancyLevel);

            if (followByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill, necromancyLevelIncrease.Value);

            minion.SetUndeadMinionMaster(player.GetPlayerName());

            // handle refunding of resources on death
            if (SkeletonMinion.dropOnDeath.Value != UndeadMinion.DropType.Nothing)
            {
                CharacterDrop characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (SkeletonMinion.dropOnDeath.Value == UndeadMinion.DropType.Everything
                    && boneFragmentsRequiredConfig.Value > 0)
                {
                    // bones
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                        m_onePerPlayer = true,
                        m_amountMin = boneFragmentsRequiredConfig.Value,
                        m_amountMax = boneFragmentsRequiredConfig.Value,
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
                        m_amountMin = surtlingCoresRequiredConfig.Value,
                        m_amountMax = surtlingCoresRequiredConfig.Value,
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
                        m_amountMin = armorLeatherScrapsRequiredConfig.Value,
                        m_amountMax = armorLeatherScrapsRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (bronzeArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Bronze"),
                        m_onePerPlayer = true,
                        m_amountMin = armorBronzeRequiredConfig.Value,
                        m_amountMax = armorBronzeRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (ironArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Iron"),
                        m_onePerPlayer = true,
                        m_amountMin = armorIronRequiredConfig.Value,
                        m_amountMax = armorIronRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (blackIronArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BlackMetal"),
                        m_onePerPlayer = true,
                        m_amountMin = armorBlackIronRequiredConfig.Value,
                        m_amountMax = armorBlackIronRequiredConfig.Value,
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

        public int CountActiveSkeletonMinions()
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

            for (int i = 0; i < minionsFound.Count; i++)
            {
                // kill off surplus
                if (result >= maxSkeletons.Value - 1)
                {
                    Tuple<int, Character> tuple = minionsFound[i];
                    tuple.Item2.SetHealth(0);
                    continue;
                }

                result++;
            }

            return result;
        }

        private bool ConsumeGuckIfAvailable(Player player)
        {
            if (!ExtraResourceConsumptionUnlocked) return false;

            // return true if guck is available and got consumed
            int guckInInventory = player.GetInventory().CountItems("$item_guck");
            if (guckInInventory >= poisonSkeletonGuckRequiredConfig.Value)
            {
                player.GetInventory().RemoveItem("$item_guck", poisonSkeletonGuckRequiredConfig.Value);
                return true;
            }
            return false;
        }

        private string PrefabFromNecromancyLevel(float necromancyLevel, SkeletonMinion.SkeletonType skeletonType)
        {
            string result = "";
            switch (skeletonType)
            {
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
