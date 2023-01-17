using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonHelmetBronzePoison : Item
    {
        // This is a copy of the HelmetBronze item that is scaled slightly larger
        // to accomodate a skeleton's large dome

        public override string ItemName { get { return "ChebGonaz_SkeletonHelmetBronzePoison"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonHelmetBronzePoison.prefab"; } }
        public override string NameLocalization { get { return "$item_helmet_bronze"; } }
        public override string DescriptionLocalization { get { return "$item_helmet_bronze_description"; } }
    }
}
