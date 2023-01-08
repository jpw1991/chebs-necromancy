using Jotunn.Entities;
using BepInEx.Configuration;
using BepInEx;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FriendlySkeletonWand
{

    public class InternalName : Attribute
    {
        public readonly string internalName;
        public InternalName(string internalName) => this.internalName = internalName;
    }

    public enum CraftingTable
    {
        None,
        [InternalName("piece_workbench")] Workbench,
        [InternalName("piece_cauldron")] Cauldron,
        [InternalName("forge")] Forge,
        [InternalName("piece_artisanstation")] ArtisanTable,
        [InternalName("piece_stonecutter")] StoneCutter
    }

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
