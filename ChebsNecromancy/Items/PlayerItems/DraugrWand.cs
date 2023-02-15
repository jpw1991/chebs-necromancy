using BepInEx.Configuration;
using ChebsNecromancy.Common;
using ChebsNecromancy.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.PlayerItems
{
    internal class DraugrWand : Wand
    {
        #region ConfigEntries
        public static ConfigEntry<int> MaxDraugr;
        public static ConfigEntry<bool> DraugrAllowed;

        public static ConfigEntry<float> DraugrBaseHealth;
        public static ConfigEntry<float> DraugrHealthMultiplier;
        public static ConfigEntry<int> DraugrTierOneQuality;
        public static ConfigEntry<int> DraugrTierTwoQuality;
        public static ConfigEntry<int> DraugrTierTwoLevelReq;
        public static ConfigEntry<int> DraugrTierThreeQuality;
        public static ConfigEntry<int> DraugrTierThreeLevelReq;
        public static ConfigEntry<float> DraugrSetFollowRange;

        public static ConfigEntry<bool> DurabilityDamage;
        public static ConfigEntry<float> DurabilityDamageWarrior;
        public static ConfigEntry<float> DurabilityDamageArcher;
        public static ConfigEntry<float> DurabilityDamageLeather;
        public static ConfigEntry<float> DurabilityDamageBronze;
        public static ConfigEntry<float> DurabilityDamageIron;
        public static ConfigEntry<float> DurabilityDamageBlackIron;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> DraugrBoneFragmentsRequiredConfig;
        public static ConfigEntry<int> DraugrMeatRequiredConfig;
        #endregion
        public override void CreateConfigs(BasePlugin plugin)
        {

            ChebsRecipeConfig.DefaultRecipe = "ElderBark:5,FineWood:5,Bronze:5,TrophyDraugr:1";
            ChebsRecipeConfig.RecipeName = "$item_friendlyskeletonwand_DraugrWand";
            ChebsRecipeConfig.ItemName = "ChebGonaz_DraugrWand";
            ChebsRecipeConfig.RecipeDescription = "$item_friendlyskeletonwand_draugrWand_desc";
            ChebsRecipeConfig.PrefabName = "ChebGonaz_DraugrWand.prefab";
            ChebsRecipeConfig.ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name;

            base.CreateConfigs(plugin);

            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "Allowed",
                true, "Whether crafting a Draugr Wand is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingStationRequired = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingStation",
                ChebsRecipe.EcraftingTable.Forge, "Crafting station where Draugr Wand is available", null, true);

            ChebsRecipeConfig.CraftingStationLevel = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingStationLevel",
                1, "Crafting station level required to craft Draugr Wand", plugin.IntQuantityValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingCosts",
               ChebsRecipeConfig.DefaultRecipe, "Materials needed to craft Draugr Wand. None or Blank will use Default settings.", null, true);

            DraugrSetFollowRange = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrCommandRange",
                10f, "The range from which nearby Draugr will hear your command.", plugin.FloatQuantityValue);

            DraugrAllowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrAllowed",
                true, "If false, draugr aren't loaded at all and can't be summoned.", null, true);

            DraugrBaseHealth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrBaseHealth",
                100f, "HP = BaseHealth + NecromancyLevel * HealthMultiplier", plugin.FloatQuantityValue, true);

            DraugrHealthMultiplier = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrHealthMultiplier",
                5f, "HP = BaseHealth + NecromancyLevel * HealthMultiplier", plugin.FloatQuantityValue, true);

            DraugrTierOneQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrTierOneQuality",
                1, "Star Quality of tier 1 Draugr minions", plugin.IntQuantityValue, true);

            DraugrTierTwoQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrTierTwoQuality",
                2, "Star Quality of tier 2 Draugr minions", plugin.IntQuantityValue, true);

            DraugrTierTwoLevelReq = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrTierTwoLevelReq",
                35, "Necromancy skill level required to summon Tier 2 Draugr", plugin.IntQuantityValue, true);

            DraugrTierThreeQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrTierThreeQuality",
                3, "Star Quality of tier 3 Draugr minions", plugin.IntQuantityValue, true);

            DraugrTierThreeLevelReq = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrTierThreeLevelReq",
                70, "Necromancy skill level required to summon Tier 3 Draugr", plugin.IntQuantityValue, true);

            DraugrMeatRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrMeatRequired",
                1, "How many pieces of meat it costs to make a Draugr.", plugin.IntQuantityValue, true);

            DraugrBoneFragmentsRequiredConfig = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrBoneFragmentsRequired",
                3, "How many bone fragments it costs to make a Draugr.", plugin.IntQuantityValue, true);

            necromancyLevelIncrease = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DraugrNecromancyLevelIncrease",
                1.5f, "How much creating a Draugr contributes to your Necromancy level increasing.", plugin.FloatQuantityValue, true);

            MaxDraugr = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "MaximumDraugr",
                0, "The maximum Draugr allowed to be created (0 = unlimited).", plugin.IntQuantityValue, true);

            DurabilityDamage = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamage",
                true, "Whether using a Draugr Wand damages its durability.", null, true);

            DurabilityDamageWarrior = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageWarrior",
                1f, "How much creating a warrior damages the wand.", plugin.FloatQuantityValue, true);

            DurabilityDamageArcher = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "DurabilityDamageArcher",
                3f, "How much creating an archer damages the wand.", plugin.FloatQuantityValue, true);

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

        public override void CreateButtons()
        {
            // call the base to add the basic generic buttons -> create, attack, follow, wait, etc.
            base.CreateButtons();

            // add any extra buttons

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
                Item = ChebsRecipeConfig.ItemName,
                ButtonConfigs = buttonConfigs.ToArray()
            };
        }

        public override bool HandleInputs()
        {
            if (MessageHud.instance != null
                    && Player.m_localPlayer != null
                    && Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                        equippedItem => equippedItem.TokenName().Equals(ChebsRecipeConfig.RecipeName)
                        ) != null
                    )
            {
                ExtraResourceConsumptionUnlocked =
                    UnlockExtraResourceConsumptionButton == null
                    || ZInput.GetButton(UnlockExtraResourceConsumptionButton.Name);

                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnFriendlyDraugr(Player.m_localPlayer,
                        DraugrBoneFragmentsRequiredConfig.Value,
                        DraugrMeatRequiredConfig.Value,
                        false
                        );
                    return true;
                }
                else if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnFriendlyDraugr(Player.m_localPlayer,
                        DraugrBoneFragmentsRequiredConfig.Value,
                        DraugrMeatRequiredConfig.Value,
                        true
                        );
                    return true;
                }
                else if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, DraugrSetFollowRange.Value, true);
                    return true;
                }
                else if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    if (ExtraResourceConsumptionUnlocked)
                    {
                        MakeNearbyMinionsRoam(Player.m_localPlayer, DraugrSetFollowRange.Value);
                    }
                    else
                    {
                        MakeNearbyMinionsFollow(Player.m_localPlayer, DraugrSetFollowRange.Value, false);
                    }
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

        public void SpawnFriendlyDraugr(Player player, int boneFragmentsRequired, int meatRequired, bool archer)
        {
            if (!DraugrAllowed.Value) return;

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
                List<string> allowedMeatTypes = new()
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
                Dictionary<string, int> meatTypesFound = new();
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
                Stack<Tuple<string, int>> meatToConsume = new();
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
            if (ExtraResourceConsumptionUnlocked && SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value > 0)
            {
                int leatherScrapsInInventory = player.GetInventory().CountItems("$item_leatherscraps");
                if (leatherScrapsInInventory >= SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value)
                {
                    createArmoredLeather = true;
                    player.GetInventory().RemoveItem("$item_leatherscraps", SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value);
                }
                else
                {
                    // no leather scraps? Try some deer hide
                    int deerHideInInventory = player.GetInventory().CountItems("$item_deerhide");
                    if (deerHideInInventory >= SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value)
                    {
                        createArmoredLeather = true;
                        player.GetInventory().RemoveItem("$item_deerhide", SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value);
                    }
                }
            }

            bool createArmoredBronze = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && SkeletonWand.ArmorBronzeRequiredConfig.Value > 0)
            {
                int bronzeInInventory = player.GetInventory().CountItems("$item_bronze");
                if (bronzeInInventory >= SkeletonWand.ArmorBronzeRequiredConfig.Value)
                {
                    createArmoredBronze = true;
                    player.GetInventory().RemoveItem("$item_bronze", SkeletonWand.ArmorBronzeRequiredConfig.Value);
                }
            }

            bool createArmoredIron = false;
            if (ExtraResourceConsumptionUnlocked && !createArmoredLeather && !createArmoredBronze && SkeletonWand.ArmorIronRequiredConfig.Value > 0)
            {
                int ironInInventory = player.GetInventory().CountItems("$item_iron");
                if (ironInInventory >= SkeletonWand.ArmorIronRequiredConfig.Value)
                {
                    createArmoredIron = true;
                    player.GetInventory().RemoveItem("$item_iron", SkeletonWand.ArmorIronRequiredConfig.Value);
                }
            }

            bool createArmoredBlackIron = false;
            if (ExtraResourceConsumptionUnlocked
                && !createArmoredLeather
                && !createArmoredBronze
                && !createArmoredIron
                && SkeletonWand.ArmorBlackIronRequiredConfig.Value > 0)
            {
                int blackIronInInventory = player.GetInventory().CountItems("$item_blackmetal");
                if (blackIronInInventory >= SkeletonWand.ArmorBlackIronRequiredConfig.Value)
                {
                    createArmoredBlackIron = true;
                    player.GetInventory().RemoveItem("$item_blackmetal", SkeletonWand.ArmorBlackIronRequiredConfig.Value);
                }
            }

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (MaxDraugr.Value > 0)
            {
                // re-count the current active draugr
                CountActiveDraugrMinions();
            }

            // scale according to skill
            float playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);

            int quality = DraugrTierOneQuality.Value;
            if (playerNecromancyLevel >= DraugrTierThreeLevelReq.Value) { quality = DraugrTierThreeQuality.Value; }
            else if (playerNecromancyLevel >= DraugrTierTwoLevelReq.Value) { quality = DraugrTierTwoQuality.Value; }

            InstantiateDraugr(player, quality, playerNecromancyLevel, archer, createArmoredLeather, createArmoredBronze, createArmoredIron, createArmoredBlackIron, boneFragmentsRequired, meatRequired);
        }

        protected void InstantiateDraugr(Player player, int quality, float playerNecromancyLevel, bool archer, bool leatherArmor, bool bronzeArmor, bool ironArmor, bool blackIronArmor, int boneFragmentsRequired, int meatRequired)
        {
            // go on to spawn draugr
            string prefabName = archer ? "ChebGonaz_DraugrArcher" : "ChebGonaz_DraugrWarrior";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Logger.LogError($"SpawnFriendlyDraugr: spawning {prefabName} failed");
            }

            GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            spawnedChar.AddComponent<FreshMinion>();
            DraugrMinion minion = spawnedChar.AddComponent<DraugrMinion>();
            minion.SetCreatedAtLevel(playerNecromancyLevel);
            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);
            minion.ScaleStats(playerNecromancyLevel);
            minion.ScaleEquipment(playerNecromancyLevel, leatherArmor, bronzeArmor, ironArmor, blackIronArmor);

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill, necromancyLevelIncrease.Value);

            if (FollowByDefault.Value)
            {
                minion.Follow(player.gameObject);
            }
            else
            {
                minion.Wait(player.transform.position);
            }

            minion.SetUndeadMinionMaster(player.GetPlayerName());

            if (DurabilityDamage.Value)
            {
                Player.m_localPlayer.GetRightItem().m_durability -= archer
                    ? DurabilityDamageArcher.Value : DurabilityDamageWarrior.Value;
            }

            // handle refunding of resources on death
            if (DraugrMinion.DropOnDeath.Value != UndeadMinion.DropType.Nothing)
            {
                CharacterDrop characterDrop = minion.gameObject.AddComponent<CharacterDrop>();

                if (DraugrMinion.DropOnDeath.Value == UndeadMinion.DropType.Everything)
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
                if (leatherArmor)
                {
                    // for now, assume deerhide
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("DeerHide"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value,
                        m_amountMax = SkeletonWand.ArmorLeatherScrapsRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (bronzeArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Bronze"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.ArmorBronzeRequiredConfig.Value,
                        m_amountMax = SkeletonWand.ArmorBronzeRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (ironArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("Iron"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.ArmorIronRequiredConfig.Value,
                        m_amountMax = SkeletonWand.ArmorIronRequiredConfig.Value,
                        m_chance = 1f
                    });
                }
                else if (blackIronArmor)
                {
                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab("BlackMetal"),
                        m_onePerPlayer = true,
                        m_amountMin = SkeletonWand.ArmorBlackIronRequiredConfig.Value,
                        m_amountMax = SkeletonWand.ArmorBlackIronRequiredConfig.Value,
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
            List<Tuple<int, Character>> minionsFound = new();

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
                if (result >= MaxDraugr.Value - 1)
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
