namespace ChebsNecromancy.Items
{
    internal class BlackIronLegs : Item
    {
        public override string ItemName => "ChebGonaz_ArmorBlackIronLegs";
        public override string PrefabName => "ChebGonaz_ArmorBlackIronLegs.prefab";
        public override string NameLocalization => "$item_chebgonaz_blackironlegs_name";
        public override string DescriptionLocalization => "$item_chebgonaz_blackironlegs_desc";
        protected override string DefaultRecipe => "BlackMetal:5";
    }
}
