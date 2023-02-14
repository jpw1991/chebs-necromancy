using System.Reflection;

namespace ChebsNecromancy.Items.MinionItems
{
    internal class SkeletonHelmetBronzePoison : Item
    {
        // This is a copy of the HelmetBronze item that is scaled slightly larger
        // to accomodate a skeleton's large dome
        public SkeletonHelmetBronzePoison()
        {
            ChebsRecipeConfig.ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name;
            ChebsRecipeConfig.RecipeName = "$item_chebgonaz_" + ChebsRecipeConfig.ObjectName.ToLower() + "_name";
            ChebsRecipeConfig.ItemName = "ChebGonaz_" + ChebsRecipeConfig.ObjectName;
            ChebsRecipeConfig.RecipeDescription = "$item_chebgonaz_" + ChebsRecipeConfig.ObjectName.ToLower() + "_desc";
            ChebsRecipeConfig.PrefabName = "ChebGonaz_" + ChebsRecipeConfig.ObjectName + ".prefab";

        }
    }
}
