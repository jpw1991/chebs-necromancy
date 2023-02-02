
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class UndeadMinion : MonoBehaviour
    {
        // we add this component to the creatures we create in the mod
        // so that we can use .GetComponent<UndeadMinion>()
        // to determine whether a creature was created by the mod, or
        // whether it was created by something else.
        //
        // This allows us to only call wait/follow/whatever on minions
        // that the mod has created. The component is lost between sessions
        // so it must be checked for in Awake and readded (see harmony patching).

        public enum CleanupType
        {
            None,
            Time,
            Logout,
        }

        public enum DropType
        {
            Nothing,
            JustResources,
            Everything,
        }

        public bool canBeCommanded = true;

        public static ConfigEntry<CleanupType> cleanupAfter;
        public static ConfigEntry<int> cleanupDelay;

        protected float cleanupAt;

        public const string minionOwnershipZDOKey = "UndeadMinionMaster";
        public const string minionDropsZDOKey = "UndeadMinionDrops";
        public const string minionWaitPosZDOKey = "UndeadMinionWaitPosition";
        public const string minionWaitObjectName = "UndeadMinionWaitPositionObject";

        #region CleanupAfterLogout
        private const float nextPlayerOnlineCheckInterval = 15f;
        private float nextPlayerOnlineCheckAt;
        #endregion

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            cleanupAfter = plugin.Config.Bind("UndeadMinion (Server Synced)", "CleanupAfter",
                CleanupType.None, new ConfigDescription("Whether a minion should be cleaned up or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            cleanupDelay = plugin.Config.Bind("UndeadMinion (Server Synced)", "CleanupDelay",
                300, new ConfigDescription("The delay, in seconds, after which a minion will be destroyed. It has no effect if CleanupAfter is set to None.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public virtual void Awake()
        {
            Tameable tameable = GetComponent<Tameable>();
            if (tameable != null)
            {
                // let the minions generate a little necromancy XP for their master
                tameable.m_levelUpOwnerSkill = SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill;
            }

            if (cleanupAfter.Value == CleanupType.Time)
            {
                cleanupAt = Time.time + cleanupDelay.Value;
            }
            else if (cleanupAfter.Value == CleanupType.Logout)
            {
                // check if player is still online every X seconds
                nextPlayerOnlineCheckAt = Time.time + nextPlayerOnlineCheckInterval;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // ignore collision with player

            Character character = collision.gameObject.GetComponent<Character>();
            if (character != null
                && character.m_faction == Character.Faction.Players
                && character.GetComponent<UndeadMinion>() == null) // allow collision between minions
            {
                Physics.IgnoreCollision(collision.gameObject.GetComponent<Collider>(), GetComponent<Collider>());
                return;
            }
        }

        private void Update()
        {
            if (cleanupAt > 0
                && Time.time > cleanupAt 
                && cleanupAfter.Value != CleanupType.None)
            {
                //Jotunn.Logger.LogInfo($"Cleaning up {name} because current time {Time.time} > {cleanupAt}");
                Kill();

                // check again in 5 seconds rather than spamming every frame with Kill requests. In
                // 99.9% of cases the 2nd check will never occur because the character will be dead
                cleanupAt += 5;
            }

            if (nextPlayerOnlineCheckAt > 0
                && Time.time > nextPlayerOnlineCheckAt)
            {
                bool playerOnline = Player.GetAllPlayers().Find(player => BelongsToPlayer(player.GetPlayerName()));
                if (!playerOnline)
                {
                    cleanupAt = Time.time + cleanupDelay.Value;
                }
                else
                {
                    cleanupAt = 0;
                }
                nextPlayerOnlineCheckAt = Time.time + nextPlayerOnlineCheckInterval;
            }
        }

        public void Kill()
        {
            if (TryGetComponent(out Character character))
            {
                if (!character.IsDead()) character.SetHealth(0);
            }
            else
            {
                Jotunn.Logger.LogError($"Cannot kill {name} because it has no Character component.");
            }
        }

        #region MinionMasterZDO
        public void SetUndeadMinionMaster(string playerName)
        {
            if (TryGetComponent(out ZNetView zNetView))
            {
                zNetView.GetZDO().Set(minionOwnershipZDOKey, playerName);
            }
            else
            {
                Jotunn.Logger.LogError($"Cannot SetUndeadMinionMaster to {playerName} because it has no ZNetView component.");
            }
        }
        #endregion
        public bool BelongsToPlayer(string playerName)
        {
            return TryGetComponent(out ZNetView zNetView) 
                && zNetView.GetZDO().GetString(minionOwnershipZDOKey, "")
                .Equals(playerName);
        }

        #region DropsZDO
        public void RecordDrops(CharacterDrop characterDrop)
        {
            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            if (TryGetComponent(out ZNetView zNetView))
            {
                string dropsList = "";
                List<string> drops = new List<string>();
                characterDrop.m_drops.ForEach(drop => drops.Add($"{drop.m_prefab.name}:{drop.m_amountMax}"));
                dropsList = string.Join(",", drops);
                //Jotunn.Logger.LogInfo($"Drops list: {dropsList}");
                zNetView.GetZDO().Set(minionDropsZDOKey, string.Join(",", dropsList));
            }
            else
            {
                Jotunn.Logger.LogError($"Cannot record drops because {name} has no ZNetView component.");
            }
        }

        public void RestoreDrops()
        {
            // the component won't be remembered by the game on logout because
            // only what is on the prefab is remembered. Even changes to the prefab
            // aren't remembered. So we must write what we're dropping into
            // the ZDO as well and then read & restore this on Awake
            if (TryGetComponent(out ZNetView zNetView))
            {
                if (gameObject.GetComponent<CharacterDrop>() != null)
                {
                    // abort - if it's already there, don't add it twice
                    return;
                }

                string minionDropsZDOValue = zNetView.GetZDO().GetString(minionDropsZDOKey, "");
                if (minionDropsZDOValue == "")
                {
                    // abort - there's no drops record -> naked minion
                    return;
                }

                CharacterDrop characterDrop = gameObject.AddComponent<CharacterDrop>();
                List<string> dropsList = new List<string>(minionDropsZDOValue.Split(','));
                dropsList.ForEach(dropString =>
                {
                    string[] splut = dropString.Split(':');

                    string prefabName = splut[0];
                    int amount = int.Parse(splut[1]);

                    characterDrop.m_drops.Add(new CharacterDrop.Drop
                    {
                        m_prefab = ZNetScene.instance.GetPrefab(prefabName),
                        m_onePerPlayer = true,
                        m_amountMin = amount,
                        m_amountMax = amount,
                        m_chance = 1f
                    });
                });
            }
            else
            {
                Jotunn.Logger.LogError($"Cannot record drops because {name} has no ZNetView component.");
            }
        }
        #endregion
        #region WaitPositionZDO
        protected void RecordWaitPosition(Vector3 waitPos)
        {
            if (TryGetComponent(out ZNetView zNetView))
            {
                zNetView.GetZDO().Set(minionWaitPosZDOKey, waitPos);
            }
            else
            {
                Jotunn.Logger.LogError($"Cannot RecordWaitPosition {waitPos} because it has no ZNetView component.");
            }
        }

        protected Vector3 GetWaitPosition()
        {
            if (TryGetComponent(out ZNetView zNetView))
            {
                return zNetView.GetZDO().GetVec3(minionWaitPosZDOKey, Vector3.negativeInfinity);
            }

            Jotunn.Logger.LogError($"Cannot GetWaitPosition because it has no ZNetView component.");
            return Vector3.negativeInfinity;
        }

        protected void WaitAtRecordedPosition()
        {
            Vector3 waitPos = GetWaitPosition();
            if (waitPos == Vector3.negativeInfinity)
            {
                // either error, or more likely, simply unset
                return;
            }
            if (TryGetComponent(out MonsterAI monsterAI))
            {
                // create a temporary object. This has no ZDO so will be cleaned up
                // after the session ends
                GameObject waitObject = new GameObject(minionWaitObjectName);
                waitObject.transform.position = waitPos;
                monsterAI.SetFollowTarget(waitObject);
            }
        }
        #endregion

        public void Follow(GameObject followObject)
        {
            if (!TryGetComponent(out MonsterAI monsterAI))
            {
                Jotunn.Logger.LogError($"Cannot Follow because it has no MonsterAI component.");
                return;
            }
            // clear out current wait object if it exists
            GameObject currentFollowTarget = monsterAI.GetFollowTarget();
            if (currentFollowTarget != null && currentFollowTarget.name == minionWaitObjectName)
            {
                GameObject.Destroy(currentFollowTarget);
            }
            // follow
            monsterAI.SetFollowTarget(followObject);
        }

        public void Wait(Vector3 waitPosition)
        {
            RecordWaitPosition(waitPosition);
            WaitAtRecordedPosition();
        }
    }
}
