using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Items;
using ChebsValheimLibrary.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;
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
        
        #region MinionSelector
        public enum MinionOption
        {
            Warrior,
            Archer,
        }

        private List<MinionOption> _minionOptions = new()
        {
            MinionOption.Warrior,
            MinionOption.Archer,
        };

        private int _selectedMinionOptionIndex;
        private MinionOption SelectedMinionOption => _minionOptions[_selectedMinionOptionIndex];
        
        private Text _createMinionButtonText;
        
        #endregion

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            DraugrSetFollowRange = plugin.Config.Bind($"{GetType().Name} (Client)", "DraugrCommandRange",
                20f, new ConfigDescription("The range from which nearby Draugr will hear your command.", null));

            Allowed = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrWandAllowed",
                true, new ConfigDescription("Whether crafting a Draugr Wand is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrWandCraftingStation",
                CraftingTable.Forge, new ConfigDescription("Crafting station where Draugr Wand is available", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Draugr Wand", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrWandCraftingCosts",
                DefaultRecipe, new ConfigDescription(
                    "Materials needed to craft Draugr Wand. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrAllowed = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrAllowed",
                true, new ConfigDescription("If false, draugr aren't loaded at all and can't be summoned.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrBaseHealth = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrBaseHealth",
                80f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrHealthMultiplier = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrHealthMultiplier",
                2.5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierOneQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierTwoQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierTwoLevelReq = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Draugr", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierThreeQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierThreeLevelReq = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Draugr", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrMeatRequiredConfig = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrMeatRequired",
                2, new ConfigDescription("How many pieces of meat it costs to make a Draugr.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrBoneFragmentsRequiredConfig = plugin.Config.Bind($"{GetType().Name} (Server Synced)",
                "DraugrBoneFragmentsRequired",
                6, new ConfigDescription("How many bone fragments it costs to make a Draugr.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancyLevelIncrease = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "DraugrNecromancyLevelIncrease",
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
            if (NextMinionButton != null) buttonConfigs.Add(NextMinionButton);
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

                if (CreateMinionButton != null)
                {
                    // https://github.com/Valheim-Modding/Jotunn/issues/398
                    if (_createMinionButtonText == null)
                    {
                        var button = GameObject.Find(CreateMinionButton.Name);
                        if (button != null)
                        {
                            _createMinionButtonText = button.GetComponentInChildren<Text>();
                        }   
                    }
                    
                    if (_createMinionButtonText != null) _createMinionButtonText.text = $"Create {SelectedMinionOption}";
                
                    if (ZInput.GetButton(CreateMinionButton.Name))
                    {
                        switch (SelectedMinionOption)
                        {
                            case MinionOption.Warrior:
                                SpawnDraugr();
                                break;
                            case MinionOption.Archer:
                                SpawnRangedDraugr();
                                break;
                        }
                    
                        return true;
                    }
                }

                if (NextMinionButton != null && ZInput.GetButton(NextMinionButton.Name))
                {
                    _selectedMinionOptionIndex++;
                    if (_selectedMinionOptionIndex >= _minionOptions.Count) _selectedMinionOptionIndex = 0;
                    if (_createMinionButtonText != null) _createMinionButtonText.text = $"Create {SelectedMinionOption}";
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

        private void ConsumeResources(DraugrMinion.DraugrType draugrType, ChebGonazMinion.ArmorType armorType, Dictionary<string, int> meatTypesFound)
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
                case ChebGonazMinion.ArmorType.Leather:
                    // todo: expose these options to config
                    var leatherItemTypes = new List<string>()
                    {
                        "$item_leatherscraps",
                        "$item_deerhide",
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
                case ChebGonazMinion.ArmorType.LeatherTroll:
                    player.GetInventory().RemoveItem("$item_trollhide", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Bronze:
                    player.GetInventory().RemoveItem("$item_bronze", BasePlugin.ArmorBronzeRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Iron:
                    player.GetInventory().RemoveItem("$item_iron", BasePlugin.ArmorIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.BlackMetal:
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
            var armorType = ExtraResourceConsumptionUnlocked 
                ? ChebGonazMinion.DetermineArmorType(
                player.GetInventory(),
                BasePlugin.ArmorBlackIronRequiredConfig.Value,
                BasePlugin.ArmorIronRequiredConfig.Value,
                BasePlugin.ArmorBronzeRequiredConfig.Value,
                BasePlugin.ArmorLeatherScrapsRequiredConfig.Value)
                : ChebGonazMinion.ArmorType.None;

            var draugrType = SpawnDraugrArcher();

            if (draugrType is DraugrMinion.DraugrType.None)
            {
                return;
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            var minionLimitIsSet = DraugrMinion.MaxDraugr.Value > 0; 
            if (minionLimitIsSet)
            {
                // re-count the current active draugr
                UndeadMinion.CountActive<DraugrMinion>(
                    DraugrMinion.MinionLimitIncrementsEveryXLevels.Value, 
                    DraugrMinion.MaxDraugr.Value);
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
        
        private DraugrMinion.DraugrType SpawnDraugrWarriorMinion(ChebGonazMinion.ArmorType armorType)
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
                ChebGonazMinion.ArmorType.Leather => DraugrMinion.DraugrType.WarriorTier1,
                ChebGonazMinion.ArmorType.Bronze => DraugrMinion.DraugrType.WarriorTier2,
                ChebGonazMinion.ArmorType.Iron => DraugrMinion.DraugrType.WarriorTier3,
                ChebGonazMinion.ArmorType.BlackMetal => DraugrMinion.DraugrType.WarriorTier4,
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
            var armorType = ExtraResourceConsumptionUnlocked
                ? ChebGonazMinion.DetermineArmorType(
                    player.GetInventory(),
                    BasePlugin.ArmorBlackIronRequiredConfig.Value,
                    BasePlugin.ArmorIronRequiredConfig.Value,
                    BasePlugin.ArmorBronzeRequiredConfig.Value,
                    BasePlugin.ArmorLeatherScrapsRequiredConfig.Value)
                : ChebGonazMinion.ArmorType.None;

            var draugrType = SpawnDraugrWarriorMinion(armorType);

            if (draugrType is DraugrMinion.DraugrType.None)
                return;

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            var minionLimitIsSet = DraugrMinion.MaxDraugr.Value > 0; 
            if (minionLimitIsSet)
            {
                // re-count the current active draugr
                UndeadMinion.CountActive<DraugrMinion>(
                    DraugrMinion.MinionLimitIncrementsEveryXLevels.Value, 
                    DraugrMinion.MaxDraugr.Value);
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

        protected void InstantiateDraugr(int quality, float playerNecromancyLevel, DraugrMinion.DraugrType draugrType, ChebGonazMinion.ArmorType armorType)
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

            GameObject spawnedChar = Object.Instantiate(prefab,
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
            if (DraugrMinion.DropOnDeath.Value == ChebGonazMinion.DropType.Nothing) return;
            
            // we have to be a little bit cautious. It normally shouldn't exist yet, but maybe some other mod
            // added it? Who knows
            var characterDrop = minion.gameObject.GetComponent<CharacterDrop>();
            if (characterDrop == null)
            {
                characterDrop = minion.gameObject.AddComponent<CharacterDrop>();
            }

            if (DraugrMinion.DropOnDeath.Value == ChebGonazMinion.DropType.Everything)
            {
                // bones
                if (DraugrBoneFragmentsRequiredConfig.Value > 0)
                {
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "BoneFragments", DraugrBoneFragmentsRequiredConfig.Value);
                }

                // meat. For now, assume Neck tails
                if (DraugrMeatRequiredConfig.Value > 0)
                {
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "NeckTail", DraugrMeatRequiredConfig.Value);
                }
            }

            switch (armorType)
            {
                case ChebGonazMinion.ArmorType.Leather:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, 
                        Random.value > .5f ? "DeerHide" : "LeatherScraps", // flip a coin for deer or scraps
                        BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherTroll:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "TrollHide", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherWolf:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "WolfPelt", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.LeatherLox:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "LoxPelt", BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Bronze:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "Bronze", BasePlugin.ArmorBronzeRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.Iron:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "Iron", BasePlugin.ArmorIronRequiredConfig.Value);
                    break;
                case ChebGonazMinion.ArmorType.BlackMetal:
                    ChebGonazMinion.AddOrUpdateDrop(characterDrop, "BlackMetal", BasePlugin.ArmorBlackIronRequiredConfig.Value);
                    break;
            }

            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            minion.RecordDrops(characterDrop);
        }
    }
}