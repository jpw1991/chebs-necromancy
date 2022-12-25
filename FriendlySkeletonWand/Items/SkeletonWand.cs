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
        public static List<GameObject> skeletons = new List<GameObject>();

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
                2, new ConfigDescription("The amount of LeatherScraps required to craft an armored skeleton."));
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
            return new KeyHintConfig
            {
                Item = ItemName,
                ButtonConfigs = new[]
                {
                    CreateMinionButton,
                    CreateArcherMinionButton,
                    FollowButton,
                    WaitButton,
                    TeleportButton,
                    AttackTargetButton,
                }
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

            bool createArmoredLeather = false;
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
                for (int i = skeletons.Count - 1; i >= 0; i--) { if (skeletons[i] == null) { skeletons.RemoveAt(i); } }
                if (skeletons.Count >= maxSkeletons.Value)
                {
                    // destroy one of the existing skeletons to make room
                    // for the new one
                    skeletons[0].GetComponent<Humanoid>().SetHealth(0);
                    skeletons.RemoveAt(0);
                }
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

            // go on to spawn skeleton
            string prefabName = archer ? "ChebGonaz_SkeletonArcher" : "ChebGonaz_SkeletonWarrior";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"{prefabName} does not exist");
                Jotunn.Logger.LogError($"SpawnFriendlySkeleton: spawning {prefabName} failed");
            }

            GameObject spawnedChar = GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
            SkeletonMinion minion = spawnedChar.AddComponent<SkeletonMinion>();
            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);
            minion.ScaleEquipment(playerNecromancyLevel, archer, createArmoredLeather);
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
    }
}
