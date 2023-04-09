using System.Reflection;
using ChebsValheimLibrary.Common;

namespace ChebsNecromancy.Structures
{
    internal class BatLantern : BatBeacon
    {
        public new static readonly ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "FineWood:10,Silver:5,Guck:15",
            IconName = "chebgonaz_batlantern_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_batlantern_name",
            PieceDescription = "$chebgonaz_batlantern_desc",
            PrefabName = "ChebGonaz_BatLantern.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };
        
        public new static void CreateConfigs(BasePlugin plugin)
        {         
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatLanternAllowed", true,
                "Whether making a bat lantern is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BatLanternBuildCosts", 
                ChebsRecipeConfig.DefaultRecipe, 
                "Materials needed to build the bat lantern. None or Blank will use Default settings. Format: " + ChebsRecipeConfig.RecipeValue, 
                null, true);
        }
    }
}