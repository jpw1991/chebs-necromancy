using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class BlackIronLegs : Item
    {
        public override string ItemName { get { return "ChebGonaz_ArmorBlackIronLegs"; } }
        public override string PrefabName { get { return "ChebGonaz_ArmorBlackIronLegs.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_blackironlegs_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_blackironlegs_desc"; } }
        protected override string DefaultRecipe { get { return "BlackMetal:5"; } }
    }
}
