using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonBow : Item
    {
        public override string ItemName { get { return "ChebGonaz_SkeletonBow"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonBow.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_skeletonbow_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_skeletonbow_desc"; } }
    }
}
