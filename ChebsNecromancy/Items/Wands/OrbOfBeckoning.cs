using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Minions.Skeletons;
using ChebsValheimLibrary.Items;
using ChebsValheimLibrary.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.Wands
{
    internal class OrbOfBeckoning : Wand
    {
        public override string ItemName => "ChebGonaz_OrbOfBeckoning";
        public override string PrefabName => "ChebGonaz_OrbOfBeckoning.prefab";
        public const string ProjectilePrefabName = "ChebGonaz_OrbOfBeckoningProjectile.prefab";
        public override string NameLocalization => "$item_chebgonaz_orbofbeckoning";
        public override string DescriptionLocalization => "$item_chebgonaz_orbofbeckoning_desc";
        
        protected override string DefaultRecipe => "Crystal:5,SurtlingCore:5,Tar:25";

        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;
        
        public static ConfigEntry<string> CraftingCost;
        
        #region MinionSelector
        public enum MinionOption
        {
            Mage,
            Leech,
        }

        private List<MinionOption> _minionOptions = new()
        {
            MinionOption.Mage,
            MinionOption.Leech,
        };

        private int _selectedMinionOptionIndex;
        private MinionOption SelectedMinionOption => _minionOptions[_selectedMinionOptionIndex];

        private TextMeshProUGUI _createMinionButtonText;
        
        #endregion

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            Allowed = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "OrbOfBeckoningAllowed",
                true, new ConfigDescription("Whether crafting an Orb of Beckoning is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "OrbOfBeckoningCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where it's available", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "OrbOfBeckoningCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "OrbOfBeckoningCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft it. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override void UpdateRecipe()
        {
            UpdateRecipe(CraftingStationRequired, CraftingCost, CraftingStationLevel);
        }
        
        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new();
            config.Name = NameLocalization;
            config.Description = DescriptionLocalization;

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

            CustomItem customItem = new (prefab, false, config);
            if (customItem.ItemPrefab == null)
            {
                Logger.LogError($"AddCustomItems: {PrefabName}'s ItemPrefab is null!");
                return null;
            }

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
            if (MessageHud.instance == null
                || Player.m_localPlayer == null
                || Player.m_localPlayer.GetInventory().GetEquippedItems().Find(
                    equippedItem => equippedItem.TokenName().Equals(NameLocalization)
                ) == null) return false;
            
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
                        MinionOption.Leech => "$chebgonaz_miniontype_leech",
                        MinionOption.Mage => "$chebgonaz_miniontype_mage",
                        _ => "Error"
                    });
                    _createMinionButtonText.text = $"{createLocalized} {minionLocalized}";
                }

                if (ZInput.GetButton(CreateMinionButton.Name))
                {
                    switch (SelectedMinionOption)
                    {
                        case MinionOption.Mage:
                            SpawnSkeleton();
                            break;
                        case MinionOption.Leech:
                            SpawnLeech();
                            break;
                    }
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
                        MinionOption.Leech => "$chebgonaz_miniontype_leech",
                        MinionOption.Mage => "$chebgonaz_miniontype_mage",
                        _ => "Error"
                    });
                    _createMinionButtonText.text = $"{createLocalized} {minionLocalized}";
                }
                return true;
            }

            if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
            {
                MakeNearbyMinionsFollow(SkeletonWand.SkeletonSetFollowRange.Value, true);
                return true;
            }

            if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
            {
                if (ExtraResourceConsumptionUnlocked)
                {
                    MakeNearbyMinionsRoam(SkeletonWand.SkeletonSetFollowRange.Value);
                }
                else
                {
                    MakeNearbyMinionsFollow(SkeletonWand.SkeletonSetFollowRange.Value, false);
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
        
        private SkeletonMinion.SkeletonType SpawnSkeletonMageMinion(ChebGonazMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            var inventory = Player.m_localPlayer.GetInventory();
            
            if (!ChebGonazMinion.CanSpawn(SkeletonMageMinion.ItemsCost, inventory, out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return SkeletonMinion.SkeletonType.None;
            }

            // determine quality

            if (armorType is not ChebGonazMinion.ArmorType.Bronze
                and not ChebGonazMinion.ArmorType.Iron
                and not ChebGonazMinion.ArmorType.BlackMetal)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "$chebgonaz_magesrequirearmor");
            }

            // mages require bronze or better to be created
            return armorType switch
            {
                ChebGonazMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.MageTier1,
                ChebGonazMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.MageTier2,
                ChebGonazMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.MageTier3,
                _ => SkeletonMinion.SkeletonType.None
            };
        }
        
        private void SpawnSkeleton()
        {
            if (!SkeletonWand.SkeletonsAllowed.Value) return;

            var player = Player.m_localPlayer;
            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = ChebGonazMinion.DetermineArmorType(
                player.GetInventory(),
                BasePlugin.ArmorBlackIronRequiredConfig.Value,
                BasePlugin.ArmorIronRequiredConfig.Value,
                BasePlugin.ArmorBronzeRequiredConfig.Value,
                BasePlugin.ArmorLeatherScrapsRequiredConfig.Value);

            var skeletonType = SpawnSkeletonMageMinion(armorType);

            if (skeletonType is SkeletonMinion.SkeletonType.None)
            {
                return;
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            var minionLimitIsSet = SkeletonMinion.MaxSkeletons.Value > 0; 
            if (minionLimitIsSet)
            {
                // re-count the current active skeletons
                UndeadMinion.CountActive<SkeletonMinion>(
                    SkeletonMinion.MinionLimitIncrementsEveryXLevels.Value, 
                    SkeletonMinion.MaxSkeletons.Value);
            }

            // scale according to skill
            int quality = SkeletonWand.SkeletonTierOneQuality.Value;
            if (playerNecromancyLevel >= SkeletonWand.SkeletonTierThreeLevelReq.Value)
            {
                quality = SkeletonWand.SkeletonTierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= SkeletonWand.SkeletonTierTwoLevelReq.Value)
            {
                quality = SkeletonWand.SkeletonTierTwoQuality.Value;
            }

            SkeletonMinion.ConsumeResources(skeletonType, armorType);

            SkeletonMinion.InstantiateSkeleton(quality, playerNecromancyLevel, skeletonType, armorType);
        }

        private LeechMinion.LeechType SpawnLeechMinion()
        {
            var player = Player.m_localPlayer;

            if (!ChebGonazMinion.CanSpawn(LeechMinion.ItemsCost, player.GetInventory(), out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return LeechMinion.LeechType.None;
            }

            return LeechMinion.LeechType.Leech;
        }
        
        private void SpawnLeech()
        {
            if (!LeechMinion.Allowed.Value) return;

            var player = Player.m_localPlayer;
            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);

            var leechType = SpawnLeechMinion();
            if (leechType is LeechMinion.LeechType.None)
            {
                return;
            }
            
            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            var minionLimitIsSet = LeechMinion.MaxLeeches.Value > 0; 
            if (minionLimitIsSet)
            {
                // re-count the current active Leechs
                UndeadMinion.CountActive<LeechMinion>(
                    LeechMinion.MinionLimitIncrementsEveryXLevels.Value, 
                    LeechMinion.MaxLeeches.Value);
            }

            // scale according to skill
            int quality = LeechMinion.LeechTierOneQuality.Value;
            if (playerNecromancyLevel >= LeechMinion.LeechTierThreeLevelReq.Value)
            {
                quality = LeechMinion.LeechTierThreeQuality.Value;
            }
            else if (playerNecromancyLevel >= LeechMinion.LeechTierTwoLevelReq.Value)
            {
                quality = LeechMinion.LeechTierTwoQuality.Value;
            }

            LeechMinion.ConsumeResources(leechType);

            LeechMinion.InstantiateLeech(quality, playerNecromancyLevel, leechType);
        }
    }
}