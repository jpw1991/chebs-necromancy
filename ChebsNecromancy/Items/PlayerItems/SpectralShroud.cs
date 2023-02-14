using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using ChebsNecromancy.Common;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Items.PlayerItems
{
    internal class SpectralShroud : Item
    {
        public static ConfigEntry<bool> SpawnWraith;
        public static ConfigEntry<int> NecromancySkillBonus;
        public static ConfigEntry<int> DelayBetweenWraithSpawns;

        public static ConfigEntry<int> GuardianWraithTierOneQuality;
        public static ConfigEntry<int> GuardianWraithTierTwoQuality;
        public static ConfigEntry<int> GuardianWraithTierTwoLevelReq;
        public static ConfigEntry<int> GuardianWraithTierThreeQuality;
        public static ConfigEntry<int> GuardianWraithTierThreeLevelReq;

        private float wraithLastSpawnedAt;

        public override void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.DefaultRecipe = "Chain:5,TrollHide:10";
            ChebsRecipeConfig.RecipeName = "$item_friendlyskeletonwand_spectralshroud";
            ChebsRecipeConfig.ItemName = "ChebGonaz_SpectralShroud";
            ChebsRecipeConfig.RecipeDescription = "$item_friendlyskeletonwand_spectralshroud_desc";
            ChebsRecipeConfig.PrefabName = "ChebGonaz_SpectralShroud.prefab";
            ChebsRecipeConfig.ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name;

            base.CreateConfigs(plugin);

            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "Allowed",
                true, "Whether crafting a Spectral Shroud is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingStationRequired = plugin.ModConfig(ChebsRecipeConfig.ObjectName, 
                ChebsRecipeConfig.ObjectName + "CraftingStation", ChebsRecipe.EcraftingTable.Workbench, 
                "Crafting station where Spectral Shroud is available", null, true);

            ChebsRecipeConfig.CraftingStationLevel = plugin.ModConfig(ChebsRecipeConfig.ObjectName, 
                ChebsRecipeConfig.ObjectName + "CraftingStationLevel", 1, "Crafting station level required to craft Spectral Shroud", 
                plugin.IntQuantityValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "CraftingCosts",
                ChebsRecipeConfig.DefaultRecipe, "Materials needed to craft Spectral Shroud. None or Blank will use Default settings.", null, true);

            SpawnWraith = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "SpawnWraith",
                true, "Whether wraiths spawn or not.", plugin.BoolValue, true);

            NecromancySkillBonus = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "SkillBonus",
                10, "How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", 
                plugin.IntQuantityValue, true);

            DelayBetweenWraithSpawns = plugin.ModConfig(ChebsRecipeConfig.ObjectName, ChebsRecipeConfig.ObjectName + "WraithDelay",
                30, "How much time must pass after a wraith spawns before a new one is able to spawn.", plugin.IntQuantityValue, true);

            GuardianWraithTierOneQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "GuardianWraithTierOneQuality",
               1, "Star Quality of tier 1 GuardianWraith minions", plugin.IntQuantityValue, true);

            GuardianWraithTierTwoQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "GuardianWraithTierTwoQuality",
                2, "Star Quality of tier 2 GuardianWraith minions", plugin.IntQuantityValue, true);

            GuardianWraithTierTwoLevelReq = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "GuardianWraithTierTwoLevelReq",
                35, "Necromancy skill level required to summon Tier 2 GuardianWraith", plugin.IntQuantityValue, true);

            GuardianWraithTierThreeQuality = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "GuardianWraithTierThreeQuality",
                3, "Star Quality of tier 3 GuardianWraith minions", plugin.IntQuantityValue, true);

            GuardianWraithTierThreeLevelReq = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "GuardianWraithTierThreeLevelReq",
                70, "Necromancy skill level required to summon Tier 3 GuardianWraith", plugin.IntQuantityValue, true);
        }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            CustomItem customItem = ChebsRecipeConfig.GetCustomItemFromPrefab<CustomItem>(prefab);

            // make sure the set effect is applied or removed according
            // to config values
            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect =
                NecromancySkillBonus.Value > 0 ?
                BasePlugin.SetEffectNecromancyArmor : null;
            customItem.ItemDrop.m_itemData.m_shared.m_equipStatusEffect =
                NecromancySkillBonus.Value > 0 ?
                BasePlugin.SetEffectNecromancyArmor : null;

            return customItem;
        }

        public override void DoOnUpdate()
        {
            if (SpawnWraith.Value
                && ZInput.instance != null
                && Player.m_localPlayer != null)
            {
                if (Time.time > DoOnUpdateDelay)
                {
                    GuardianWraithStuff();

                    DoOnUpdateDelay = Time.time + .5f;
                }
            }
        }

        protected bool EnemiesNearby(out Character enemy)
        {

            List<Character> charactersInRange = new List<Character>();
            Character.GetCharactersInRange(
                Player.m_localPlayer.transform.position,
                30f,
                charactersInRange
                );
            foreach (var character in charactersInRange.Where(character => character != null && character.m_faction != Character.Faction.Players))
            {
                enemy = character;
                return true;
            }
            enemy = null;
            return false;
        }

        private void GuardianWraithStuff()
        {
            Player player = Player.m_localPlayer;
            float playerNecromancyLevel = player.GetSkillLevel(
                SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill);

            if (Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                    equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud")
                ) == null) return;

            if (!(Time.time > wraithLastSpawnedAt + DelayBetweenWraithSpawns.Value)) return;
            if (playerNecromancyLevel >= GuardianWraithMinion.GuardianWraithLevelRequirement.Value)
            {
                if (!EnemiesNearby(out Character enemy)) return;
                
                GameObject prefab = ZNetScene.instance.GetPrefab("ChebGonaz_GuardianWraith");
                if (!prefab)
                {
                    Logger.LogError("GuardianWraithCoroutine: spawning Wraith failed");
                }
                else
                {
                    int quality = GuardianWraithTierOneQuality.Value;
                    if (playerNecromancyLevel >= GuardianWraithTierThreeLevelReq.Value) { quality = GuardianWraithTierThreeQuality.Value; }
                    else if (playerNecromancyLevel >= GuardianWraithTierTwoLevelReq.Value) { quality = GuardianWraithTierTwoQuality.Value; }

                    player.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_wraithmessage");
                    GameObject instance = GameObject.Instantiate(prefab,
                        player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                    GuardianWraithMinion guardianWraithMinion = instance.AddComponent<GuardianWraithMinion>();
                    guardianWraithMinion.canBeCommanded = false;
                    Character character = instance.GetComponent<Character>();
                    character.SetLevel(quality);
                    character.m_faction = Character.Faction.Players;
                    // set owner to player
                    character.GetComponent<ZNetView>().GetZDO().SetOwner(ZDOMan.instance.GetMyID());

                    MonsterAI monsterAI = instance.GetComponent<MonsterAI>();
                    monsterAI.SetFollowTarget(player.gameObject);
                    monsterAI.SetTarget(enemy);

                    wraithLastSpawnedAt = Time.time;
                }
            }
            else
            {
                // instantiate hostile wraith to punish player
                player.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_wraithangrymessage");
                GameObject prefab = ZNetScene.instance.GetPrefab("Wraith");
                if (!prefab)
                {
                    Logger.LogError("Wraith prefab null!");
                }
                else
                {
                    GameObject.Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                    wraithLastSpawnedAt = Time.time;
                }
            }
        }
    }
}
