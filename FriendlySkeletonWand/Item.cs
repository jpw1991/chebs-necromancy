using Jotunn.Entities;
using BepInEx.Configuration;
using BepInEx;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class Item
    {
        public ConfigEntry<bool> allowed;

        public string ItemName;

        public virtual void CreateConfigs(BaseUnityPlugin plugin) {}

        public virtual CustomItem GetCustomItem(Sprite icon=null)
        {
            return null;
        }
    }
}
