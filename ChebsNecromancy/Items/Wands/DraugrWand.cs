using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Draugr;
using ChebsValheimLibrary.Items;
using ChebsValheimLibrary.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.Wands
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
        #endregion

        public override string ItemName => "ChebGonaz_DraugrWand";
        public override string PrefabName => "ChebGonaz_DraugrWand.prefab";
        protected override string DefaultRecipe => "ElderBark:5,FineWood:5,Bronze:5,TrophyDraugr:1";
        
        #region MinionSelector
        public enum MinionOption
        {
            Warrior,
            Archer,
            BattleNeckro
        }

        private List<MinionOption> _minionOptions = new()
        {
            MinionOption.Warrior,
            MinionOption.Archer,
            MinionOption.BattleNeckro
        };

        private int _selectedMinionOptionIndex;
        private MinionOption SelectedMinionOption => _minionOptions[_selectedMinionOptionIndex];
        
        private TextMeshProUGUI _createMinionButtonText;
        
        #endregion

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            var serverSynced = $"{GetType().Name} (Server Synced)";
            var clientSynced = $"{GetType().Name} (Client)";
            
            DraugrSetFollowRange = plugin.Config.Bind(clientSynced, "DraugrCommandRange",
                20f, new ConfigDescription("The range from which nearby Draugr will hear your command.", null));

            Allowed = plugin.Config.Bind(serverSynced, "DraugrWandAllowed",
                true, new ConfigDescription("Whether crafting a Draugr Wand is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind(serverSynced, "DraugrWandCraftingStation",
                CraftingTable.Forge, new ConfigDescription("Crafting station where Draugr Wand is available", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind(serverSynced, "DraugrWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Draugr Wand", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind(serverSynced, "DraugrWandCraftingCosts",
                DefaultRecipe, new ConfigDescription(
                    "Materials needed to craft Draugr Wand. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrAllowed = plugin.Config.Bind(serverSynced, "DraugrAllowed",
                true, new ConfigDescription("If false, draugr aren't loaded at all and can't be summoned.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrBaseHealth = plugin.Config.Bind(serverSynced, "DraugrBaseHealth",
                80f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrHealthMultiplier = plugin.Config.Bind(serverSynced, "DraugrHealthMultiplier",
                2.5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierOneQuality = plugin.Config.Bind(serverSynced, "DraugrTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierTwoQuality = plugin.Config.Bind(serverSynced, "DraugrTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierTwoLevelReq = plugin.Config.Bind(serverSynced, "DraugrTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Draugr", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierThreeQuality = plugin.Config.Bind(serverSynced, "DraugrTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Draugr minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DraugrTierThreeLevelReq = plugin.Config.Bind(serverSynced, "DraugrTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Draugr", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override void UpdateRecipe()
        {
            UpdateRecipe(CraftingStationRequired, CraftingCost, CraftingStationLevel);
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            var config = new ItemConfig();
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

            var customItem = new CustomItem(prefab, false, config);
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
            var buttonConfigs = new List<ButtonConfig>();

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
                && Player.m_localPlayer.GetInventory().GetEquippedItems().Find(
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
                            _createMinionButtonText = button.GetComponentInChildren<TextMeshProUGUI>();
                        }   
                    }

                    if (_createMinionButtonText != null)
                    {
                        var createLocalized = BasePlugin.Localization.TryTranslate("$chebgonaz_wand_create");
                        var minionLocalized = BasePlugin.Localization.TryTranslate(SelectedMinionOption switch
                        {
                            MinionOption.Archer => "$chebgonaz_miniontype_archer",
                            MinionOption.Warrior => "$chebgonaz_miniontype_warrior",
                            MinionOption.BattleNeckro => "$chebgonaz_miniontype_battleneckro",
                            _ => "Error"
                        });
                        _createMinionButtonText.text = $"{createLocalized} {minionLocalized}";
                    }
                
                    if (ZInput.GetButton(CreateMinionButton.Name))
                    {
                        SpawnMinion(SelectedMinionOption);
                        return true;
                    }
                }

                if (NextMinionButton != null && ZInput.GetButton(NextMinionButton.Name))
                {
                    _selectedMinionOptionIndex++;
                    if (_selectedMinionOptionIndex >= _minionOptions.Count) _selectedMinionOptionIndex = 0;
                    if (_createMinionButtonText != null)
                    {
                        var createLocalized = BasePlugin.Localization.TryTranslate("$chebgonaz_wand_create");
                        var minionLocalized = BasePlugin.Localization.TryTranslate(SelectedMinionOption switch
                        {
                            MinionOption.Archer => "$chebgonaz_miniontype_archer",
                            MinionOption.Warrior => "$chebgonaz_miniontype_warrior",
                            MinionOption.BattleNeckro => "$chebgonaz_miniontype_battleneckro",
                            _ => "Error"
                        });
                        _createMinionButtonText.text = $"{createLocalized} {minionLocalized}";
                    }
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

        private void SpawnMinion(MinionOption minionOption)
        {
            var playerNecromancyLevel =
                Player.m_localPlayer.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = ExtraResourceConsumptionUnlocked
                ? ChebGonazMinion.DetermineArmorType(
                    Player.m_localPlayer.GetInventory(),
                    BasePlugin.ArmorBlackIronRequiredConfig.Value,
                    BasePlugin.ArmorIronRequiredConfig.Value,
                    BasePlugin.ArmorBronzeRequiredConfig.Value,
                    BasePlugin.ArmorLeatherScrapsRequiredConfig.Value)
                : ChebGonazMinion.ArmorType.None;

            switch (minionOption)
            {
                case MinionOption.Archer:
                    SpawnDraugr(playerNecromancyLevel, SpawnDraugrArcher(), armorType);
                    break;
                
                case MinionOption.Warrior:
                    SpawnDraugr(playerNecromancyLevel, SpawnDraugrWarriorMinion(armorType), armorType);
                    break;
                
                case MinionOption.BattleNeckro:
                    SpawnBattleNeckro();
                    break;
            }
        }
        
        private void SpawnDraugr(float playerNecromancyLevel,
            DraugrMinion.DraugrType draugrType, ChebGonazMinion.ArmorType armorType)
        {
            if (draugrType == DraugrMinion.DraugrType.None) return;
            if (!DraugrAllowed.Value) return;

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            var minionLimitIsSet = DraugrMinion.MaxDraugr.Value > 0;
            if (minionLimitIsSet)
            {
                // re-count the current active skeletons
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

            DraugrMinion.ConsumeResources(draugrType, armorType);

            DraugrMinion.InstantiateDraugr(quality, playerNecromancyLevel, draugrType, armorType);
        }
        
        private DraugrMinion.DraugrType SpawnDraugrArcher()
        {
            // Determine type of archer to spawn and consume resources.
            // Return None if unable to determine archer type, or if necessary resources are missing.
            
            var inventory = Player.m_localPlayer.GetInventory();
            
            if (UndeadMinion.CanSpawn(DraugrArcherSilverMinion.ItemsCost, inventory, out _))
                return DraugrMinion.DraugrType.ArcherSilver;
            
            if (UndeadMinion.CanSpawn(DraugrArcherFireMinion.ItemsCost, inventory, out _))
                return DraugrMinion.DraugrType.ArcherFire;
            
            if (UndeadMinion.CanSpawn(DraugrArcherFrostMinion.ItemsCost, inventory, out _))
                return DraugrMinion.DraugrType.ArcherFrost;
            
            if (UndeadMinion.CanSpawn(DraugrArcherPoisonMinion.ItemsCost, inventory, out _))
                return DraugrMinion.DraugrType.ArcherPoison;
            
            if (UndeadMinion.CanSpawn(DraugrArcherTier3Minion.ItemsCost, inventory, out _))
                return DraugrMinion.DraugrType.ArcherTier3;
            
            if (UndeadMinion.CanSpawn(DraugrArcherTier2Minion.ItemsCost, inventory, out _))
                return DraugrMinion.DraugrType.ArcherTier2;
            
            if (UndeadMinion.CanSpawn(DraugrArcherTier1Minion.ItemsCost, inventory, out var message))
                return DraugrMinion.DraugrType.ArcherTier1;

            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
            return DraugrMinion.DraugrType.None;
        }

        private DraugrMinion.DraugrType SpawnDraugrWarriorMinion(ChebGonazMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            var inventory = Player.m_localPlayer.GetInventory();
            
            if (!UndeadMinion.CanSpawn(DraugrWarriorMinion.ItemsCost, inventory, out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return DraugrMinion.DraugrType.None;
            }

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
        
        private void SpawnBattleNeckro()
        {
            if (!BattleNeckroMinion.Allowed.Value) return;
            
            var player = Player.m_localPlayer;

            if (!UndeadMinion.CanSpawn(BattleNeckroMinion.ItemsCost, player.GetInventory(), out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return;
            }

            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            var minionLimitIsSet = BattleNeckroMinion.MaxBattleNeckros.Value > 0; 
            if (minionLimitIsSet)
            {
                // re-count the current active draugr
                UndeadMinion.CountActive<BattleNeckroMinion>(
                    BattleNeckroMinion.MinionLimitIncrementsEveryXLevels.Value, 
                    BattleNeckroMinion.MaxBattleNeckros.Value);
            }

            // scale according to skill
            var quality = BattleNeckroMinion.TierOneQuality.Value;
            if (playerNecromancyLevel >= BattleNeckroMinion.TierThreeLevelReq.Value)
            {
                quality = BattleNeckroMinion.TierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= BattleNeckroMinion.TierTwoLevelReq.Value)
            {
                quality = BattleNeckroMinion.TierTwoQuality.Value;
            }

            BattleNeckroMinion.ConsumeResources();

            BattleNeckroMinion.InstantiateBattleNeckro(quality, playerNecromancyLevel);
        }
    }
}