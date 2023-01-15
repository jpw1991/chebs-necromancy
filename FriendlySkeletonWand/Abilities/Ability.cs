using BepInEx.Configuration;
using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand.Abilities
{
    public class Ability
    {
        public virtual string CooldownEndKey => $"FriendlySkeletonWand.{AbilityDef.ID}.CooldownEnd";

        public AbilityDefinition AbilityDef;

        protected Player _player;
        protected ZNetView _netView;

        public static readonly ConfigEntry<string>[] AbilityKeyCodes = new ConfigEntry<string>[AbilityController.AbilitySlotCount];
        public static ConfigEntry<TextAnchor> AbilityBarAnchor;
        public static ConfigEntry<Vector2> AbilityBarPosition;
        public static ConfigEntry<TextAnchor> AbilityBarLayoutAlignment;
        public static ConfigEntry<float> AbilityBarIconSpacing;

        public void CreateConfigs(BaseUnityPlugin plugin)
        {
            AbilityKeyCodes[0] = plugin.Config.Bind("Ability Hotbar (Client)", "Follow",
                "g", new ConfigDescription("Hotkey for Ability Slot 1."));
            AbilityKeyCodes[1] = plugin.Config.Bind("Ability Hotbar (Client)", "Wait",
                "h", new ConfigDescription("Hotkey for Ability Slot 2."));
            AbilityKeyCodes[2] = plugin.Config.Bind("Ability Hotbar (Client)", "Teleport",
                "j", new ConfigDescription("Hotkey for Ability Slot 3."));
            AbilityKeyCodes[3] = plugin.Config.Bind("Ability Hotbar (Client)", "Target",
                "g", new ConfigDescription("Hotkey for Ability Slot 1."));
            AbilityKeyCodes[4] = plugin.Config.Bind("Ability Hotbar (Client)", "SkeletonWarrior",
                "h", new ConfigDescription("Hotkey for Ability Slot 2."));
            AbilityKeyCodes[5] = plugin.Config.Bind("Ability Hotbar (Client)", "SkeletonArcher",
                "j", new ConfigDescription("Hotkey for Ability Slot 3."));
            AbilityKeyCodes[6] = plugin.Config.Bind("Ability Hotbar (Client)", "SkeletonMage",
                "g", new ConfigDescription("Hotkey for Ability Slot 1."));
            AbilityKeyCodes[7] = plugin.Config.Bind("Ability Hotbar (Client)", "PoisonSkeleton",
                "h", new ConfigDescription("Hotkey for Ability Slot 2."));
            AbilityKeyCodes[8] = plugin.Config.Bind("Ability Hotbar (Client)", "DraugrWarrior",
                "j", new ConfigDescription("Hotkey for Ability Slot 3."));
            AbilityKeyCodes[9] = plugin.Config.Bind("Ability Hotbar (Client)", "DraugrArcher",
                "j", new ConfigDescription("Hotkey for Ability Slot 3."));
            AbilityBarAnchor = plugin.Config.Bind("Ability Hotbar (Client)", "AbilityBarAnchor",
                TextAnchor.LowerLeft, new ConfigDescription("The point on the HUD to anchor the ability bar. Changing this also changes the pivot of the ability bar to that corner. For reference: the ability bar size is 208 by 64."));
            AbilityBarPosition = plugin.Config.Bind("Ability Hotbar (Client)", "AbilityBarPosition",
                new Vector2(150, 170), new ConfigDescription("The position offset from the Ability Bar Anchor at which to place the ability bar."));
            AbilityBarLayoutAlignment = plugin.Config.Bind("Ability Hotbar (Client)", "AbilityBarLayoutAlignment",
                TextAnchor.LowerLeft, new ConfigDescription("The Ability Bar is a Horizontal Layout Group. This value indicates how the elements inside are aligned. Choices with 'Center' in them will keep the items centered on the bar, even if there are fewer than the maximum allowed. 'Left' will be left aligned, and similar for 'Right'."));
            AbilityBarIconSpacing = plugin.Config.Bind("Ability Hotbar (Client)", "AbilityBarIconSpacing",
                8.0f, "The number of units between the icons on the ability bar.");
        }


        public virtual void Initialize(AbilityDefinition abilityDef, Player player)
        {
            AbilityDef = abilityDef;
            _player = player;
            _netView = _player.GetComponent<ZNetView>();
        }

        public virtual void OnUpdate()
        {
            if (AbilityDef.ActivationMode == AbilityActivationMode.Triggerable && ShouldTrigger())
            {
                TryActivate();
            }
        }

        protected virtual bool ShouldTrigger()
        {
            return false;
        }

        protected static float GetTime()
        {
            return (float)ZNet.instance.GetTimeSeconds();
        }

        public virtual bool IsOnCooldown()
        {
            if (HasCooldown())
            {
                return GetTime() < GetCooldownEndTime();
            }
            return false;
        }

        public virtual float TimeUntilCooldownEnds()
        {
            var cooldownEndTime = GetCooldownEndTime();
            return Mathf.Max(0, cooldownEndTime - GetTime());
        }

        public virtual float PercentCooldownComplete()
        {
            if (HasCooldown() && IsOnCooldown())
            {
                return 1.0f - (TimeUntilCooldownEnds() / AbilityDef.Cooldown);
            }

            return 1.0f;
        }

        public virtual bool CanActivate()
        {
            return !IsOnCooldown();
        }

        public virtual void TryActivate()
        {
            if (CanActivate())
            {
                Activate();
            }
        }

        protected virtual void Activate()
        {
            if (HasCooldown())
            {
                var cooldownEndTime = GetTime() + AbilityDef.Cooldown;
                SetCooldownEndTime(cooldownEndTime);
            }

            switch (AbilityDef.Action)
            {
                case AbilityAction.Custom:
                    ActivateCustomAction();
                    break;

                case AbilityAction.StatusEffect:
                    ActivateStatusEffectAction();
                    break;
            }
        }

        protected virtual void ActivateCustomAction()
        {
        }

        protected virtual void ActivateStatusEffectAction()
        {
            if (AbilityDef.Action != AbilityAction.StatusEffect)
            {
                Jotunn.Logger.LogError($"Tried to activate a status effect ability ({AbilityDef.ID}) that was not marked as Action=StatusEffect!");
                return;
            }

            var statusEffectName = AbilityDef.ActionParams.FirstOrDefault();
            if (string.IsNullOrEmpty(statusEffectName))
            {
                Jotunn.Logger.LogError($"Tried to activate a status effect ability ({AbilityDef.ID}) but the status effect name param was missing!");
                return;
            }

            var statusEffect = FriendlySkeletonWand.LoadAsset<StatusEffect>(statusEffectName);
            if (statusEffect == null)
            {
                Jotunn.Logger.LogError($"Tried to activate a status effect ability ({AbilityDef.ID}) but the status effect asset could not be found ({statusEffectName})!");
                return;
            }

            _player.GetSEMan().AddStatusEffect(statusEffect);
        }

        protected virtual bool HasCooldown()
        {
            return AbilityDef.Cooldown > 0;
        }

        protected virtual void SetCooldownEndTime(float cooldownEndTime)
        {
            _netView?.GetZDO()?.Set(CooldownEndKey, cooldownEndTime);
        }

        public virtual float GetCooldownEndTime()
        {
            return _netView?.GetZDO()?.GetFloat(CooldownEndKey) ?? 0;
        }

        public bool IsActivatedAbility()
        {
            return AbilityDef.ActivationMode == AbilityActivationMode.Activated;
        }

        public void ResetCooldown()
        {
            SetCooldownEndTime(0);
        }
    }
}
