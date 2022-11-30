
using System.Collections;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class GuardianWraithMinion : UndeadMinion
    {
        public static float tetherDistance = 15;

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
                        if (Vector3.Distance(player.transform.position, transform.position) > BasePlugin.guardianWraithTetherDistance.Value)
                        {
                            transform.position = player.transform.position;
                        }
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }
    }
}
