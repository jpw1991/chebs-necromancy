using BepInEx.Configuration;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ChebsNecromancy.Minions;

namespace ChebsNecromancy
{
    internal class DraugrWand : Wand
    {
        public static ConfigEntry<int> maxDraugr;

        public static ConfigEntry<CraftingTable> craftingStationRequired;
        public static ConfigEntry<int> craftingStationLevel;

        public static ConfigEntry<string> craftingCost;

        public static ConfigEntry<bool> draugrAllowed;

        public static ConfigEntry<float> draugrBaseHealth;
        public static ConfigEntry<float> draugrHealthMultiplier;
        public static ConfigEntry<int> draugrTierOneQuality;
        public static ConfigEntry<int> draugrTierTwoQuality;
        public static ConfigEntry<int> draugrTierTwoLevelReq;
        public static ConfigEntry<int> draugrTierThreeQuality;
        public static ConfigEntry<int> draugrTierThreeLevelReq;
        public static ConfigEntry<float> draugrSetFollowRange;

        public static ConfigEntry<bool> durabilityDamage;
        public static ConfigEntry<float> durabilityDamageWarrior;
        public static ConfigEntry<float> durabilityDamageArcher;
        public static ConfigEntry<float> durabilityDamageLeather;
        public static ConfigEntry<float> durabilityDamageBronze;
        public static ConfigEntry<float> durabilityDamageIron;
        public static ConfigEntry<float> durabilityDamageBlackIron;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> draugrBoneFragmentsRequiredConfig;
        public static ConfigEntry<int> draugrMeatRequiredConfig;

        public override string ItemName { get { return "ChebGonaz_DraugrWand"; } }
        public override string PrefabName { get { return "ChebGonaz_DraugrWand.prefab"; } }
        protected override string DefaultRecipe { get { return "ElderBark:5,FineWood:5,Bronze:5,TrophyDraugr:1"; } }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            draugrSetFollowRange = plugin.Config.Bind("DraugrWand (Client)", "DraugrCommandRange",
                10f, new ConfigDescription("The range from which nearby Draugr will hear your command.", null));

            allowed = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandAllowed",
                true, new ConfigDescription("Whether crafting a Draugr Wand is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingStationRequired = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandCraftingStation",
                CraftingTable.Forge, new ConfigDescription("Crafting station where Draugr Wand is available", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingStationLevel = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Draugr Wand", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingCost = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrWandCraftingCosts",
               DefaultRecipe, new ConfigDescription("Materials needed to craft Draugr Wand. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrAllowed = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrAllowed",
                true, new ConfigDescription("If false, draugr aren't loaded at all and can't be summoned.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrBaseHealth = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrBaseHealth",
                100f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrHealthMultiplier = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrHealthMultiplier",
                5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrTierOneQuality = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierOneQuality",
                1, new ConfigDescription("Star Quality of tier 1 Draugr minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrTierTwoQuality = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 Draugr minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrTierTwoLevelReq = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 Draugr", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrTierThreeQuality = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 Draugr minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrTierThreeLevelReq = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 Draugr", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrMeatRequiredConfig = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrMeatRequired",
                1, new ConfigDescription("How many pieces of meat it costs to make a Draugr.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            draugrBoneFragmentsRequiredConfig = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrBoneFragmentsRequired",
                3, new ConfigDescription("How many bone fragments it costs to make a Draugr.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancyLevelIncrease = plugin.Config.Bind("DraugrWand (Server Synced)", "DraugrNecromancyLevelIncrease",
                1.5f, new ConfigDescription("How much creating a Draugr contributes to your Necromancy level increasing.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            maxDraugr = plugin.Config.Bind("DraugrWand (Server Synced)", "MaximumDraugr",
                0, new ConfigDescription("The maximum Draugr allowed to be created (0 = unlimited).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamage = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamage",
                true, new ConfigDescription("Whether using a Draugr Wand damages its durability.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageWarrior = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamageWarrior",
                1f, new ConfigDescription("How much creating a warrior damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageArcher = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamageArcher",
                3f, new ConfigDescription("How much creating an archer damages the wand.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageLeather = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamageLeather",
                1f, new ConfigDescription("How much armoring the minion in leather damages the wand (value is added on top of damage from minion type).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageBronze = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamageBronze",
                1f, new ConfigDescription("How much armoring the minion in bronze damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageIron = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamageIron",
                1f, new ConfigDescription("How much armoring the minion in iron damages the wand (value is added on top of damage from minion type)", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            durabilityDamageBlackIron = plugin.Config.Bind("DraugrWand (Server Synced)", "DurabilityDamageBlackIron",
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
            config.Name = "$item_friendlyskeletonwand_draugrwand";
            config.Description = "$item_friendlyskeletonwand_draugrwand_desc";

            if (allowed.Value)
            {
                if (string.IsNullOrEmpty(craftingCost.Value))
                {
                    craftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                SetRecipeReqs(
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
                        equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_draugrwand")
                        ) != null
                    )
            {
                ExtraResourceConsumptionUnlocked =
                    UnlockExtraResourceConsumptionButton == null
                    || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnFriendlyDraugr(Player.m_localPlayer,
                        draugrBoneFragmentsRequiredConfig.Value,
                        draugrMeatRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        false
                        );
                    return true;
                }
                else if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnFriendlyDraugr(Player.m_localPlayer,
                        draugrBoneFragmentsRequiredConfig.Value,
                        draugrMeatRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        true
                        );
                    return true;
                }
                else if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, draugrSetFollowRange.Value, true);
                    return true;
                }
                else if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, draugrSetFollowRange.Value, false);
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

        public void SpawnFriendlyDraugr(Player player, int boneFragmentsRequired, int meatRequired, float necromancyLevelIncrease, bool archer)
        {
            if (!draugrAllowed.Value) return;

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

            if (meatRequired > 0)
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
                Dictionary<string, int> meatTypesFound = new Dictionary<string, int>();
                int meatInInventory = 0;
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

                if (meatInInventory < meatRequired)
                {
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notenoughmeat");
                    return;
                }

                // consume the fragments
                player.GetInventory().RemoveItem("$item_bonefragments", boneFragmentsRequired);

                // consume the meat
                int meatConsumed = 0;
                Stack<Tuple<string, int>> meatToConsume = new Stack<Tuple<string, int>>();
                foreach (string key in meatTypesFound.Keys)
                {
                    if (meatConsumed >= meatRequired) { break; }

                    int meatAvailable = meatTypesFound[key];

                    if (meatAvailable <= meatRequired)
                    {
                        meatToConsume.Push(new Tuple<string, int>(key, meatAvailable));
                        meatConsumed += meatAvailable;
                    }
                    else
                    {
                        meatToConsume.Push(new Tuple<string, int>(key, meatRequired));
                        meatConsumed += meatRequired;
                    }
                }

                while (meatToConsume.Count > 0)
                {
                    Tuple<string, int> keyValue = meatToConsume.Pop();
                    player.GetInventory().RemoveItem(keyValue.Item1, keyValue.Item2);
                }
            }

            bool createArmoredLeather = false;
            if (ExtraResourceConsumptionUnlocked && SkeletonWand.armorLeatherScrapsRequiredConfig.Value > 0)
            {
                int leatherScrapsInInventory = player.GetInventory().CountItems("$item_leatherscraps");
                if (leatherScrapsInInventory >= SkeletonWand.armorLeatherScrapsRequiredConfig.Value)
                {
                    createArmoredLeather = true;
                    player.GetInventory().RemoveItem("$item_leatherscraps", SkeletonWand.armorLeatherScrapsRequiredConfig.Value);
                }
                else
                {
                    // no leather scraps? Try some deer hide
                    int deerHideInInventory = player.GetInventory().CountItems("$item_deerhide");
                    if (deerHideInInventory >= SkeletonWand.armorLeatherScrapsRequiredConfig.Value)
                    {
                        createArmoredLeather = true;
                        player.GetInventory().RemoveItem("$item_deerhide", SkeletonWand.armorLeatherScrapsRequiredConfig.Value);
                    }
                }
            }

            bool createArmoredBronze = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && SkeletonWand.armorBronzeRequiredConfig.Value > 0)
            {
                int bronzeInInventory = player.GetInventory().CountItems("$item_bronze");
                if (bronzeInInventory >= SkeletonWand.armorBronzeRequiredConfig.Value)
                {
                    createArmoredBronze = true;
                    player.GetInventory().RemoveItem("$item_bronze", SkeletonWand.armorBronzeRequiredConfig.Value);
                }
            }

            bool createArmoredIron = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && !createArmoredBronze && SkeletonWand.armorIronRequiredConfig.Value > 0)
            {
                int ironInInventory = player.GetInventory().CountItems("$item_iron");
                if (ironInInventory >= SkeletonWand.armorIronRequiredConfig.Value)
                {
                    createArmoredIron = true;
                    player.GetInventory().RemoveItem("$item_iron", SkeletonWand.armorIronRequiredConfig.Value);
                }
            }

            bool createArmoredBlackIron = false;
            if (ExtraResourceConsumptionUnlocked
                && !createArmoredLeather
                && !createArmoredBronze
                && !createArmoredIron
                && SkeletonWand.armorBlackIronRequiredConfig.Value > 0)
            {
                int blackIronInInventory = player.GetInventory().CountItems("$item_blackmetal");
                if (blackIronInInventory >= SkeletonWand.armorBlackIronRequiredConfig.Value)
                {
                    createArmoredBlackIron = true;
                    player.GetInventory().RemoveItem("$item_blackmetal", SkeletonWand.armorBlackIronRequiredConfig.Value);
                }
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (maxDraugr.Value > 0)
            {
                // re-count the current active draugr
                CountActiveDraugrMinions();
            }

            // scale according to skill
            float playerNecromancyLevel = 1;
            try
            {
                playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError($"Failed to get player necromancy level: {e}");
            }

            int quality = draugrTierOneQuality.Value;
            if (playerNecromancyLevel >= draugrTierThreeLevelReq.Value) { quality = draugrTierThreeQuality.Value; }
            else if (playerNecromancyLevel >= draugrTierTwoLevelReq.Value) { quality = draugrTierTwoQuality.Value; }

            // go on to spawn draugr
            string prefabName = archer ? "ChebGonaz_DraugrArcher" : "ChebGonaz_DraugrWarrior";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Jotunn.Logger.LogError($"SpawnFriendlyDraugr: spawning {prefabName} failed");
            }

            GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            DraugrMinion minion = spawnedChar.AddComponent<DraugrMinion>();
            Character character = spawnedChar.GetComponent<Character>();
            character.m_faction = Character.Faction.Players;
            character.SetLevel(quality);
            minion.ScaleStats(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, createArmoredLeather, createArmoredBronze, createArmoredIron, createArmoredBlackIron);

            try
            {
                player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill, necromancyLevelIncrease);
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError($"Failed to raise player necromancy level: {e}");
            }

            if (followByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            minion.SetUndeadMinionMaster(player.GetPlayerName());

            if (DraugrWand.durabilityDamage.Value)
            {
                Player.m_localPlayer.GetRightItem().m_durability -= archer
                    ? durabilityDamageArcher.Value : durabilityDamageWarrior.Value ;
            }

            // handle refunding of resources on death
            if (DraugrMinion.dropOnDeath.Value != UndeadMinion.DropType.Nothing)
            {
                CharacterDrop characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (DraugrMinion.dropOnDeath.Value == UndeadMinion.DropType.Everything)
                {
                    // bones
                    if (boneFragmentsRequired > 0)
                    {
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("BoneFragments"),
                            m_onePerPlayer = true,
                            m_amountMin = boneFragmentsRequired,
                            m_amountMax = boneFragmentsRequired,
                            m_chance = 1f
                        });
                    }

                    // meat. For now, assume Neck tails
                    if (meatRequired > 0)
                    {
                        characterDrop.m_drops.Add(new CharacterDrop.Drop
                        {
                            m_prefab = ZNetScene.instance.GetPrefab("NeckTail"),
                            m_onePerPlayer = true,
                            m_amountMin = meatRequired,
                            m_amountMax = meatRequired,
                            m_chance = 1f
                        });
                    }
                }
                if (createArmoredLeather)
                {
                    // for now, assume deerhide
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("DeerHide"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.armorLeatherScrapsRequiredConfig.Value,
                        m_amountMax = SkeletonWand.armorLeatherScrapsRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (createArmoredBronze)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Bronze"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.armorBronzeRequiredConfig.Value,
                        m_amountMax = SkeletonWand.armorBronzeRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (createArmoredIron)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Iron"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.armorIronRequiredConfig.Value,
                        m_amountMax = SkeletonWand.armorIronRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (createArmoredBlackIron)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BlackMetal"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.armorBlackIronRequiredConfig.Value,
                        m_amountMax = SkeletonWand.armorBlackIronRequiredConfig.Value,
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

            for (int i = 0; i < minionsFound.Count; i++)
            {
                // kill off surplus
                if (result >= maxDraugr.Value - 1)
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
