
using System.Collections;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class GuardianWraithMinion : FriendlySkeletonWandMinion
    {
        public static float tetherDistance;

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
        }

        IEnumerator TetherToPlayerCoroutine()
        {
            while (true)
            {
                if (ZInput.instance != null)
                {
                    
                    Player player = Player.m_localPlayer;
                    GetComponent<MonsterAI>().SetFollowTarget(player.gameObject);
                    if (Vector3.Distance(player.transform.position, transform.position) > tetherDistance)
                    {
                        transform.position = player.transform.position;
                    }
                }
                yield return new WaitForSeconds(5);
            }
        }
    }
}
