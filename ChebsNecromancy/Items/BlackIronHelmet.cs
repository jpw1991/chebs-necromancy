namespace ChebsNecromancy.Items
{
    internal class BlackIronHelmet : Item
    {
        public override string ItemName => "ChebGonaz_HelmetBlackIron";
        public override string PrefabName => "ChebGonaz_HelmetBlackIron.prefab";
        public override string NameLocalization => "$item_chebgonaz_blackironhelmet_name";
        public override string DescriptionLocalization => "$item_chebgonaz_blackironhelmet_desc";
        protected override string DefaultRecipe => "BlackMetal:5";
    }
}
