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
    internal class DraugrWand : Wand
    {
        #region ConfigEntries
        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;

        public static ConfigEntry<string> CraftingCost;

        public static ConfigEntry<bool> DraugrAllowed;

        public static ConfigEntry<float> DraugrBaseHealth;
        public static ConfigEntry<float> DraugrHealthMultiplier;
        public static ConfigEntry<int> DraugrTierOneQuality;
        public static ConfigEntry<int> DraugrTierTwoQuality;
        public static ConfigEntry<int> DraugrTierTwoLevelReq;
        public static ConfigEntry<int> DraugrTierThreeQuality;
        public static ConfigEntry<int> DraugrTierThreeLevelReq;
        public static ConfigEntry<float> DraugrSetFollowRange;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> DraugrBoneFragmentsRequiredConfig;
        public static ConfigEntry<int> DraugrMeatRequiredConfig;
        #endregion

        public override string ItemName => "ChebGonaz_DraugrWand";
        public override string PrefabName => "ChebGonaz_DraugrWand.prefab";
        protected override string DefaultRecipe => "ElderBark:5,FineWood:5,Bronze:5,TrophyDraugr:1";

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            DraugrSetFollowRange = plugin.Config.Bind("DraugrWand (Client)", "DraugrCommandRange",
                20f, new ConfigDescription("The range from which nearby Draugr will hear your command.", null));

            Allowed = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandAllowed",
                true, new ConfigDescription("Whether crafting a Draugr Wand is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandCraftingStation",
                CraftingTable.Forge, new ConfigDescription("Crafting station where Draugr Wand is available", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Draugr Wand", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandCraftingCosts",
                DefaultRecipe, new ConfigDescription(
                    "Materials needed to craft Draugr Wand. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrAllowed = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrAllowed",
                true, new ConfigDescription("If false, draugr aren't loaded at all and can't be summoned.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrBaseHealth = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrBaseHealth",
                80f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrHealthMultiplier = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrHealthMultiplier",
                2.5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierOneQuality = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierTwoQuality = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierTwoLevelReq = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Draugr", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierThreeQuality = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierThreeLevelReq = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Draugr", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrMeatRequiredConfig = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrMeatRequired",
                2, new ConfigDescription("How many pieces of meat it costs to make a Draugr.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrBoneFragmentsRequiredConfig = plugin.Config.Bind("DraugrWand (Server Synced)",
                "DraugrBoneFragmentsRequired",
                6, new ConfigDescription("How many bone fragments it costs to make a Draugr.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancyLevelIncrease = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrNecromancyLevelIncrease",
                1.5f, new ConfigDescription(
                    "How much creating a Draugr contributes to your Necromancy level increasing.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override void UpdateRecipe()
        {
            UpdateRecipe(CraftingStationRequired, CraftingCost, CraftingStationLevel);
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
            config.Name = "$item_friendlyskeletonwand_draugrwand";
            config.Description = "$item_friendlyskeletonwand_draugrwand_desc";

            if (Allowed.Value)
            {
                if (string.IsNullOrEmpty(CraftingCost.Value))
                {
                    CraftingCost.Value = DefaultRecipe;
                }

                // set recipe requirements
                SetRecipeReqs(
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
            if (MessageHud.instance != null
                && Player.m_localPlayer != null
                && Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                    equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_draugrwand")
                ) != null
               )
            {
                ExtraResourceConsumptionUnlocked =
                    UnlockExtraResourceConsumptionButton == null
                    || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnDraugr();
                    return true;
                }

                if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnRangedDraugr();
                    return true;
                }
                if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbyMinionsFollow(DraugrSetFollowRange.Value, true);
                    return true;
                }
                if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    if (ExtraResourceConsumptionUnlocked)
                    {
                        MakeNearbyMinionsRoam(DraugrSetFollowRange.Value);
                    }
                    else
                    {
                        MakeNearbyMinionsFollow(DraugrSetFollowRange.Value, false);
                    }

                    return true;
                }
                if (TeleportButton != null && ZInput.GetButton(TeleportButton.Name))
                {
                    TeleportFollowingMinionsToPlayer();
                    return true;
                }
            }

            return false;
        }

        private void ConsumeResources(DraugrMinion.DraugrType draugrType, UndeadMinion.ArmorType armorType, Dictionary<string, int> meatTypesFound)
        {
            Player player = Player.m_localPlayer;

            // consume bones
            player.GetInventory().RemoveItem("$item_bonefragments", DraugrBoneFragmentsRequiredConfig.Value);
            
            // consume the meat
            int meatConsumed = 0;
            Stack<Tuple<string, int>> meatToConsume = new Stack<Tuple<string, int>>();
            foreach (string key in meatTypesFound.Keys)
            {
                if (meatConsumed >= DraugrMeatRequiredConfig.Value)
                {
                    break;
                }

                int meatAvailable = meatTypesFound[key];

                if (meatAvailable <= DraugrMeatRequiredConfig.Value)
                {
                    meatToConsume.Push(new Tuple<string, int>(key, meatAvailable));
                    meatConsumed += meatAvailable;
                }
                else
                {
                    meatToConsume.Push(new Tuple<string, int>(key, DraugrMeatRequiredConfig.Value));
                    meatConsumed += DraugrMeatRequiredConfig.Value;
                }
            }

            while (meatToConsume.Count > 0)
            {
                Tuple<string, int> keyValue = meatToConsume.Pop();
                player.GetInventory().RemoveItem(keyValue.Item1, keyValue.Item2);
            }

            // consume other
            switch (draugrType)
            {
                case DraugrMinion.DraugrType.ArcherTier1:
                    player.GetInventory().RemoveItem("$item_arrow_wood", BasePlugin.ArcherTier1ArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.ArcherTier2:
                    player.GetInventory().RemoveItem("$item_arrow_bronze", BasePlugin.ArcherTier2ArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.ArcherTier3:
                    player.GetInventory().RemoveItem("$item_arrow_iron", BasePlugin.ArcherTier3ArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.ArcherPoison:
                    player.GetInventory().RemoveItem("$item_arrow_poison", BasePlugin.ArcherPoisonArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.ArcherFire:
                    player.GetInventory().RemoveItem("$item_arrow_fire", BasePlugin.ArcherFireArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.ArcherFrost:
                    player.GetInventory().RemoveItem("$item_arrow_frost", BasePlugin.ArcherFrostArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.ArcherSilver:
                    player.GetInventory().RemoveItem("$item_arrow_silver", BasePlugin.ArcherSilverArrowsRequiredConfig.Value);
                    break;
                case DraugrMinion.DraugrType.WarriorNeedle:
                    player.GetInventory().RemoveItem("$item_needle", BasePlugin.NeedlesRequiredConfig.Value);
                    break;
            }

            // consume armor materials
            switch (armorType)
            {
                case UndeadMinion.ArmorType.Leather:
                    // todo: expose these options to config
                    var leatherItemTypes = new List<string>()
                    {
                        "$item_leatherscraps",
                        "$item_deerhide",
                        "$item_trollhide",
                        "$item_wolfpelt",
                        "$item_loxpelt",
                        "$item_scalehide"
                    };
                    
                    foreach (var leatherItem in leatherItemTypes)
                    {
                        var leatherItemsInInventory = player.GetInventory().CountItems(leatherItem);
                        if (leatherItemsInInventory >= BasePlugin.ArmorLeatherScrapsRequiredConfig.Value)
                        {
                            player.GetInventory().RemoveItem(leatherItem,
                                BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                            break;
                        }
                    }
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


        private DraugrMinion.DraugrType SpawnDraugrArcher()
        {
            // Determine type of archer to spawn and consume resources.
            // Return None if unable to determine archer type, or if necessary resources are missing.
            
            var player = Player.m_localPlayer;
            
            // check for bones
            if (DraugrBoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < DraugrBoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlydraugrwand_notenoughbones");
                    return DraugrMinion.DraugrType.None;
                }
            }

            // check for arrows
            var silverRequirement = BasePlugin.ArcherSilverArrowsRequiredConfig.Value;
            if (silverRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_silver") >= silverRequirement)
            {
                return DraugrMinion.DraugrType.ArcherSilver;
            }
            
            var fireRequirement = BasePlugin.ArcherFireArrowsRequiredConfig.Value;
            if (fireRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_fire") >= fireRequirement)
            {
                return DraugrMinion.DraugrType.ArcherFire;
            }

            var frostRequirement = BasePlugin.ArcherFrostArrowsRequiredConfig.Value;
            if (frostRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_frost") >= frostRequirement)
            {
                return DraugrMinion.DraugrType.ArcherFrost;
            }

            var poisonRequirement = BasePlugin.ArcherPoisonArrowsRequiredConfig.Value;
            if (poisonRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_poison") >= poisonRequirement)
            {
                return DraugrMinion.DraugrType.ArcherPoison;
            }

            var ironRequirement = BasePlugin.ArcherTier3ArrowsRequiredConfig.Value;
            if (ironRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_iron") >= ironRequirement)
            {
                return DraugrMinion.DraugrType.ArcherTier3;
            }

            var bronzeRequirement = BasePlugin.ArcherTier2ArrowsRequiredConfig.Value;
            if (bronzeRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_bronze") >= bronzeRequirement)
            {
                return DraugrMinion.DraugrType.ArcherTier2;
            }

            var woodRequirement = BasePlugin.ArcherTier1ArrowsRequiredConfig.Value;
            if (woodRequirement <= 0
                || player.GetInventory().CountItems("$item_arrow_wood") >= woodRequirement)
            {
                return DraugrMinion.DraugrType.ArcherTier1;
            }

            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenougharrows");
            return DraugrMinion.DraugrType.None;
        }


        private void SpawnRangedDraugr()
        {
            if (!DraugrAllowed.Value) return;

            var player = Player.m_localPlayer;
            
            var meatTypesFound = LookForMeat();
            if (DraugrMeatRequiredConfig.Value > 0)
            {
                var meatInInventory = meatTypesFound.Item1;
                if (meatInInventory < DraugrMeatRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughmeat");
                    return;
                }
            }
            
            // check for bones
            if (DraugrBoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < DraugrBoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughbones");
                    return;
                }
            }
            
            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = ExtraResourceConsumptionUnlocked ? UndeadMinion.DetermineArmorType() : UndeadMinion.ArmorType.None;

            var draugrType = SpawnDraugrArcher();

            if (draugrType is DraugrMinion.DraugrType.None)
            {
                return;
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (DraugrMinion.MaxDraugr.Value > 0)
            {
                // re-count the current active draugr
                CountActiveDraugrMinions();
            }

            // scale according to skill
            int quality = DraugrTierOneQuality.Value;
            if (playerNecromancyLevel >= DraugrTierThreeLevelReq.Value)
            {
                quality = DraugrTierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= DraugrTierTwoLevelReq.Value)
            {
                quality = DraugrTierTwoQuality.Value;
            }

            ConsumeResources(draugrType, armorType, meatTypesFound.Item2);

            InstantiateDraugr(quality, playerNecromancyLevel, draugrType, armorType);
        }
        
        private DraugrMinion.DraugrType SpawnDraugrWarriorMinion(UndeadMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            // determine quality
            var needleRequirement = BasePlugin.NeedlesRequiredConfig.Value;
            if (needleRequirement <= 0
                || Player.m_localPlayer.GetInventory().CountItems("$item_needle") >= needleRequirement)
            {
                return DraugrMinion.DraugrType.WarriorNeedle;
            }
        
            return armorType switch
            {
                UndeadMinion.ArmorType.Leather => DraugrMinion.DraugrType.WarriorTier1,
                UndeadMinion.ArmorType.Bronze => DraugrMinion.DraugrType.WarriorTier2,
                UndeadMinion.ArmorType.Iron => DraugrMinion.DraugrType.WarriorTier3,
                UndeadMinion.ArmorType.BlackMetal => DraugrMinion.DraugrType.WarriorTier4,
                _ => DraugrMinion.DraugrType.WarriorTier1
            };
        }


        private Tuple<int, Dictionary<string, int>> LookForMeat()
        {
            Player player = Player.m_localPlayer;
            var meatTypesFound = new Dictionary<string, int>();
            var meatInInventory = 0;
            if (DraugrMeatRequiredConfig.Value > 0)
            {
                List<string> allowedMeatTypes = new List<string>()
                {
                    "$item_meat_rotten",
                    "$item_boar_meat",
                    "$item_necktail",
                    "$item_deer_meat",
                    "$item_loxmeat",
                    "$item_wolf_meat",
                    "$item_serpentmeat",
                    "$item_bug_meat",
                    "$item_chicken_meat",
                    "$item_hare_meat",
                };
                
                allowedMeatTypes.ForEach(meatTypeStr =>
                    {
                        int meatFound = player.GetInventory().CountItems(meatTypeStr);
                        if (meatFound > 0)
                        {
                            meatInInventory += meatFound;
                            meatTypesFound[meatTypeStr] = meatFound;
                        }
                    }
                );
            }

            return new Tuple<int, Dictionary<string, int>>(meatInInventory, meatTypesFound);
        }

        private void SpawnDraugr()
        {
            if (!DraugrAllowed.Value) return;
            
            var player = Player.m_localPlayer;

            var meatTypesFound = LookForMeat();
            if (DraugrMeatRequiredConfig.Value > 0)
            {
                var meatInInventory = meatTypesFound.Item1;
                if (meatInInventory < DraugrMeatRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughmeat");
                    return;
                }
            }
            
            // check for bones
            if (DraugrBoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < DraugrBoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughbones");
                    return;
                }
            }

            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = ExtraResourceConsumptionUnlocked ? UndeadMinion.DetermineArmorType() : UndeadMinion.ArmorType.None;

            var draugrType = SpawnDraugrWarriorMinion(armorType);

            if (draugrType is DraugrMinion.DraugrType.None)
                return;

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (DraugrMinion.MaxDraugr.Value > 0)
            {
                // re-count the current active draugr
                CountActiveDraugrMinions();
            }

            // scale according to skill
            int quality = DraugrTierOneQuality.Value;
            if (playerNecromancyLevel >= DraugrTierThreeLevelReq.Value)
            {
                quality = DraugrTierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= DraugrTierTwoLevelReq.Value)
            {
                quality = DraugrTierTwoQuality.Value;
            }

            ConsumeResources(draugrType, armorType, meatTypesFound.Item2);

            InstantiateDraugr(quality, playerNecromancyLevel, draugrType, armorType);
        }

        protected void InstantiateDraugr(int quality, float playerNecromancyLevel, DraugrMinion.DraugrType draugrType, UndeadMinion.ArmorType armorType)
        {
            if (draugrType is DraugrMinion.DraugrType.None) return;
            
            Player player = Player.m_localPlayer;
            // go on to spawn draugr
            string prefabName = InternalName.GetName(draugrType);
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"SpawnFriendlyDraugr: spawning {prefabName} failed");
            }

            GameObject spawnedChar = GameObject.Instantiate(prefab,
                player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            spawnedChar.AddComponent<FreshMinion>();
            DraugrMinion minion = spawnedChar.AddComponent<DraugrMinion>();
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);
            minion.ScaleStats(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, armorType);

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill,
                necromancyLevelIncrease.Value);

            if (FollowByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            minion.UndeadMinionMaster = player.GetPlayerName();

            // handle refunding of resources on death
            if (DraugrMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing)
            {
                CharacterDrop characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (DraugrMinion.DropOnDeath.Value == UndeadMinion.DropType.Everything)
                {
                    // bones
                    if (DraugrBoneFragmentsRequiredConfig.Value > 0)
                    {
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                            m_onePerPlayer = true,
                            m_amountMin = DraugrBoneFragmentsRequiredConfig.Value,
                            m_amountMax = DraugrBoneFragmentsRequiredConfig.Value,
                            m_chance = 1f
                        });
                    }

                    // meat. For now, assume Neck tails
                    if (DraugrMeatRequiredConfig.Value > 0)
                    {
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("NeckTail"),
                            m_onePerPlayer = true,
                            m_amountMin = DraugrMeatRequiredConfig.Value,
                            m_amountMax = DraugrMeatRequiredConfig.Value,
                            m_chance = 1f
                        });
                    }
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

        public int CountActiveDraugrMinions()
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

                DraugrMinion minion = item.GetComponent<DraugrMinion>();
                if (minion != null && minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
                {
                    minionsFound.Add(new Tuple<int, Character>(minion.createdOrder, item));
                }
            }

            // reverse so that we get newest first, oldest last. This means
            // when we kill off surplus, the oldest things are getting killed
            // not the newest things
            minionsFound = minionsFound.OrderByDescending((arg) => arg.Item1).ToList();

            var playerNecromancyLevel =
                Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var bonusMinions = DraugrMinion.MinionLimitIncrementsEveryXLevels.Value > 0
                ? (int)playerNecromancyLevel / DraugrMinion.MinionLimitIncrementsEveryXLevels.Value
                : 0;
            var maxMinions = DraugrMinion.MaxDraugr.Value + bonusMinions;
            
            for (int i = 0; i < minionsFound.Count; i++)
            {
                // kill off surplus
                if (result >= maxMinions - 1)
                {
                    Tuple<int, Character> tuple = minionsFound[i];
                    tuple.Item2.SetHealth(0);
                    continue;
                }

                result++;
            }

            return result;
        }
    }
}