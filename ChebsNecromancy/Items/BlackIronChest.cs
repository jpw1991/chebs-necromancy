namespace ChebsNecromancy.Items
{
    internal class BlackIronChest : Item
    {
        public override string ItemName => "ChebGonaz_ArmorBlackIronChest";
        public override string PrefabName => "ChebGonaz_ArmorBlackIronChest.prefab";
        public override string NameLocalization => "$item_chebgonaz_blackironchest_name";
        public override string DescriptionLocalization => "$item_chebgonaz_blackironchest_desc";
        protected override string DefaultRecipe => "BlackMetal:5";
    }
}
