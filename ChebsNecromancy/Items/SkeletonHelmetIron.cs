using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonHelmetIron : Item
    {
        // This is a copy of the HelmetIron item that is scaled slightly larger
        // to accomodate a skeleton's large dome

        public override string ItemName { get { return "ChebGonaz_SkeletonHelmetIron"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonHelmetIron.prefab"; } }
        public override string NameLocalization { get { return "$item_helmet_iron"; } }
        public override string DescriptionLocalization { get { return "$item_helmet_iron_description"; } }
    }
}
