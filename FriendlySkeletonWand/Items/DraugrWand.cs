using BepInEx.Configuration;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FriendlySkeletonWand.Minions;

namespace FriendlySkeletonWand
{
    internal class DraugrWand : Wand
    {
        public static ConfigEntry<int> maxDraugr;

        public static ConfigEntry<bool> draugrAllowed;

        public static ConfigEntry<float> draugrBaseHealth;
        public static ConfigEntry<float> draugrHealthMultiplier;
        public static ConfigEntry<float> draugrSetFollowRange;

        private ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> draugrBoneFragmentsRequiredConfig;
        public static ConfigEntry<int> draugrMeatRequiredConfig;

        public override string ItemName { get { return "ChebGonaz_DraugrWand"; } }
        public override string PrefabName { get { return "ChebGonaz_DraugrWand.prefab"; } }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            allowed = plugin.Config.Bind("Client config", "DraugrWandAllowed",
                true, new ConfigDescription("Whether crafting a Draugr Wand is allowed or not."));

            draugrAllowed = plugin.Config.Bind("Client config", "DraugrAllowed",
                true, new ConfigDescription("If false, draugr aren't loaded at all and can't be summoned."));

            draugrBaseHealth = plugin.Config.Bind("Client config", "DraugrBaseHealth",
                100f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier"));

            draugrHealthMultiplier = plugin.Config.Bind("Client config", "DraugrHealthMultiplier",
                5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier"));

            draugrSetFollowRange = plugin.Config.Bind("Client config", "DraugrCommandRange",
                10f, new ConfigDescription("The range from which nearby Draugr will hear your command."));

            draugrMeatRequiredConfig = plugin.Config.Bind("Client config", "DraugrMeatRequired",
                1, new ConfigDescription("How many pieces of meat it costs to make a Draugr."));

            draugrBoneFragmentsRequiredConfig = plugin.Config.Bind("Client config", "DraugrBoneFragmentsRequired",
                3, new ConfigDescription("How many bone fragments it costs to make a Draugr."));

            necromancyLevelIncrease = plugin.Config.Bind("Client config", "DraugrNecromancyLevelIncrease",
                1.5f, new ConfigDescription("How much creating a Draugr contributes to your Necromancy level increasing."));

            maxDraugr = plugin.Config.Bind("Client config", "MaximumDraugr",
                0, new ConfigDescription("The maximum Draugr allowed to be created (0 = unlimited)."));
        }

        public override void CreateButtons()
        {
            // call the base to add the basic generic buttons -> create, attack, follow, wait, etc.
            base.CreateButtons();

            // add any extra buttons
            
        }

        public override CustomItem GetCustomItem(Sprite icon = null)
        {
            Jotunn.Logger.LogError("I shouldn't be called");
            return null;
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand_draugrwand";
            config.Description = "$item_friendlyskeletonwand_draugrwand_desc";
            if (allowed.Value)
            {
                config.CraftingStation = "piece_workbench";
                config.AddRequirement(new RequirementConfig("ElderBark", 5));
                config.AddRequirement(new RequirementConfig("FineWood", 5));
                config.AddRequirement(new RequirementConfig("Bronze", 5));
                config.AddRequirement(new RequirementConfig("TrophyDraugr", 1));
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

                Jotunn.Logger.LogInfo($"BoneFragments in inventory: {boneFragmentsInInventory}");
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

                Jotunn.Logger.LogInfo($"Meat in inventory: {meatInInventory}");
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

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (maxDraugr.Value > 0)
            {
                // re-count the current active draugr
                int activeDraugr = CountActiveDraugrMinions();
                Jotunn.Logger.LogInfo($"Draugr count: {activeDraugr}; maxDraugr = {maxDraugr.Value}");
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
            Jotunn.Logger.LogInfo($"Player necromancy level: {playerNecromancyLevel}");

            int quality = 1;
            if (playerNecromancyLevel >= 70) { quality = 3; }
            else if (playerNecromancyLevel >= 35) { quality = 2; }

            // go on to spawn draugr
            string prefabName = archer ? "ChebGonaz_DraugrArcher" : "ChebGonaz_DraugrWarrior";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Jotunn.Logger.LogError($"SpawnFriendlyDraugr: spawning {prefabName} failed");
            }

            Jotunn.Logger.LogInfo($"Spawning {prefabName}");
            GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            DraugrMinion minion = spawnedChar.AddComponent<DraugrMinion>();
            Character character = spawnedChar.GetComponent<Character>();
            character.m_faction = Character.Faction.Players;
            character.SetLevel(quality);
            minion.ScaleStats(playerNecromancyLevel);

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
                spawnedChar.GetComponent<MonsterAI>().SetFollowTarget(player.gameObject);
            }

            try
            {
                spawnedChar.GetComponent<ZNetView>().GetZDO().SetOwner(
                    ZDOMan.instance.GetMyID()
                    );
            }
            catch (Exception e)
            {
                Jotunn.Logger.LogError($"Failed to set minion owner to player: {e}");
            }
        }

        public int CountActiveDraugrMinions()
        {
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
                if (minion != null)
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
