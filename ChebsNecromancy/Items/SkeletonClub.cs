using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonClub : Item
    {
        public override string ItemName { get { return "ChebGonaz_SkeletonClub"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonClub.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_skeletonclub_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_skeletonclub_desc"; } }
    }
}
