using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class Wand : MonoBehaviour
    {
        public string ItemName;
        public List<ButtonConfig> buttonConfigs = new List<ButtonConfig>();

        public virtual void CreateConfigs(BaseUnityPlugin plugin) { }

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
    }
}
