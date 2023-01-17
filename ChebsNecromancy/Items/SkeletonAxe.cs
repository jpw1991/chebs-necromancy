using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonAxe : Item
    {
        public override string ItemName { get { return "ChebGonaz_SkeletonAxe"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonAxe.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_skeletonaxe_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_skeletonaxe_desc"; } }
    }
}
