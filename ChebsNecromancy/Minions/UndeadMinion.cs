using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    public class UndeadMinion : ChebGonazMinion
    {
        public const string MinionCreatedAtLevelKey = "UndeadMinionCreatedAtLevel";
        public const string MinionEmblemZdoKey = "UndeadMinionEmblem";
        
        #region CleanupAfterLogout
        private const float NextPlayerOnlineCheckInterval = 15f;
        private float nextPlayerOnlineCheckAt;
        protected float CleanupAt;
        #endregion
        
        public static ConfigEntry<CleanupType> CleanupAfter;
        public static ConfigEntry<int> CleanupDelay;
        public static ConfigEntry<bool> Commandable;
        public static ConfigEntry<float> RoamRange;
        
        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            CleanupAfter = plugin.Config.Bind("UndeadMinion (Server Synced)", "CleanupAfter",
                CleanupType.None, new ConfigDescription("Whether a minion should be cleaned up or not.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            CleanupDelay = plugin.Config.Bind("UndeadMinion (Server Synced)", "CleanupDelay",
                300, new ConfigDescription("The delay, in seconds, after which a minion will be destroyed. It has no effect if CleanupAfter is set to None.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            Commandable = plugin.Config.Bind("UndeadMinion (Client)", "Commandable",
                true, new ConfigDescription("If true, minions can be commanded individually with E (or equivalent) keybind."));
            RoamRange = plugin.Config.Bind("UndeadMinion (Client)", "RoamRange",
                10f, new ConfigDescription("How far a unit is allowed to roam from its current position."));
        }

        public virtual void Awake()
        {
            var tameable = GetComponent<Tameable>();
            if (tameable != null)
            {
                // let the minions generate a little necromancy XP for their master
                tameable.m_levelUpOwnerSkill = SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill;
                tameable.m_commandable = Commandable.Value;
            }
            if (CleanupAfter.Value == CleanupType.Time)
            {
                CleanupAt = Time.time + CleanupDelay.Value;
            }
            else if (CleanupAfter.Value == CleanupType.Logout)
            {
                // check if player is still online every X seconds
                nextPlayerOnlineCheckAt = Time.time + NextPlayerOnlineCheckInterval;
            }
        }
        
        private void Update()
        {
            if (CleanupAt > 0
                && Time.time > CleanupAt 
                && CleanupAfter.Value != CleanupType.None)
            {
                //Jotunn.Logger.LogInfo($"Cleaning up {name} because current time {Time.time} > {cleanupAt}");
                Kill();
                // check again in 5 seconds rather than spamming every frame with Kill requests. In
                // 99.9% of cases the 2nd check will never occur because the character will be dead
                CleanupAt += 5;
            }
            if (nextPlayerOnlineCheckAt > 0
                && Time.time > nextPlayerOnlineCheckAt)
            {
                bool playerOnline = Player.GetAllPlayers().Find(player => BelongsToPlayer(player.GetPlayerName()));
                if (!playerOnline)
                {
                    CleanupAt = Time.time + CleanupDelay.Value;
                }
                else
                {
                    CleanupAt = 0;
                }
                nextPlayerOnlineCheckAt = Time.time + NextPlayerOnlineCheckInterval;
            }
        }
        
        #region CreatedAtLevelZDO
        public void SetCreatedAtLevel(float necromancyLevel)
        {
            // We store the level the minion was created at so it can be scaled
            // correctly in the Awake function
            if (!TryGetComponent(out ZNetView zNetView))
            {
                Jotunn.Logger.LogError($"Cannot SetCreatedAtLevel to {necromancyLevel} because it has no ZNetView component.");
                return;
            }
            zNetView.GetZDO().Set(MinionCreatedAtLevelKey, necromancyLevel);
        }
        protected float GetCreatedAtLevel()
        {
            if (!TryGetComponent(out ZNetView zNetView))
            {
                Jotunn.Logger.LogError($"Cannot read {MinionCreatedAtLevelKey} because it has no ZNetView component.");
                return 1f;
            }
            return zNetView.GetZDO().GetFloat(MinionCreatedAtLevelKey, 1f);
        }
        #endregion
        
        public static void CountActive<T>(int minionLimitIncrementsEveryXLevels, int maxMinions) where T : UndeadMinion
        {
            Jotunn.Logger.LogInfo($"increments: {minionLimitIncrementsEveryXLevels}, max: {maxMinions}");
            // Get all active skeleton minions that belong to the local player
            var minions = Character.GetAllCharacters()
                .Where(c => !c.IsDead())
                .Select(c => (c.GetComponent<T>(), c))
                .Where(t => t.Item1 != null && t.Item1.BelongsToPlayer(Player.m_localPlayer.GetPlayerName()))
                .OrderByDescending(t => t.Item1.createdOrder)
                .ToList();
            // Determine the maximum number of minions the player can have
            var necromancySkill = SkillManager.Instance.GetSkill(BasePlugin.NecromancySkillIdentifier).m_skill;
            var playerNecromancyLevel = Player.m_localPlayer.GetSkillLevel(necromancySkill);
            var bonusMinions = minionLimitIncrementsEveryXLevels > 0
                ? Mathf.FloorToInt(playerNecromancyLevel / minionLimitIncrementsEveryXLevels)
                : 0;
            var maxMinionsPlusBonus = maxMinions + bonusMinions;
            maxMinionsPlusBonus -= 1;
            Jotunn.Logger.LogInfo($"maxMinionsPlusBonus: {maxMinionsPlusBonus}");
            
            // Kill off surplus minions
            for (var i = maxMinionsPlusBonus; i < minions.Count; i++)
            {
                minions[i].Item2.SetHealth(0);
            }
        }
        #region EmblemZDO
        public string Emblem
        {
            // todo: refactor to store (int)Emblem value instead
            get => TryGetComponent(out ZNetView zNetView) ? zNetView.GetZDO().GetString(MinionEmblemZdoKey) : InternalName.GetName(NecromancerCape.Emblem.Blank);
            set
            {
                if (TryGetComponent(out ZNetView zNetView))
                {
                    zNetView.GetZDO().Set(MinionEmblemZdoKey, value);
                }
                else
                {
                    Jotunn.Logger.LogError($"Cannot set emblem to {value} because it has no ZNetView component.");
                }
            }
        }
        #endregion
    }
}