using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class Wand
    {
        public string ItemName;

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

        public ConfigEntry<KeyCode> AttackTargetConfig;
        public ConfigEntry<InputManager.GamepadButton> AttackTargetGamepadConfig;
        public ButtonConfig AttackTargetButton;

        public ConfigEntry<KeyCode> CreateArcherMinionConfig;
        public ConfigEntry<InputManager.GamepadButton> CreateArcherMinionGamepadConfig;
        public ButtonConfig CreateArcherMinionButton;

        private GameObject targetObject;

        public virtual void CreateConfigs(BaseUnityPlugin plugin)
        {
            CreateMinionConfig = plugin.Config.Bind("Client config", ItemName+"CreateMinion",
                KeyCode.B, new ConfigDescription("$friendlyskeletonwand_config_create_minion_desc"));
            CreateMinionGamepadConfig = plugin.Config.Bind("Client config", ItemName + "CreateMinionGamepad",
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("$friendlyskeletonwand_config_create_minion_gamepad_desc"));

            CreateArcherMinionConfig = plugin.Config.Bind("Client config", ItemName + "CreateArcher",
                KeyCode.E, new ConfigDescription("$friendlyskeletonwand_config_create_minion_desc"));
            CreateArcherMinionGamepadConfig = plugin.Config.Bind("Client config", ItemName + "CreateArcherGamepad",
                InputManager.GamepadButton.ButtonSouth,
                new ConfigDescription("$friendlyskeletonwand_config_create_archer_minion_gamepad_desc"));

            FollowConfig = plugin.Config.Bind("Client config", ItemName + "Follow",
                KeyCode.F, new ConfigDescription("$friendlyskeletonwand_config_follow_desc"));
            FollowGamepadConfig = plugin.Config.Bind("Client config", ItemName + "FollowGamepad",
                InputManager.GamepadButton.ButtonWest,
                new ConfigDescription("$friendlyskeletonwand_config_follow_gamepad_desc"));

            WaitConfig = plugin.Config.Bind("Client config", ItemName + "Wait",
                KeyCode.T, new ConfigDescription("$friendlyskeletonwand_config_wait_desc"));
            WaitGamepadConfig = plugin.Config.Bind("Client config", ItemName + "WaitGamepad",
                InputManager.GamepadButton.ButtonEast,
                new ConfigDescription("$friendlyskeletonwand_config_wait_gamepad_desc"));

            TeleportConfig = plugin.Config.Bind("Client config", ItemName + "Teleport",
                KeyCode.G, new ConfigDescription("$friendlyskeletonwand_config_teleport_desc"));
            TeleportGamepadConfig = plugin.Config.Bind("Client config", ItemName + "TeleportGamepad",
                InputManager.GamepadButton.SelectButton,
                new ConfigDescription("$friendlyskeletonwand_config_teleport_gamepad_desc"));

            AttackTargetConfig = plugin.Config.Bind("Client config", ItemName + "Target",
                KeyCode.R, new ConfigDescription("$friendlyskeletonwand_config_attacktarget_desc"));
            AttackTargetGamepadConfig = plugin.Config.Bind("Client config", ItemName + "TargetGamepad",
                InputManager.GamepadButton.StartButton,
                new ConfigDescription("$friendlyskeletonwand_config_attacktarget_gamepad_desc"));
        }

        public virtual void CreateButtons()
        {
            CreateMinionButton = new ButtonConfig
            {
                Name = ItemName+"CreateMinion",
                Config = CreateMinionConfig,
                GamepadConfig = CreateMinionGamepadConfig,
                HintToken = "$friendlyskeletonwand_create",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(BasePlugin.PluginGUID, CreateMinionButton);

            CreateArcherMinionButton = new ButtonConfig
            {
                Name = ItemName+"CreateArcherMinion",
                Config = CreateArcherMinionConfig,
                GamepadConfig = CreateArcherMinionGamepadConfig,
                HintToken = "$friendlyskeletonwand_create_archer",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(BasePlugin.PluginGUID, CreateArcherMinionButton);

            FollowButton = new ButtonConfig
            {
                Name = ItemName + "Follow",
                Config = FollowConfig,
                GamepadConfig = FollowGamepadConfig,
                HintToken = "$friendlyskeletonwand_follow",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(BasePlugin.PluginGUID, FollowButton);

            WaitButton = new ButtonConfig
            {
                Name = ItemName + "Wait",
                Config = WaitConfig,
                GamepadConfig = WaitGamepadConfig,
                HintToken = "$friendlyskeletonwand_wait",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(BasePlugin.PluginGUID, WaitButton);

            TeleportButton = new ButtonConfig
            {
                Name = ItemName + "Teleport",
                Config = TeleportConfig,
                GamepadConfig = TeleportGamepadConfig,
                HintToken = "$friendlyskeletonwand_teleport",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(BasePlugin.PluginGUID, TeleportButton);

            AttackTargetButton = new ButtonConfig
            {
                Name = ItemName + "AttackTarget",
                Config = AttackTargetConfig,
                GamepadConfig = AttackTargetGamepadConfig,
                HintToken = "$friendlyskeletonwand_attacktarget",
                BlockOtherInputs = true
            };
            InputManager.Instance.AddButton(BasePlugin.PluginGUID, AttackTargetButton);
        }

        public virtual CustomItem GetCustomItem()
        {
            return null;
        }

        public virtual KeyHintConfig GetKeyHint()
        {
            return null;
        }

        public virtual void AddInputs()
        {

        }

        public virtual bool HandleInputs() { return false; }

        public void MakeNearbyMinionsFollow(Player player, float radius, bool follow)
        {
            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                UndeadMinion minion = item.GetComponent<UndeadMinion>();
                if (minion != null && minion.canBeCommanded)
                {
                    float distance = Vector3.Distance(item.transform.position, player.transform.position);
                    if (distance < radius || item.GetComponent<MonsterAI>().GetFollowTarget() == targetObject)
                    {
                        MonsterAI monsterAI = item.GetComponent<MonsterAI>();
                        if (monsterAI == null)
                        {
                            Jotunn.Logger.LogError("MakeNearbyMinionsFollow: failed to make skeleton follow -> MonsterAI is null!");
                        }
                        else
                        {
                            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                                follow ? "$friendlyskeletonwand_skeletonfollowing" : "$friendlyskeletonwand_skeletonwaiting");
                            monsterAI.SetFollowTarget(follow ? player.gameObject : null);
                        }
                    }
                }
            }
        }

        public void TeleportFollowingMinionsToPlayer(Player player)
        {
            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                if (item.GetComponent<UndeadMinion>() != null
                    && item.GetComponent<MonsterAI>().GetFollowTarget() == player.gameObject)
                {
                    item.transform.position = player.transform.position;
                }
            }
        }

        public void MakeFollowingMinionsAttackTarget(Player player)
        {
            // make raycast between player and whatever is being looked at
            // we also need to highlight the target somehow (dont know how yet)
            // eg. change its shader
            Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                                "$friendlyskeletonwand_targetfound");
                targetObject = GameObject.Instantiate(ZNetScene.instance.GetPrefab("Stone"));
                targetObject.transform.position = hit.transform.position;
                targetObject.transform.position += new Vector3(0, 10, 0);
                targetObject.transform.localScale = Vector3.one * 5;
                targetObject.name = "FriendlySkeletonWandTarget";
            }
            else
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                                "$friendlyskeletonwand_notargetfound");
                return;
            }

            // based off BaseAI.FindClosestCreature
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (item.IsDead())
                {
                    continue;
                }

                if (item.GetComponent<UndeadMinion>() != null
                    && item.GetComponent<MonsterAI>().GetFollowTarget() == player.gameObject)
                {
                    item.GetComponent<MonsterAI>().SetFollowTarget(targetObject);
                }
            }
        }
    }
}
