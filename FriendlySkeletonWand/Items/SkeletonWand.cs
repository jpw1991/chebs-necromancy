using BepInEx;
using BepInEx.Configuration;
using FriendlySkeletonWand.Minions;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class SkeletonWand : Wand
    {
        public const string SkeletonWarriorPrefabName = "ChebGonaz_SkeletonWarrior";
        public const string SkeletonArcherPrefabName = "ChebGonaz_SkeletonArcher";
        public const string PoisonSkeletonPrefabName = "ChebGonaz_PoisonSkeleton";

        public static ConfigEntry<bool> skeletonsAllowed;

        public static ConfigEntry<int> maxSkeletons;

        public static ConfigEntry<float> skeletonBaseHealth;
        public static ConfigEntry<float> skeletonHealthMultiplier;
        public static ConfigEntry<float> skeletonSetFollowRange;

        private static ConfigEntry<float> necromancyLevelIncrease;

        public static ConfigEntry<int> boneFragmentsRequiredConfig;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMin;
        public static ConfigEntry<int> boneFragmentsDroppedAmountMax;

        public static ConfigEntry<int> armorLeatherScrapsRequiredConfig;

        public static ConfigEntry<int> poisonSkeletonLevelRequirementConfig;
        public static ConfigEntry<float> poisonSkeletonBaseHealth;
        public static ConfigEntry<int> poisonSkeletonGuckRequiredConfig;
        public static ConfigEntry<float> poisonSkeletonNecromancyLevelIncrease;

        public override string ItemName { get { return "ChebGonaz_SkeletonWand"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonWand.prefab"; } }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            allowed = plugin.Config.Bind("Client config", "SkeletonWandAllowed",
                true, new ConfigDescription("Whether crafting a Skeleton Wand is allowed or not."));

            skeletonsAllowed = plugin.Config.Bind("Client config", "SkeletonsAllowed",
                true, new ConfigDescription("If false, skeletons aren't loaded at all and can't be summoned."));

            skeletonBaseHealth = plugin.Config.Bind("Client config", "SkeletonBaseHealth",
                20f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier"));

            skeletonHealthMultiplier = plugin.Config.Bind("Client config", "SkeletonHealthMultiplier",
                2.5f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier"));

            skeletonSetFollowRange = plugin.Config.Bind("Client config", "SkeletonCommandRange",
                10f, new ConfigDescription("The distance which nearby skeletons will hear your commands."));

            boneFragmentsRequiredConfig = plugin.Config.Bind("Client config", "BoneFragmentsRequired",
                3, new ConfigDescription("The amount of Bone Fragments required to craft a skeleton."));

            boneFragmentsDroppedAmountMin = plugin.Config.Bind("Client config", "BoneFragmentsDroppedAmountMin",
                1, new ConfigDescription("The minimum amount of bones dropped by creatures."));
            boneFragmentsDroppedAmountMax = plugin.Config.Bind("Client config", "BoneFragmentsDroppedAmountMax",
                3, new ConfigDescription("The maximum amount of bones dropped by creautres."));

            necromancyLevelIncrease = plugin.Config.Bind("Client config", "NecromancyLevelIncrease",
                1f, new ConfigDescription("How much crafting a skeleton contributes to your Necromancy level increasing."));

            maxSkeletons = plugin.Config.Bind("Client config", "MaximumSkeletons",
                0, new ConfigDescription("The maximum amount of skeletons that can be made (0 = unlimited)."));

            armorLeatherScrapsRequiredConfig = plugin.Config.Bind("Client config", "ArmoredSkeletonLeatherScrapsRequired",
                2, new ConfigDescription("The amount of LeatherScraps required to craft an armored skeleton (WIP, not ready yet!)."));

            poisonSkeletonBaseHealth = plugin.Config.Bind("Client config", "PoisonSkeletonBaseHealth",
                100f, new ConfigDescription("HP = BaseHealth + NecromancyLevel * HealthMultiplier"));

            poisonSkeletonLevelRequirementConfig = plugin.Config.Bind("Client config", "PoisonSkeletonLevelRequired",
                50, new ConfigDescription("The Necromancy level needed to summon a Poison Skeleton."));

            poisonSkeletonGuckRequiredConfig = plugin.Config.Bind("Client config", "PoisonSkeletonGuckRequired",
                1, new ConfigDescription("The amount of Guck required to craft a Poison Skeleton."));

            poisonSkeletonNecromancyLevelIncrease = plugin.Config.Bind("Client config", "PoisonSkeletonNecromancyLevelIncrease",
                3f, new ConfigDescription("How much crafting a Poison Skeleton contributes to your Necromancy level increasing."));
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
            config.Name = "$item_friendlyskeletonwand";
            config.Description = "$item_friendlyskeletonwand_desc";

            if (allowed == null)
            {
                Jotunn.Logger.LogError("allowed config entry is null!");
            }

            if (allowed.Value)
            {
                config.CraftingStation = "piece_workbench";
                config.AddRequirement(new RequirementConfig("Wood", 5));
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
                        equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand")
                        ) != null
                    )
            {
                if (CreateMinionButton != null && ZInput.GetButton(CreateMinionButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        false
                        );
                    return true;
                }
                else if (CreateArcherMinionButton != null && ZInput.GetButton(CreateArcherMinionButton.Name))
                {
                    SpawnFriendlySkeleton(Player.m_localPlayer,
                        boneFragmentsRequiredConfig.Value,
                        necromancyLevelIncrease.Value,
                        true
                        );
                    return true;
                }
                else if (FollowButton != null && ZInput.GetButton(FollowButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, true);
                    return true;
                }
                else if (WaitButton != null && ZInput.GetButton(WaitButton.Name))
                {
                    MakeNearbyMinionsFollow(Player.m_localPlayer, skeletonSetFollowRange.Value, false);
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

        public int CountActiveSkeletonMinions()
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

                SkeletonMinion minion = item.GetComponent<SkeletonMinion>();
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
                if (result >= maxSkeletons.Value - 1)
                {
                    Tuple<int, Character> tuple = minionsFound[i];
                    tuple.Item2.SetHealth(0);
                    continue;
                }

                result++;
            }

            return result;
        }

        private bool ConsumeGuckIfAvailable(Player player)
        {
            // return true if guck is available and got consumed
            int guckInInventory = player.GetInventory().CountItems("$item_guck");
            if (guckInInventory >= poisonSkeletonGuckRequiredConfig.Value)
            {
                player.GetInventory().RemoveItem("$item_guck", poisonSkeletonGuckRequiredConfig.Value);
                return true;
            }
            return false;
        }

        private void InstantiateSkeleton(Player player, int quality, float playerNecromancyLevel, string prefabName)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Jotunn.Logger.LogError($"InstantiateSkeleton: spawning {prefabName} failed");
            }

            GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            if (prefabName == SkeletonWarriorPrefabName)
            {
                SkeletonMinion minion = spawnedChar.AddComponent<SkeletonMinion>();
                minion.ScaleEquipment(playerNecromancyLevel, false, false);
                minion.ScaleStats(playerNecromancyLevel);
            }
            else if (prefabName == SkeletonArcherPrefabName)
            {
                SkeletonMinion minion = spawnedChar.AddComponent<SkeletonMinion>();
                minion.ScaleEquipment(playerNecromancyLevel, true, false);
                minion.ScaleStats(playerNecromancyLevel);
            }
            else if (prefabName == PoisonSkeletonPrefabName)
            {
                PoisonSkeletonMinion minion = spawnedChar.AddComponent<PoisonSkeletonMinion>();
                minion.ScaleEquipment(playerNecromancyLevel, false, false);
                minion.ScaleStats(playerNecromancyLevel);
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

            player.RaiseSkill(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill, necromancyLevelIncrease.Value);
        }

        public void SpawnFriendlySkeleton(Player player, int boneFragmentsRequired, float necromancyLevelIncrease, bool archer)
        {
            if (!skeletonsAllowed.Value) return;

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

                // consume the fragments
                player.GetInventory().RemoveItem("$item_bonefragments", boneFragmentsRequired);
            }

            //bool createArmoredLeather = false;
            //if (armorLeatherScrapsRequiredConfig.Value > 0)
            //{
            //    int leatherScrapsInInventory = player.GetInventory().CountItems("$item_leatherscraps");
            //    Jotunn.Logger.LogInfo($"LeatherScraps in inventory: {leatherScrapsInInventory}");
            //    if (leatherScrapsInInventory >= boneFragmentsRequired)
            //    {
            //        createArmoredLeather = true;
            //        player.GetInventory().RemoveItem("$item_leatherscraps", armorLeatherScrapsRequiredConfig.Value);
            //    }
            //}

            // if players have decided to foolishly restrict their power and
            // create a *cough* LIMIT *spits*... check that here
            if (maxSkeletons.Value > 0)
            {
                // re-count the current active skeletons
                int activeSkeletons = CountActiveSkeletonMinions();
                Jotunn.Logger.LogInfo($"Skeleton count: {activeSkeletons}; maxSkeletons = {maxSkeletons.Value}");
            }

            // scale according to skill
            float playerNecromancyLevel = player.GetSkillLevel(SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);
            Jotunn.Logger.LogInfo($"Player necromancy level: {playerNecromancyLevel}");

            int quality = 1;
            if (playerNecromancyLevel >= 70) { quality = 3; }
            else if (playerNecromancyLevel >= 35) { quality = 2; }

            // go on to spawn skeleton
            if (archer)
            {
                InstantiateSkeleton(player, quality, playerNecromancyLevel, SkeletonArcherPrefabName);
            }
            else if (playerNecromancyLevel >= poisonSkeletonLevelRequirementConfig.Value && ConsumeGuckIfAvailable(player))
            {
                InstantiateSkeleton(player, quality, playerNecromancyLevel, PoisonSkeletonPrefabName);
            }
            else
            {
                InstantiateSkeleton(player, quality, playerNecromancyLevel, SkeletonWarriorPrefabName);
            }
        }
    }
}
