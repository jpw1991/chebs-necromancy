using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class BlackIronChest : Item
    {
        public override string ItemName { get { return "ChebGonaz_ArmorBlackIronChest"; } }
        public override string PrefabName { get { return "ChebGonaz_ArmorBlackIronChest.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_blackironchest_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_blackironchest_desc"; } }
        protected override string DefaultRecipe { get { return "BlackMetal:5"; } }
    }
}
