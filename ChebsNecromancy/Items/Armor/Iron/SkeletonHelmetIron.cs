namespace ChebsNecromancy.Items
{
    internal class SkeletonHelmetIron : Item
    {
        // This is a copy of the HelmetIron item that is scaled slightly larger
        // to accomodate a skeleton's large dome

        public override string ItemName => "ChebGonaz_SkeletonHelmetIron";
        public override string PrefabName => "ChebGonaz_SkeletonHelmetIron.prefab";
        public override string NameLocalization => "$item_helmet_iron";
        public override string DescriptionLocalization => "$item_helmet_iron_description";
    }
}
