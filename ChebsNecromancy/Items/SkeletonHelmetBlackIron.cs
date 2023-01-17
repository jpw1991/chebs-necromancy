using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonHelmetBlackIron : Item
    {
        public override string ItemName { get { return "ChebGonaz_HelmetBlackIronSkeleton"; } }
        public override string PrefabName { get { return "ChebGonaz_HelmetBlackIronSkeleton.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_skeletonblackironhelmet_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_skeletonblackironhelmet_desc"; } }
    }
}
