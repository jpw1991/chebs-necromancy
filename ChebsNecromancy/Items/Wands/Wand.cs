using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.CustomPrefabs;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Items;
using ChebsValheimLibrary.Minions;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsNecromancy.Items
{
    internal class Wand : Item
    {
        public static ConfigEntry<bool> FollowByDefault;
        public static ConfigEntry<float> FollowDistance;
        public static ConfigEntry<float> RunDistance;

        public ConfigEntry<KeyCode> CreateMinionConfig;
        public ConfigEntry<InputManager.GamepadButton> CreateMinionGamepadConfig;
        public ButtonConfig CreateMinionButton;

        public ConfigEntry<KeyCode> FollowConfig;
        public ConfigEntry<InputManager.GamepadButton> FollowGamepadConfig;
        public ButtonConfig FollowButton;

        public ConfigEntry<KeyCode> WaitConfig;
        public ConfigEntry<InputManager.GamepadButton> WaitGamepadConfig;
        public ButtonConfig WaitButton;

        public ConfigEntry<KeyCode> TeleportConfig;
        public ConfigEntry<InputManager.GamepadButton> TeleportGamepadConfig;
        public ButtonConfig TeleportButton;

        public ConfigEntry<float> TeleportDurabilityCost;
        public ConfigEntry<float> TeleportCooldown;
        protected float lastTeleport;

        public bool CanTeleport =>
            TeleportCooldown.Value == 0f || Time.time - lastTeleport > TeleportCooldown.Value;

        public ConfigEntry<KeyCode> NextMinionConfig;
        public ConfigEntry<InputManager.GamepadButton> NextMinionGamepadConfig;
        public ButtonConfig NextMinionButton;

        public ConfigEntry<KeyCode> UnlockExtraResourceConsumptionConfig;
        public ButtonConfig UnlockExtraResourceConsumptionButton;

        public bool ExtraResourceConsumptionUnlocked = false;

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            FollowByDefault = plugin.Config.Bind("Wands (Client)", "FollowByDefault",
                false, new ConfigDescription("Whether minions will automatically be set to follow upon being created or not."));
            
            FollowDistance = plugin.Config.Bind("Wands (Client)", "FollowDistance",
                3f, new ConfigDescription("How closely a minion will follow you (0 = standing on top of you, 3 = default)."));
            
            RunDistance = plugin.Config.Bind("Wands (Client)", "RunDistance",
                3f, new ConfigDescription("How close a following minion needs to be to you before it stops running and starts walking (0 = always running, 10 = default)."));

            CreateMinionConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"CreateMinion",
                KeyCode.B, new ConfigDescription("The key to create a warrior minion with."));

            CreateMinionGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"CreateMinionGamepad",
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("The key to gamepad button to create a warrior minion with."));

            NextMinionConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"NextMinion",
                KeyCode.H, new ConfigDescription("The key to cycle minion types."));

            NextMinionGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"NextMinionGamepad",
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("The key to gamepad button to cycle minion types."));

            FollowConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"Follow",
                KeyCode.F, new ConfigDescription("The key to tell minions to follow."));

            FollowGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"FollowGamepad",
                InputManager.GamepadButton.ButtonWest,
                new ConfigDescription("The gamepad button to tell minions to follow."));

            WaitConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"Wait",
                KeyCode.T, new ConfigDescription("The key to tell minions to wait."));

            WaitGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"WaitGamepad",
                InputManager.GamepadButton.ButtonEast,
                new ConfigDescription("The gamepad button to tell minions to wait."));

            TeleportConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"Teleport",
                KeyCode.G, new ConfigDescription("The key to teleport following minions to you."));

            TeleportGamepadConfig = plugin.Config.Bind("Keybinds (Client)", ItemName+"TeleportGamepad",
                InputManager.GamepadButton.SelectButton,
                new ConfigDescription("The gamepad button to teleport following minions to you."));
            
            TeleportCooldown = plugin.Config.Bind("Wands (Server Synced)", 
                "TeleportCooldown",
                5f, new ConfigDescription("How long a player must wait before being able to teleport minions again (0 for no cooldown).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            
            TeleportDurabilityCost = plugin.Config.Bind("Wands (Server Synced)", 
                "TeleportDurabilityCost",
                0f, new ConfigDescription("How much damage a wand receives from being used to teleport minions with (0 for no damage).", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));

            UnlockExtraResourceConsumptionConfig = plugin.Config.Bind("Keybinds (Client)", ItemName + "UnlockExtraResourceConsumption",
                KeyCode.LeftShift, new ConfigDescription("The key to permit consumption of additional resources when creating the minion eg. iron to make an armored skeleton."));
        }

        public virtual void CreateButtons()
        {
            if (CreateMinionConfig.Value != KeyCode.None)
            {
                CreateMinionButton = new ButtonConfig
                {
                    Name = ItemName + "CreateMinion",
                    Config = CreateMinionConfig,
                    GamepadConfig = CreateMinionGamepadConfig,
                    HintToken = "$chebgonaz_wand_createminion",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, CreateMinionButton);
            }

            if (NextMinionConfig.Value != KeyCode.None)
            {
                NextMinionButton = new ButtonConfig
                {
                    Name = ItemName + "NextMinion",
                    Config = NextMinionConfig,
                    GamepadConfig = NextMinionGamepadConfig,
                    HintToken = "$chebgonaz_wand_nextminion",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, NextMinionButton);
            }

            if (FollowConfig.Value != KeyCode.None)
            {
                FollowButton = new ButtonConfig
                {
                    Name = ItemName + "Follow",
                    Config = FollowConfig,
                    GamepadConfig = FollowGamepadConfig,
                    HintToken = "$friendlyskeletonwand_follow",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, FollowButton);
            }

            if (WaitConfig.Value != KeyCode.None)
            {
                WaitButton = new ButtonConfig
                {
                    Name = ItemName + "Wait",
                    Config = WaitConfig,
                    GamepadConfig = WaitGamepadConfig,
                    HintToken = "$friendlyskeletonwand_wait",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, WaitButton);
            }

            if (TeleportConfig.Value != KeyCode.None)
            {
                TeleportButton = new ButtonConfig
                {
                    Name = ItemName + "Teleport",
                    Config = TeleportConfig,
                    GamepadConfig = TeleportGamepadConfig,
                    HintToken = "$friendlyskeletonwand_teleport",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, TeleportButton);
            }

            if (UnlockExtraResourceConsumptionConfig.Value != KeyCode.None)
            {
                UnlockExtraResourceConsumptionButton = new ButtonConfig
                {
                    Name = ItemName + "UnlockExtraResourceConsumption",
                    Config = UnlockExtraResourceConsumptionConfig,
                    //GamepadConfig = AttackTargetGamepadConfig,
                    HintToken = "$friendlyskeletonwand_unlockextraresourceconsumption",
                    BlockOtherInputs = false
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, UnlockExtraResourceConsumptionButton);
            }
        }

        public virtual KeyHintConfig GetKeyHint()
        {
            return null;
        }

        public virtual void AddInputs()
        {

        }

        public virtual bool HandleInputs() { return false; }

        public void MakeNearbyMinionsRoam(float radius)
        {
            var player = Player.m_localPlayer;
            var allCharacters = new List<Character>();
            Character.GetCharactersInRange(player.transform.position, radius, allCharacters);
            foreach (var character in allCharacters)
            {
                if (character.IsDead()) continue;
                
                var minion = character.GetComponent<ChebGonazMinion>();
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(player.GetPlayerName())) continue;
                
                if (character.GetComponent<MonsterAI>().GetFollowTarget() != player.gameObject) continue;

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$chebgonaz_roaming");
                minion.Roam();
            }
        }
        
        public void MakeNearbyMinionsFollow(float radius, bool follow)
        {
            var player = Player.m_localPlayer;
            // based off BaseAI.FindClosestCreature
            var allCharacters = Character.GetAllCharacters();
            foreach (var item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                var minion = item.GetComponent<ChebGonazMinion>();
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(player.GetPlayerName())) continue;
                
                var distance = Vector3.Distance(item.transform.position, player.transform.position);
                
                // if within radius OR it's set to the targetObject so you can recall those you've commanded
                // to be somewhere that's beyond the radius
                var minionFollowTarget = item.GetComponent<MonsterAI>().GetFollowTarget();
                var minionFollowingOrb = minionFollowTarget != null &&
                                          minionFollowTarget.TryGetComponent(out OrbOfBeckoningProjectile _);
                //var minionFollowingPlayer = !minionFollowingOrb && minionFollowTarget == player.gameObject;
                if (distance > radius && !minionFollowingOrb) continue;
                
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    follow ? "$friendlyskeletonwand_skeletonfollowing" : "$friendlyskeletonwand_skeletonwaiting");
                if (follow)
                {
                    minion.Follow(player.gameObject);
                }
                else if (minionFollowTarget == player.gameObject)
                {
                    minion.Wait(player.transform.position);
                }
            }
        }

        public void TeleportFollowingMinionsToPlayer()
        {
            if (!CanTeleport) return;
            var player = Player.m_localPlayer;
            var rightItem = player.GetRightItem();
            if (TeleportDurabilityCost.Value > 0 && rightItem != null)
            {
                rightItem.m_durability -= TeleportDurabilityCost.Value;
            }

            lastTeleport = Time.time;
            
            // based off BaseAI.FindClosestCreature
            var allCharacters = Character.GetAllCharacters();
            foreach (var item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                if (item.GetComponent<ChebGonazMinion>() != null
                    && item.TryGetComponent(out MonsterAI monsterAI)
                    && monsterAI.GetFollowTarget() == player.gameObject)
                {
                    item.transform.position = player.transform.position;
                    // forget position of current enemy so they don't start chasing after it. Cannot set it to null
                    // via monsterAI.SetTarget(null) because this has no effect. Code below inspired by reading
                    // MonsterAI.UpdateTarget
                    monsterAI.SetAlerted(false);
                    monsterAI.m_targetCreature = null;
                    monsterAI.m_targetStatic = null;
                    monsterAI.m_timeSinceAttacking = 0.0f;
                }
            }
        }
    }
}
