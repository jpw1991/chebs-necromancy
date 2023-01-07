using Jotunn.Configs;
using Jotunn.Entities;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Managers;
using Jotunn;
using System.Collections.Generic;

namespace FriendlySkeletonWand
{
    internal class SpectralShroud : Item
    {
        public override string ItemName { get { return "ChebGonaz_SpectralShroud"; } }
        public override string PrefabName { get { return "ChebGonaz_SpectralShroud.prefab"; } }

        public static ConfigEntry<bool> spawnWraith;
        public static ConfigEntry<int> necromancySkillBonus;
        public static ConfigEntry<int> delayBetweenWraithSpawns;

        private float wraithLastSpawnedAt;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            base.CreateConfigs(plugin);

            allowed = plugin.Config.Bind("Server config", "SpectralShroudAllowed",
                true, new ConfigDescription("Whether crafting a Spectral Shroud is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            spawnWraith = plugin.Config.Bind("Server config", "SpectralShroudSpawnWraith",
                true, new ConfigDescription("Whether wraiths spawn or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            necromancySkillBonus = plugin.Config.Bind("Server config", "SpectralShroudSkillBonus",
                10, new ConfigDescription("How much wearing the item should raise the Necromancy level (set to 0 to have no set effect at all).", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            delayBetweenWraithSpawns = plugin.Config.Bind("Server config", "SpectralShroudWraithDelay",
                30, new ConfigDescription("How much time must pass after a wraith spawns before a new one is able to spawn.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public override CustomItem GetCustomItem(Sprite icon=null)
        {
            Jotunn.Logger.LogError("I shouldn't be called");
            return null;
        }

        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            ItemConfig config = new ItemConfig();
            config.Name = "$item_friendlyskeletonwand_spectralshroud";
            config.Description = "$item_friendlyskeletonwand_spectralshroud_desc";
            if (allowed.Value)
            {
                config.CraftingStation = "piece_workbench";
                config.AddRequirement(new RequirementConfig("Chain", 5));
                config.AddRequirement(new RequirementConfig("TrollHide", 10));
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
            float necromancyLevel = player.GetSkillLevel(
                SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);

            if (Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                    equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud")
                    ) != null)
            {
                if (Time.time > wraithLastSpawnedAt + delayBetweenWraithSpawns.Value)
                {
                    if (necromancyLevel >= GuardianWraithMinion.guardianWraithLevelRequirement.Value)
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
                                int quality = 1;
                                if (necromancyLevel >= 70) { quality = 3; }
                                else if (necromancyLevel >= 35) { quality = 2; }

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
            //else
            //{
            //    if (GuardianWraithMinion.instance != null)
            //    {
            //        if (GuardianWraithMinion.instance.TryGetComponent(out Humanoid humanoid))
            //        {
            //            GuardianWraithMinion.instance.GetComponent<Humanoid>().SetHealth(0);
            //        }
            //    }
            //}
        }
    }
}
