using System.Collections.Generic;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ChebsNecromancy.Items.PlayerItems
{
    internal class Wand : Item
    {
        #region ConfigEntries
        public static ConfigEntry<bool> FollowByDefault;

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

        public ConfigEntry<KeyCode> UnlockExtraResourceConsumptionConfig;
        public ButtonConfig UnlockExtraResourceConsumptionButton;
        #endregion ConfigEntries

        public bool ExtraResourceConsumptionUnlocked = false;
        private GameObject targetObject;

        public override void CreateConfigs(BasePlugin plugin)
        {
            FollowByDefault = plugin.ModConfig("Wands", "FollowByDefault",
                false,"Whether minions will automatically be set to follow upon being created or not.", plugin.BoolValue);

            CreateMinionConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName + "CreateMinion",
                KeyCode.B, "The key to create a warrior minion with.");

            CreateMinionGamepadConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName + "CreateMinionGamepad",
                InputManager.GamepadButton.ButtonSouth, "The key to gamepad button to create a warrior minion with.");

            CreateArcherMinionConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"CreateArcher",
                KeyCode.H, "The key to create an archer minion with.");

            CreateArcherMinionGamepadConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"CreateArcherGamepad",
                InputManager.GamepadButton.ButtonSouth, "The key to gamepad button to create an archer minion with.");

            FollowConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"Follow",
                KeyCode.F, "The key to tell minions to follow.");

            FollowGamepadConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"FollowGamepad",
                InputManager.GamepadButton.ButtonWest, "The gamepad button to tell minions to follow.");

            WaitConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"Wait",
                KeyCode.T, "The key to tell minions to wait.");

            WaitGamepadConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"WaitGamepad",
                InputManager.GamepadButton.ButtonEast, "The gamepad button to tell minions to wait.");

            TeleportConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"Teleport",
                KeyCode.G, "The key to teleport following minions to you.");

            TeleportGamepadConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"TeleportGamepad",
                InputManager.GamepadButton.SelectButton, "The gamepad button to teleport following minions to you.");

            AttackTargetConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"Target",
                KeyCode.R, "The key to tell minions to go to a specific target.");

            AttackTargetGamepadConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName+"TargetGamepad",
                InputManager.GamepadButton.StartButton, "The gamepad button to tell minions to go to a specific target.");

            UnlockExtraResourceConsumptionConfig = plugin.ModConfig("Keybinds", ChebsRecipeConfig.ObjectName + "UnlockExtraResourceConsumption",
                KeyCode.LeftShift, "The key to permit consumption of additional resources when creating the minion eg. iron to make an armored skeleton.");
        }
        public CustomItem GetCustomItemFromPrefab(GameObject prefab)
        {
            CustomItem customItem = ChebsRecipeConfig.GetCustomItemFromPrefab<CustomItem>(prefab);

            customItem.ItemDrop.m_itemData.m_shared.m_setStatusEffect = BasePlugin.SetEffectNecromancyArmor;

            return customItem;
        }

        public virtual void CreateButtons()
        {
            if (CreateMinionConfig.Value != KeyCode.None)
            {
                CreateMinionButton = new ButtonConfig
                {
                    Name = ChebsRecipeConfig.ObjectName + "CreateMinion",
                    Config = CreateMinionConfig,
                    GamepadConfig = CreateMinionGamepadConfig,
                    HintToken = "$friendlyskeletonwand_create",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, CreateMinionButton);
            }

            if (CreateArcherMinionConfig.Value != KeyCode.None)
            {
                CreateArcherMinionButton = new ButtonConfig
                {
                    Name = ChebsRecipeConfig.ObjectName + "CreateArcherMinion",
                    Config = CreateArcherMinionConfig,
                    GamepadConfig = CreateArcherMinionGamepadConfig,
                    HintToken = "$friendlyskeletonwand_create_archer",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, CreateArcherMinionButton);
            }

            if (FollowConfig.Value != KeyCode.None)
            {
                FollowButton = new ButtonConfig
                {
                    Name = ChebsRecipeConfig.ObjectName + "Follow",
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
                    Name = ChebsRecipeConfig.ObjectName + "Wait",
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
                    Name = ChebsRecipeConfig.ObjectName + "Teleport",
                    Config = TeleportConfig,
                    GamepadConfig = TeleportGamepadConfig,
                    HintToken = "$friendlyskeletonwand_teleport",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, TeleportButton);
            }

            if (AttackTargetConfig.Value != KeyCode.None)
            {
                AttackTargetButton = new ButtonConfig
                {
                    Name = ChebsRecipeConfig.ObjectName + "AttackTarget",
                    Config = AttackTargetConfig,
                    GamepadConfig = AttackTargetGamepadConfig,
                    HintToken = "$friendlyskeletonwand_attacktarget",
                    BlockOtherInputs = true
                };
                InputManager.Instance.AddButton(BasePlugin.PluginGuid, AttackTargetButton);
            }

            if (UnlockExtraResourceConsumptionConfig.Value != KeyCode.None)
            {
                UnlockExtraResourceConsumptionButton = new ButtonConfig
                {
                    Name = ChebsRecipeConfig.ObjectName + "UnlockExtraResourceConsumption",
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

        public void MakeNearbyMinionsRoam(Player player, float radius)
        {
            List<Character> allCharacters = new();
            Character.GetCharactersInRange(player.transform.position, radius, allCharacters);
            foreach (var character in allCharacters)
            {
                if (character.IsDead()) continue;
                
                UndeadMinion minion = character.GetComponent<UndeadMinion>();
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(player.GetPlayerName())) continue;
                
                if (character.GetComponent<MonsterAI>().GetFollowTarget() != player.gameObject) continue;

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$chebgonaz_roaming");
                minion.Roam();
            }
        }
        
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
                if (minion == null || !minion.canBeCommanded
                                   || !minion.BelongsToPlayer(Player.m_localPlayer.GetPlayerName())) continue;
                
                float distance = Vector3.Distance(item.transform.position, player.transform.position);
                // if within radius OR it's set to the targetObject so you can recall those you've commanded
                // to be somewhere that's beyond the radius
                if (!(distance < radius)
                    && (item.GetComponent<MonsterAI>().GetFollowTarget() != targetObject
                        || item.GetComponent<MonsterAI>().GetFollowTarget() == null)) continue;
                
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,
                    follow ? "$friendlyskeletonwand_skeletonfollowing" : "$friendlyskeletonwand_skeletonwaiting");
                if (follow)
                {
                    minion.Follow(player.gameObject);
                }
                else
                {
                    minion.Wait(player.transform.position);
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
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_targetfound");
                targetObject = GameObject.Instantiate(ZNetScene.instance.GetPrefab("Stone"));
                var position = targetObject.transform.position;
                position = hit.transform.position;
                position += new Vector3(0, 10, 0);
                targetObject.transform.position = position;
                targetObject.transform.localScale = Vector3.one * 5;
                targetObject.name = "ChebsNecromancyTarget";
            }
            else
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "$friendlyskeletonwand_notargetfound");
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
