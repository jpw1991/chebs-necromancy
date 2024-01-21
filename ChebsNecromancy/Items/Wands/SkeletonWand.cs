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
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.Wands
{
    internal class SkeletonWand : Wand
    {
        #region ConfigEntries

        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;
        public static ConfigEntry<string> CraftingCost;

        public static ConfigEntry<bool> SkeletonsAllowed;
        
        public static ConfigEntry<int> SkeletonTierOneQuality;
        public static ConfigEntry<int> SkeletonTierTwoQuality;
        public static ConfigEntry<int> SkeletonTierTwoLevelReq;
        public static ConfigEntry<int> SkeletonTierThreeQuality;
        public static ConfigEntry<int> SkeletonTierThreeLevelReq;
        public static ConfigEntry<float> SkeletonSetFollowRange;
        
        public static ConfigEntry<float> SkeletonArmorValueMultiplier;

        #endregion

        public override string ItemName => "ChebGonaz_SkeletonWand";
        public override string PrefabName => "ChebGonaz_SkeletonWand.prefab";
        protected override string DefaultRecipe => "Wood:5,Stone:1";

        #region MinionSelector
        public enum MinionOption
        {
            Warrior,
            Archer,
            Poison,
            Woodcutter,
            Miner
        }

        private List<MinionOption> _minionOptions = new()
        {
            MinionOption.Warrior,
            MinionOption.Archer,
            MinionOption.Poison,
            MinionOption.Woodcutter,
            MinionOption.Miner,
        };

        private int _selectedMinionOptionIndex;
        private MinionOption SelectedMinionOption => _minionOptions[_selectedMinionOptionIndex];

        private TextMeshProUGUI _createMinionButtonText;
        
        #endregion

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            SkeletonSetFollowRange = plugin.Config.Bind($"{GetType().Name} (Client)", "SkeletonCommandRange",
                20f, new ConfigDescription("The distance which nearby skeletons will hear your commands."));

            Allowed = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonWandAllowed",
                true, new ConfigDescription("Whether crafting a Skeleton Wand is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonWandCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Skeleton Wand is available",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind($"{GetType().Name} (Server Synced)",
                "SkeletonWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Skeleton Wand", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonWandCraftingCosts",
                DefaultRecipe, new ConfigDescription(
                    "Materials needed to craft Skeleton Wand. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonsAllowed = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonsAllowed",
                true, new ConfigDescription("If false, skeletons can't be summoned.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierOneQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Skeleton minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierTwoQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Skeleton minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierTwoLevelReq = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Skeleton", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierThreeQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Skeleton minions", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonTierThreeLevelReq = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SkeletonTierThreeLevelReq",
                90, new ConfigDescription("Necromancy skill level required to summon Tier 3 Skeleton", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SkeletonArmorValueMultiplier = plugin.Config.Bind($"{GetType().Name} (Server Synced)",
                "SkeletonArmorValueMultiplier",
                1f, new ConfigDescription(
                    "If you find the armor value for skeletons to be too low, you can multiply it here. By default, a skeleton wearing iron armor will have an armor value of 42 (14+14+14). A multiplier of 1.5 will cause this armor value to increase to 63.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override void UpdateRecipe()
        {
            UpdateRecipe(CraftingStationRequired, CraftingCost, CraftingStationLevel);
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
            if (MessageHud.instance == null
                || Player.m_localPlayer == null
                || Player.m_localPlayer.GetInventory().GetEquippedItems().Find(
                    equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand")
                ) == null) return false;

            ExtraResourceConsumptionUnlocked =
                UnlockExtraResourceConsumptionButton == null
                || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

            // handle visual side of keyhints
            // if (TeleportButton != null && TeleportCooldown.Value > 0)
            // {
            //     TeleportButton.HintToken = Time.time - lastTeleport < TeleportCooldown.Value ? "Cooldown" : "$friendlyskeletonwand_teleport";
            // }

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
                        MinionOption.Miner => "$chebgonaz_miniontype_miner",
                        MinionOption.Woodcutter => "$chebgonaz_miniontype_woodcutter",
                        MinionOption.Warrior => "$chebgonaz_miniontype_warrior",
                        MinionOption.Poison => "$chebgonaz_miniontype_poison",
                        _ => "Error"
                    });
                    _createMinionButtonText.text = $"{createLocalized} {minionLocalized}";
                }

                if (ZInput.GetButton(CreateMinionButton.Name))
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
                    
                    switch (SelectedMinionOption)
                    {
                        case MinionOption.Warrior:
                            SpawnSkeleton(playerNecromancyLevel, SpawnSkeletonWarriorMinion(armorType), armorType);
                            break;
                        case MinionOption.Archer:
                            SpawnSkeleton(playerNecromancyLevel, SpawnSkeletonArcher(), armorType);
                            break;
                        case MinionOption.Poison:
                            SpawnSkeleton(playerNecromancyLevel, SpawnPoisonSkeletonMinion(playerNecromancyLevel, armorType), armorType);
                            break;
                        case MinionOption.Woodcutter:
                            SpawnSkeleton(playerNecromancyLevel, SpawnSkeletonWoodcutter(), armorType);
                            break;
                        case MinionOption.Miner:
                            SpawnSkeleton(playerNecromancyLevel, SpawnSkeletonMiner(), armorType);
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
                        MinionOption.Archer => "$chebgonaz_miniontype_archer",
                        MinionOption.Miner => "$chebgonaz_miniontype_miner",
                        MinionOption.Woodcutter => "$chebgonaz_miniontype_woodcutter",
                        MinionOption.Warrior => "$chebgonaz_miniontype_warrior",
                        MinionOption.Poison => "$chebgonaz_miniontype_poison",
                        _ => "Error"
                    });
                    _createMinionButtonText.text = $"{createLocalized} {minionLocalized}";
                }
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
            
            var inventory = Player.m_localPlayer.GetInventory();
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherSilverMinion.ItemsCost, inventory, out _))
                return SkeletonMinion.SkeletonType.ArcherSilver;
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherFireMinion.ItemsCost, inventory, out _))
                return SkeletonMinion.SkeletonType.ArcherFire;
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherFrostMinion.ItemsCost, inventory, out _))
                return SkeletonMinion.SkeletonType.ArcherFrost;
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherPoisonMinion.ItemsCost, inventory, out _))
                return SkeletonMinion.SkeletonType.ArcherPoison;
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherTier3Minion.ItemsCost, inventory, out _))
                return SkeletonMinion.SkeletonType.ArcherTier3;
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherTier2Minion.ItemsCost, inventory, out _))
                return SkeletonMinion.SkeletonType.ArcherTier2;
            
            if (ChebGonazMinion.CanSpawn(SkeletonArcherTier1Minion.ItemsCost, inventory, out var message))
                return SkeletonMinion.SkeletonType.ArcherTier1;

            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
            return SkeletonMinion.SkeletonType.None;
        }

        private SkeletonMinion.SkeletonType SpawnSkeletonWoodcutter()
        {
            // Return None if necessary resources are missing.
            var player = Player.m_localPlayer;

            if (!ChebGonazMinion.CanSpawn(SkeletonWoodcutterMinion.ItemsCost, player.GetInventory(), out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return SkeletonMinion.SkeletonType.None;
            }

            return SkeletonMinion.SkeletonType.Woodcutter;
        }

        private SkeletonMinion.SkeletonType SpawnSkeletonMiner()
        {
            // Return None if necessary resources are missing.
            var player = Player.m_localPlayer;

            if (!ChebGonazMinion.CanSpawn(SkeletonMinerMinion.ItemsCost, player.GetInventory(), out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return SkeletonMinion.SkeletonType.None;
            }

            return SkeletonMinion.SkeletonType.Miner;
        }

        private SkeletonMinion.SkeletonType SpawnPoisonSkeletonMinion(float playerNecromancyLevel,
            ChebGonazMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            var player = Player.m_localPlayer;
            
            if (playerNecromancyLevel < PoisonSkeletonMinion.LevelRequirementConfig.Value)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "$chebgonaz_necromancyleveltoolow");
                return SkeletonMinion.SkeletonType.None;   
            }
            
            if (!ChebGonazMinion.CanSpawn(PoisonSkeletonMinion.ItemsCost, player.GetInventory(), out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return SkeletonMinion.SkeletonType.None;
            }

            // determine quality
            return armorType switch
            {
                ChebGonazMinion.ArmorType.Leather => SkeletonMinion.SkeletonType.PoisonTier1,
                ChebGonazMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.PoisonTier2,
                ChebGonazMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.PoisonTier3,
                ChebGonazMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.PoisonTier3,
                _ => SkeletonMinion.SkeletonType.PoisonTier1
            };
        }

        private SkeletonMinion.SkeletonType SpawnSkeletonWarriorMinion(ChebGonazMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.
            
            var player = Player.m_localPlayer;
            
            if (!ChebGonazMinion.CanSpawn(SkeletonWarriorMinion.ItemsCost, player.GetInventory(), out var message))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
                return SkeletonMinion.SkeletonType.None;
            }

            // determine quality
            var needleRequirement = BasePlugin.NeedlesRequiredConfig.Value;
            if (needleRequirement <= 0
                || Player.m_localPlayer.GetInventory().CountItems("$item_needle") >= needleRequirement)
            {
                return SkeletonMinion.SkeletonType.WarriorNeedle;
            }
            
            return armorType switch
            {
                ChebGonazMinion.ArmorType.Leather => SkeletonMinion.SkeletonType.WarriorTier1,
                ChebGonazMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.WarriorTier2,
                ChebGonazMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.WarriorTier3,
                ChebGonazMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.WarriorTier4,
                _ => SkeletonMinion.SkeletonType.WarriorTier1
            };
        }

        private void SpawnSkeleton(float playerNecromancyLevel,
            SkeletonMinion.SkeletonType skeletonType, ChebGonazMinion.ArmorType armorType)
        {
            if (skeletonType == SkeletonMinion.SkeletonType.None) return;
            if (!SkeletonsAllowed.Value) return;

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