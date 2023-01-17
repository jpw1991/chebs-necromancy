using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonFireballLevel1 : Item
    {
        public override string ItemName { get { return "ChebGonaz_FireballLevel1"; } }
        public override string PrefabName { get { return "ChebGonaz_FireballLevel1.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_fireballlevel1_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_fireballlevel1_desc"; } }
    }
}
