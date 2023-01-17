using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;

namespace ChebsNecromancy
{
    internal class BlackIronHelmet : Item
    {
        public override string ItemName { get { return "ChebGonaz_HelmetBlackIron"; } }
        public override string PrefabName { get { return "ChebGonaz_HelmetBlackIron.prefab"; } }
        public override string NameLocalization { get { return "$item_chebgonaz_blackironhelmet_name"; } }
        public override string DescriptionLocalization { get { return "$item_chebgonaz_blackironhelmet_desc"; } }
        protected override string DefaultRecipe { get { return "BlackMetal:5"; } }
    }
}
