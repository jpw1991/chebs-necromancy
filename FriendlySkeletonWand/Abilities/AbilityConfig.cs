using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriendlySkeletonWand.Abilities
{
    [Serializable]
    public enum AbilityActivationMode
    {
        Passive,
        Triggerable,
        Activated,
        Toggleable
    }

    [Serializable]
    public enum AbilityAction
    {
        Custom,
        StatusEffect
    }

    [Serializable]
    public class AbilityDefinition
    {
        public string ID;
        public string IconAsset;
        public AbilityActivationMode ActivationMode;
        public float Cooldown;
        public AbilityAction Action;
        public List<string> ActionParams = new List<string>();
    }

    [Serializable]
    public class AbilityConfig
    {
        public List<AbilityDefinition> Abilities;
    }
}
