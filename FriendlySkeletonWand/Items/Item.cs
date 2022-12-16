using Jotunn.Entities;
using BepInEx.Configuration;
using BepInEx;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class Item
    {
        public ConfigEntry<bool> allowed;

        //protected virtual string itemName = "";
        public virtual string ItemName { get { return ""; } }
        //protected const string prefabName = "";
        public virtual string PrefabName { get { return ""; } }

        public virtual void CreateConfigs(BaseUnityPlugin plugin) {}

        public virtual CustomItem GetCustomItem(Sprite icon=null)
        {
            return null;
        }
    }
}
