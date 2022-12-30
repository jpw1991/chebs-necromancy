using Jotunn.Entities;
using BepInEx.Configuration;
using BepInEx;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class Item
    {
        public ConfigEntry<bool> allowed;

        public virtual string ItemName { get { return ""; } }
        public virtual string PrefabName { get { return ""; } }

        public virtual void CreateConfigs(BaseUnityPlugin plugin) {}

        public virtual CustomItem GetCustomItem(Sprite icon=null)
        {
            return null;
        }

        // coroutines cause problems and this is not a monobehavior, but we
        // may still want some stuff to happen during update.
        protected float doOnUpdateDelay;
        public virtual void DoOnUpdate()
        {

        }
    }
}
