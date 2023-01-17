using Jotunn.Configs;
using Jotunn.Entities;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Managers;
using Jotunn;
using System.Collections.Generic;

namespace ChebsNecromancy
{
    internal class SpectralShroud : Item
    {
        public override string ItemName { get { return "ChebGonaz_SpectralShroud"; } }
        public override string PrefabName { get { return "ChebGonaz_SpectralShroud.prefab"; } }
        protected override string DefaultRecipe { get { return "Chain:5,TrollHide:10"; } }

        public static ConfigEntry<bool> spawnWraith;
        public static ConfigEntry<int> necromancySkillBonus;
        public static ConfigEntry<int> delayBetweenWraithSpawns;

        public static ConfigEntry<int> guardianWraithTierOneQuality;
        public static ConfigEntry<int> guardianWraithTierTwoQuality;
        public static ConfigEntry<int> guardianWraithTierTwoLevelReq;
        public static ConfigEntry<int> guardianWraithTierThreeQuality;
        public static ConfigEntry<int> guardianWraithTierThreeLevelReq;

        public static ConfigEntry<CraftingTable> craftingStationRequired;
        public static ConfigEntry<int> craftingStationLevel;

        public static ConfigEntry<string> craftingCost;

        private float wraithLastSpawnedAt;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            allowed = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudAllowed",
                true, new ConfigDescription("Whether crafting a Spectral Shroud is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingStationRequired = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudCraftingStation",
                CraftingTable.Workbench, new ConfigDescription("Crafting station where Spectral Shroud is available", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingStationLevel = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudCraftingStationLevel",
                1, new ConfigDescription("Crafting station level required to craft Spectral Shroud", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingCost = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudCraftingCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to craft Spectral Shroud. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            spawnWraith = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudSpawnWraith",
                true, new ConfigDescription("Whether wraiths spawn or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancySkillBonus = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudSkillBonus",
                10, new ConfigDescription("How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            delayBetweenWraithSpawns = plugin.Config.Bind("SpectralShroud (Server Synced)", "SpectralShroudWraithDelay",
                30, new ConfigDescription("How much time must pass after a wraith spawns before a new one is able to spawn.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            guardianWraithTierOneQuality = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithTierOneQuality",
               1, new ConfigDescription("Star Quality of tier 1 GuardianWraith minions", null,
               new ConfigurationManagerAttributes { IsAdminOnly = true }));

            guardianWraithTierTwoQuality = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithTierTwoQuality",
                2, new ConfigDescription("Star Quality of tier 2 GuardianWraith minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            guardianWraithTierTwoLevelReq = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithTierTwoLevelReq",
                35, new ConfigDescription("Necromancy skill level required to summon Tier 2 GuardianWraith", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            guardianWraithTierThreeQuality = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithTierThreeQuality",
                3, new ConfigDescription("Star Quality of tier 3 GuardianWraith minions", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            guardianWraithTierThreeLevelReq = plugin.Config.Bind("SpectralShroud (Server Synced)", "GuardianWraithTierThreeLevelReq",
                70, new ConfigDescription("Necromancy skill level required to summon Tier 3 GuardianWraith", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand_spectralshroud";
            config.Description = "$item_friendlyskeletonwand_spectralshroud_desc";

            if (allowed.Value)
            {
                if (string.IsNullOrEmpty(craftingCost.Value))
                {
                    craftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                this.SetRecipeReqs(
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
            // make sure the set effect is applied or removed according
            // to config values
            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect =
                necromancySkillBonus.Value > 0 ?
                BasePlugin.setEffectNecromancyArmor : null;
            customItem.ItemDrop.m_itemData.m_shared.m_equipStatusEffect =
                necromancySkillBonus.Value > 0 ?
                BasePlugin.setEffectNecromancyArmor : null;

            return customItem;
        }

        public override void DoOnUpdate()
        {
            if (spawnWraith.Value
                && ZInput.instance != null
                && Player.m_localPlayer != null)
            {
                if (Time.time > doOnUpdateDelay)
                {
                    GuardianWraithStuff();

                    doOnUpdateDelay = Time.time + .5f;
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
            foreach (Character character in charactersInRange)
            {
                if (character != null && character.m_faction != Character.Faction.Players)
                {
                    enemy = character;
                    return true;
                }
            }
            enemy = null;
            return false;
        }

        private void GuardianWraithStuff()
        {
            Player player = Player.m_localPlayer;
            float playerNecromancyLevel = player.GetSkillLevel(
                SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);

            if (Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                    equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud")
                    ) != null)
            {
                if (Time.time > wraithLastSpawnedAt + delayBetweenWraithSpawns.Value)
                {
                    if (playerNecromancyLevel >= GuardianWraithMinion.guardianWraithLevelRequirement.Value)
                    {
                        if (EnemiesNearby(out Character enemy))
                        {
                            GameObject prefab = ZNetScene.instance.GetPrefab("ChebGonaz_GuardianWraith");
                            if (!prefab)
                            {
                                Jotunn.Logger.LogError("GuardianWraithCoroutine: spawning Wraith failed");
                            }
                            else
                            {
                                int quality = guardianWraithTierOneQuality.Value;
                                if (playerNecromancyLevel >= guardianWraithTierThreeLevelReq.Value) { quality = guardianWraithTierThreeQuality.Value; }
                                else if (playerNecromancyLevel >= guardianWraithTierTwoLevelReq.Value) { quality = guardianWraithTierTwoQuality.Value; }

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
                    }
                    else
                    {
                        // instantiate hostile wraith to punish player
                        player.Message(MessageHud.MessageType.Center, "$friendlyskeletonwand_wraithangrymessage");
                        GameObject prefab = ZNetScene.instance.GetPrefab("Wraith");
                        if (!prefab)
                        {
                            Jotunn.Logger.LogError("Wraith prefab null!");
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
    }
}
