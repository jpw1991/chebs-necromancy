using System.Collections.Generic;
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

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            // orb
            //
            // don't call base.CreateConfig because we want to omit the archer button

            Allowed = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningAllowed",
                true, new ConfigDescription("Whether crafting an Orb of Beckoning is allowed or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where it's available", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("OrbOfBeckoning (Server Synced)", "OrbOfBeckoningCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft it. None or Blank will use Default settings.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            // wand
            
            FollowByDefault = plugin.Config.Bind("Wands (Client)", "FollowByDefault",
                false, new ConfigDescription("Whether minions will automatically be set to follow upon being created or not."));
            
            FollowDistance = plugin.Config.Bind("Wands (Client)", "FollowDistance",
                3f, new ConfigDescription("How closely a minion will follow you (0 = standing on top of you, 3 = default)."));
            
            RunDistance = plugin.Config.Bind("Wands (Client)", "RunDistance",
                3f, new ConfigDescription("How close a following minion needs to be to you before it stops running and starts walking (0 = always running, 10 = default)."));

            CreateMinionConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"CreateMageMinion",
                KeyCode.B, new ConfigDescription("The key to create a mage minion with."));

            CreateMinionGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"CreateMageMinionGamepad",
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("The key to gamepad button to create a mage minion with."));

            FollowConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"Follow",
                KeyCode.F, new ConfigDescription("The key to tell minions to follow."));

            FollowGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"FollowGamepad",
                InputManager.GamepadButton.ButtonWest,
                new ConfigDescription("The gamepad button to tell minions to follow."));

            WaitConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"Wait",
                KeyCode.T, new ConfigDescription("The key to tell minions to wait."));

            WaitGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"WaitGamepad",
                InputManager.GamepadButton.ButtonEast,
                new ConfigDescription("The gamepad button to tell minions to wait."));

            TeleportConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"Teleport",
                KeyCode.G, new ConfigDescription("The key to teleport following minions to you."));

            TeleportGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"TeleportGamepad",
                InputManager.GamepadButton.SelectButton,
                new ConfigDescription("The gamepad button to teleport following minions to you."));
            
            UnlockExtraResourceConsumptionConfig = plugin.Config.Bind("Keybinds (Client)", ItemName + "UnlockExtraResourceConsumption",
                KeyCode.LeftShift, new ConfigDescription("The key to permit consumption of additional resources when creating the minion eg. iron to make an armored skeleton."));
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

            return customItem;
        }
        
        public override KeyHintConfig GetKeyHint()
        {
            var buttonConfigs = new List<ButtonConfig>();

            if (CreateMinionButton != null) buttonConfigs.Add(CreateMinionButton);
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
                    equippedItem => equippedItem.TokenName().Equals(NameLocalization)
                ) == null) return false;
            
            ExtraResourceConsumptionUnlocked =
                UnlockExtraResourceConsumptionButton == null
                || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

            if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
            {
                SpawnSkeleton();
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
        
        private SkeletonMinion.SkeletonType SpawnSkeletonMageMinion(UndeadMinion.ArmorType armorType)
        {
            // Determine type of minion to spawn and consume resources.
            // Return None if unable to determine minion type, or if necessary resources are missing.

            Player player = Player.m_localPlayer;
            
            // check for bones
            if (SkeletonWand.BoneFragmentsRequiredConfig.Value > 0)
            {
                int boneFragmentsInInventory = player.GetInventory().CountItems("$item_bonefragments");

                if (boneFragmentsInInventory < SkeletonWand.BoneFragmentsRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$friendlyskeletonwand_notenoughbones");
                    return SkeletonMinion.SkeletonType.None;
                }
            }

            // check for surtling cores
            if (BasePlugin.SurtlingCoresRequiredConfig.Value > 0)
            {
                int surtlingCoresInInventory = player.GetInventory().CountItems("$item_surtlingcore");
                if (surtlingCoresInInventory < BasePlugin.SurtlingCoresRequiredConfig.Value)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                        "$chebgonaz_notenoughcores");
                    return SkeletonMinion.SkeletonType.None;
                }
            }

            // determine quality

            if (armorType is not UndeadMinion.ArmorType.Bronze
                and not UndeadMinion.ArmorType.Iron
                and not UndeadMinion.ArmorType.BlackMetal)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    "$chebgonaz_magesrequirearmor");
            }

            // mages require bronze or better to be created
            return armorType switch
            {
                UndeadMinion.ArmorType.Bronze => SkeletonMinion.SkeletonType.MageTier1,
                UndeadMinion.ArmorType.Iron => SkeletonMinion.SkeletonType.MageTier2,
                UndeadMinion.ArmorType.BlackMetal => SkeletonMinion.SkeletonType.MageTier3,
                _ => SkeletonMinion.SkeletonType.None
            };
        }
        
        private void SpawnSkeleton()
        {
            if (!SkeletonWand.SkeletonsAllowed.Value) return;

            var player = Player.m_localPlayer;
            var playerNecromancyLevel =
                player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);
            var armorType = UndeadMinion.DetermineArmorType();

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
        
        public override void CreateButtons()
        {
            if (CreateMinionConfig.Value != KeyCode.None)
            {
                CreateMinionButton = new ButtonConfig
                {
                    Name = ItemName + "CreateMinion",
                    Config = CreateMinionConfig,
                    GamepadConfig = CreateMinionGamepadConfig,
                    HintToken = "$chebgonaz_orbofbeckoning_create",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, CreateMinionButton);
            }

            if (FollowConfig.Value != KeyCode.None)
            {
                FollowButton = new ButtonConfig
                {
                    Name = ItemName + "Follow",
                    Config = FollowConfig,
                    GamepadConfig = FollowGamepadConfig,
                    HintToken = "$friendlyskeletonwand_follow",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, FollowButton);
            }

            if (WaitConfig.Value != KeyCode.None)
            {
                WaitButton = new ButtonConfig
                {
                    Name = ItemName + "Wait",
                    Config = WaitConfig,
                    GamepadConfig = WaitGamepadConfig,
                    HintToken = "$friendlyskeletonwand_wait",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, WaitButton);
            }

            if (TeleportConfig.Value != KeyCode.None)
            {
                TeleportButton = new ButtonConfig
                {
                    Name = ItemName + "Teleport",
                    Config = TeleportConfig,
                    GamepadConfig = TeleportGamepadConfig,
                    HintToken = "$friendlyskeletonwand_teleport",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, TeleportButton);
            }
            
            if (UnlockExtraResourceConsumptionConfig.Value != KeyCode.None)
            {
                UnlockExtraResourceConsumptionButton = new ButtonConfig
                {
                    Name = ItemName + "UnlockExtraResourceConsumption",
                    Config = UnlockExtraResourceConsumptionConfig,
                    //GamepadConfig = AttackTargetGamepadConfig,
                    HintToken = "$friendlyskeletonwand_unlockextraresourceconsumption",
                    BlockOtherInputs = false
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, UnlockExtraResourceConsumptionButton);
            }
        }
    }
}