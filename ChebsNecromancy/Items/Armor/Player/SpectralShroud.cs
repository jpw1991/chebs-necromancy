using System.Collections.Generic;
using System.Linq;
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
    internal class SpectralShroud : Item
    {
        public override string ItemName => "ChebGonaz_SpectralShroud";
        public override string PrefabName => "ChebGonaz_SpectralShroud.prefab";
        protected override string DefaultRecipe => "Chain:5,TrollHide:10";

        public static ConfigEntry<bool> SpawnWraith;
        public static ConfigEntry<int> NecromancySkillBonus;
        public static ConfigEntry<int> DelayBetweenWraithSpawns;

        public static ConfigEntry<int> GuardianWraithTierOneQuality;
        public static ConfigEntry<int> GuardianWraithTierTwoQuality;
        public static ConfigEntry<int> GuardianWraithTierTwoLevelReq;
        public static ConfigEntry<int> GuardianWraithTierThreeQuality;
        public static ConfigEntry<int> GuardianWraithTierThreeLevelReq;

        public static ConfigEntry<CraftingTable> CraftingStationRequired;
        public static ConfigEntry<int> CraftingStationLevel;

        public static ConfigEntry<string> CraftingCost;

        private float wraithLastSpawnedAt;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            Allowed = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudAllowed",
                true, new ConfigDescription("Whether crafting a Spectral Shroud is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationRequired = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Spectral Shroud is available", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingStationLevel = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Spectral Shroud", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft Spectral Shroud. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SpawnWraith = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudSpawnWraith",
                true, new ConfigDescription("Whether wraiths spawn or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            NecromancySkillBonus = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudSkillBonus",
                10, new ConfigDescription("How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DelayBetweenWraithSpawns = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "SpectralShroudWraithDelay",
                30, new ConfigDescription("How much time must pass after a wraith spawns before a new one is able to spawn.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            GuardianWraithTierOneQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "GuardianWraithTierOneQuality",
               1, new ConfigDescription("Star Quality of tier 1 GuardianWraith minions", null,
               new ConfigurationManagerAttributes { IsAdminOnly = true }));

            GuardianWraithTierTwoQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "GuardianWraithTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 GuardianWraith minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            GuardianWraithTierTwoLevelReq = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "GuardianWraithTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 GuardianWraith", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            GuardianWraithTierThreeQuality = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "GuardianWraithTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 GuardianWraith minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            GuardianWraithTierThreeLevelReq = plugin.Config.Bind($"{GetType().Name} (Server Synced)", "GuardianWraithTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 GuardianWraith", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }
        
        public override void UpdateRecipe()
        {
            UpdateRecipe(CraftingStationRequired, CraftingCost, CraftingStationLevel);
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand_spectralshroud";
            config.Description = "$item_friendlyskeletonwand_spectralshroud_desc";

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

            CustomItem customItem = new CustomItem(prefab, false, config);
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
            foreach (var character in charactersInRange.Where(
                         character => 
                             character != null
                             && (character.m_faction != Character.Faction.Players && !character.m_tamed)))
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

            bool shroudEquipped = false, backpackEquipped = false;
            foreach (var equippedItem in player.GetInventory().GetEquipedtems())
            {
                if (!shroudEquipped
                    && equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud"))
                    shroudEquipped = true;
                if (!backpackEquipped
                    && equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud_backpack"))
                    backpackEquipped = true;
            }

            if (!shroudEquipped && !backpackEquipped) return;

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
