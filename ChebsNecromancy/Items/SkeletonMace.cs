using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonMace : Item
    {
        public override string ItemName { get { return "ChebGonaz_SkeletonMace"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonMace.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_skeletonmace_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_skeletonmace_desc"; } }
    }
}
