using BepInEx.Configuration;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FriendlySkeletonWand
{
    internal class DraugrWand : Wand
    {
        public void Awake()
        {
            ItemName = "DraugrWand";
        }

        public override void CreateConfigs(BaseUnityPlugin plugin)
        {
            // todo
        }

        public override CustomItem GetCustomItem()
        {
            // todo
            return null;
        }

        public override KeyHintConfig GetKeyHint()
        {
            // todo
            return null;
        }

        public override bool HandleInputs()
        {
            // todo
            return false;
        }
    }
}
