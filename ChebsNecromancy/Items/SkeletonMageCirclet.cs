using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class SkeletonMageCirclet : Item
    {
        // This is a copy of the HelmetDverger item that is scaled slightly larger
        // to accomodate a skeleton's large dome and also has different colors

        public override string ItemName { get { return "ChebGonaz_SkeletonMageCirclet"; } }
        public override string PrefabName { get { return "ChebGonaz_SkeletonMageCirclet.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_magecirclet_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_magecirclet_desc"; } }
    }
}
