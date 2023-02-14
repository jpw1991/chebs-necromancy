using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using ChebsNecromancy.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.PlayerItems
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
               
        public override void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.DefaultRecipe = "Wood:5,Stone:1";
            ChebsRecipeConfig.RecipeName = "$item_friendlyskeletonwand";
            ChebsRecipeConfig.ItemName = "ChebGonaz_SkeletonWand";
            ChebsRecipeConfig.RecipeDescription = "$item_friendlyskeletonwand_desc";
            ChebsRecipeConfig.PrefabName = "ChebGonaz_SkeletonWand.prefab";
            ChebsRecipeConfig.ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name;

            base.CreateConfigs(plugin);

            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName+"Allowed",
                true, "Whether crafting a Skeleton Wand is allowed or not.", null, true);

            ChebsRecipeConfig.CraftingStationRequired = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName+"CraftingStation",
                ChebsRecipe.EcraftingTable.Workbench, "Crafting station where Skeleton Wand is available", null, true);

            ChebsRecipeConfig.CraftingStationLevel = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName+"CraftingStationLevel",
                1, "Crafting station level required to craft Skeleton Wand", plugin.IntQuantityValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName+"CraftingCosts",
                ChebsRecipeConfig.DefaultRecipe, "Materials needed to craft Skeleton Wand. None or Blank will use Default settings.", 
                null, true);

            SkeletonSetFollowRange = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.RecipeName+"CommandRange",
                20f, "The distance which nearby skeletons will hear your commands.", plugin.IntQuantityValue);                    

            SkeletonsAllowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonsAllowed",
                true, "If false, skeletons aren't loaded at all and can't be summoned.", plugin.BoolValue, true);

            SkeletonBaseHealth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonBaseHealth",
                20f, "HP = BaseHealth + NecromancyLevel * HealthMultiplier", plugin.FloatQuantityValue, true);

            SkeletonHealthMultiplier = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonHealthMultiplier",
                2.5f, "HP = BaseHealth + NecromancyLevel * HealthMultiplier", plugin.FloatQuantityValue, true);

            SkeletonTierOneQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonTierOneQuality",
                1, "Star Quality of tier 1 Skeleton minions", plugin.IntQuantityValue, true);

            SkeletonTierTwoQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonTierTwoQuality",
                2, "Star Quality of tier 2 Skeleton minions", plugin.IntQuantityValue, true);

            SkeletonTierTwoLevelReq = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonTierTwoLevelReq",
                35, "Necromancy skill level required to summon Tier 2 Skeleton", plugin.IntQuantityValue, true);

            SkeletonTierThreeQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonTierThreeQuality",
                3, "Star Quality of tier 3 Skeleton minions", plugin.IntQuantityValue, true);

            SkeletonTierThreeLevelReq = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonTierThreeLevelReq",
                70, "Necromancy skill level required to summon Tier 3 Skeleton", plugin.IntQuantityValue, true);

            BoneFragmentsRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BoneFragmentsRequired",
                3, "The amount of Bone Fragments required to craft a skeleton.", plugin.IntQuantityValue, true);

            BoneFragmentsDroppedAmountMin = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BoneFragmentsDroppedAmountMin",
                1, "The minimum amount of bones dropped by creatures.", plugin.IntQuantityValue, true);

            BoneFragmentsDroppedAmountMax = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BoneFragmentsDroppedAmountMax",
                3, "The maximum amount of bones dropped by creautres.", plugin.IntQuantityValue, true);

            _necromancyLevelIncrease = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NecromancyLevelIncrease",
                1f, "How much crafting a skeleton contributes to your Necromancy level increasing.", 
                plugin.FloatQuantityValue, true);

            MaxSkeletons = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "MaximumSkeletons",
                0, "The maximum amount of skeletons that can be made (0 = unlimited).", null, true);

            ArmorLeatherScrapsRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmoredSkeletonLeatherScrapsRequired",
                5, "The amount of LeatherScraps required to craft a skeleton in leather armor.", null, true);

            ArmorBronzeRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmoredSkeletonBronzeRequired",
                1, "The amount of Bronze required to craft a skeleton in bronze armor.", null, true);

            ArmorIronRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmoredSkeletonIronRequired",
                1, "The amount of Iron required to craft a skeleton in iron armor.", null, true);

            SurtlingCoresRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonMageSurtlingCoresRequired",
                1, "The amount of surtling cores required to craft a skeleton mage.", null, true);

            ArmorBlackIronRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ArmoredSkeletonBlackIronRequired",
                1, "The amount of Black Metal required to craft a skeleton in black iron armor.", null, true);

            PoisonSkeletonBaseHealth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PoisonSkeletonBaseHealth",
                100f, "HP = BaseHealth + NecromancyLevel * HealthMultiplier", plugin.FloatQuantityValue, true);

            PoisonSkeletonLevelRequirementConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PoisonSkeletonLevelRequired",
                50, "The Necromancy level needed to summon a Poison Skeleton.", null, true);

            PoisonSkeletonGuckRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PoisonSkeletonGuckRequired",
                1, "The amount of Guck required to craft a Poison Skeleton.", null, true);

            PoisonSkeletonNecromancyLevelIncrease = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PoisonSkeletonNecromancyLevelIncrease",
                3f, "How much crafting a Poison Skeleton contributes to your Necromancy level increasing.", 
                plugin.FloatQuantityValue, true);

            SkeletonArmorValueMultiplier = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SkeletonArmorValueMultiplier",
                1f, "If you find the armor value for skeletons to be too low, you can multiply it here. By default, a skeleton wearing " + 
                "iron armor will have an armor value of 42 (14+14+14). A multiplier of 1.5 will cause this armor value to increase to 63.", 
                plugin.FloatQuantityValue, true);

            DurabilityDamage = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamage",
                true, "Whether using a Skeleton Wand damages its durability.", plugin.BoolValue, true);

            DurabilityDamageWarrior = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageWarrior",
                1f, "How much creating a warrior damages the wand.", plugin.FloatQuantityValue, true);

            DurabilityDamageArcher = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageArcher",
                3f, "How much creating an archer damages the wand.", plugin.FloatQuantityValue, true);

            DurabilityDamageMage = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageMage",
                5f, "How much creating a mage damages the wand.", plugin.FloatQuantityValue, true);

            DurabilityDamagePoison = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamagePoison",
                5f, "How much creating a poison skeleton damages the wand.", plugin.FloatQuantityValue, true);

            DurabilityDamageLeather = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageLeather",
                1f, "How much armoring the minion in leather damages the wand (value is added on top of damage from minion type).", 
                plugin.FloatQuantityValue, true);

            DurabilityDamageBronze = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageBronze",
                1f, "How much armoring the minion in bronze damages the wand (value is added on top of damage from minion type)", 
                plugin.FloatQuantityValue, true);

            DurabilityDamageIron = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageIron",
                1f, "How much armoring the minion in iron damages the wand (value is added on top of damage from minion type)", 
                plugin.FloatQuantityValue, true);

            DurabilityDamageBlackIron = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageBlackIron",
                1f, "How much armoring the minion in black iron damages the wand (value is added on top of damage from minion type)", 
                plugin.FloatQuantityValue, true);
        }

        public override KeyHintConfig GetKeyHint()
        {
            List<ButtonConfig> buttonConfigs = new();

            if (CreateMinionButton != null) buttonConfigs.Add(CreateMinionButton);
            if (CreateArcherMinionButton != null) buttonConfigs.Add(CreateArcherMinionButton);
            if (FollowButton != null) buttonConfigs.Add(FollowButton);
            if (WaitButton != null) buttonConfigs.Add(WaitButton);
            if (TeleportButton != null) buttonConfigs.Add(TeleportButton);
            if (AttackTargetButton != null) buttonConfigs.Add(AttackTargetButton);

            return new KeyHintConfig
            {
                Item = ChebsRecipeConfig.RecipeName,
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
            if (AttackTargetButton != null && ZInput.GetButton(AttackTargetButton.Name))
            {
                MakeFollowingMinionsAttackTarget(Player.m_localPlayer);
                return true;
            }

            return false;
        }

        private void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, bool archer)
        {
            if (!SkeletonsAllowed.Value) return;

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
            if (ExtraResourceConsumptionUnlocked && ArmorLeatherScrapsRequiredConfig.Value > 0)
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
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && ArmorBronzeRequiredConfig.Value > 0)
            {
                int bronzeInInventory = player.GetInventory().CountItems("$item_bronze");
                if (bronzeInInventory >= ArmorBronzeRequiredConfig.Value)
                {
                    createArmoredBronze = true;
                    player.GetInventory().RemoveItem("$item_bronze", ArmorBronzeRequiredConfig.Value);
                }
            }

            bool createArmoredIron = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && !createArmoredBronze && ArmorIronRequiredConfig.Value > 0)
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

            SkeletonMinion minion = skeletonType == SkeletonMinion.SkeletonType.Poison
                ? spawnedChar.AddComponent<PoisonSkeletonMinion>()
                : spawnedChar.AddComponent<SkeletonMinion>();
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, skeletonType, leatherArmor, bronzeArmor, ironArmor, blackIronArmor);
            minion.ScaleStats(playerNecromancyLevel);

            if (FollowByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill, _necromancyLevelIncrease.Value);

            minion.SetUndeadMinionMaster(player.GetPlayerName());

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
            List<Tuple<int, Character>> minionsFound = new();

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
            string result;

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
