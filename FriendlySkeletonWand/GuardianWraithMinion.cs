using BepInEx;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Managers;
using System.Collections;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class GuardianWraithMinion : UndeadMinion
    {
        public static ConfigEntry<int> guardianWraithLevelRequirement;
        public static ConfigEntry<float> guardianWraithTetherDistance;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            guardianWraithLevelRequirement = plugin.Config.Bind("Client config", "GuardianWraithLevelRequirement",
                25, new ConfigDescription("The Necromancy level required to control a Guardian Wraith."));
            guardianWraithTetherDistance = plugin.Config.Bind("Client config", "GuardianWraithTetherDistance",
                30f, new ConfigDescription("How far a Guardian Wraith can be from the player before it is teleported back to you."));
        }

        public static GameObject instance;

        private void Awake()
        {
            canBeCommanded = false;
            StartCoroutine(WaitForZInstance());
        }

        void DoWhenZInputAvailable()
        {
            GetComponent<Character>().m_faction = Character.Faction.Players;
            StartCoroutine(TetherToPlayerCoroutine());
        }

        IEnumerator WaitForZInstance()
        {
            bool zinstanceAvailable = false;
            while (!zinstanceAvailable)
            { 
                zinstanceAvailable = ZInput.instance != null;
                yield return new WaitForSeconds(1);
            }
            DoWhenZInputAvailable();
        }

        IEnumerator TetherToPlayerCoroutine()
        {
            while (true)
            {
                if (ZInput.instance != null)
                {
                    
                    Player player = Player.m_localPlayer;
                    if (player != null)
                    {
                        GetComponent<MonsterAI>().SetFollowTarget(player.gameObject);
                        if (Vector3.Distance(player.transform.position, transform.position) > guardianWraithTetherDistance.Value)
                        {
                            transform.position = player.transform.position;
                        }
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }

        public static IEnumerator GuardianWraithCoroutine()
        {
            while (true)
            {
                if (ZInput.instance != null && Player.m_localPlayer != null)
                {
                    Player player = Player.m_localPlayer;
                    float necromancyLevel = player.GetSkillLevel(
                        SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill);

                    if (Player.m_localPlayer.GetInventory().GetEquipedtems().Find(
                            equippedItem => equippedItem.TokenName().Equals("$item_friendlyskeletonwand_spectralshroud")
                            ) != null)
                    {
                        if (necromancyLevel >= guardianWraithLevelRequirement.Value)
                        {
                            if (instance == null || instance.GetComponent<Character>().IsDead())
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
                                    instance = GameObject.Instantiate(prefab,
                                        player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                                    GuardianWraithMinion guardianWraithMinion = instance.AddComponent<GuardianWraithMinion>();
                                    guardianWraithMinion.canBeCommanded = false;
                                    Character character = instance.GetComponent<Character>();
                                    character.SetLevel(quality);
                                    character.m_faction = Character.Faction.Players;
                                    instance.GetComponent<MonsterAI>().SetFollowTarget(player.gameObject);
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
                                Instantiate(prefab, player.transform.position + player.transform.forward * 2f + Vector3.up, Quaternion.identity);
                            }
                        }
                    }
                    else
                    {
                        if (instance != null)
                        {
                            if (instance.TryGetComponent(out Humanoid humanoid))
                            {
                                instance.GetComponent<Humanoid>().SetHealth(0);
                            }
                            else { Destroy(instance); }
                        }
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }
    }
}
