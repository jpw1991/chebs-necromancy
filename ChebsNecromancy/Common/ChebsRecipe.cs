using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using System.Linq;
using UnityEngine;

namespace ChebsNecromancy.Common
{
    public class ChebsRecipe
    {
        public AcceptableValueList<string> RecipeValue = new("<Prefab1>:<quantity>[[,<PreFab2>:<quantity>], ...]");
        public ConfigEntry<bool> Allowed { get; set; }
        public ConfigEntry<string> CraftingCost { get; set; }
        public string DefaultRecipe { get; set; }
        public string PieceTable { get; set; }
        public string PieceCategory { get; set; }
        public string PieceName { get; set; }
        public string PieceDescription { get; set; }
        public string PrefabName { get; set; }
        public string IconName { get; set; }
                
        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new PieceConfig();
            config.Name = PieceName;
            config.Description = PieceDescription;

            if (Allowed.Value)
            {
                if (string.IsNullOrEmpty(CraftingCost.Value))
                {
                    CraftingCost.Value = DefaultRecipe;
                }

                SetRecipeReqs(config, CraftingCost);
            }
            else
            {
                config.Enabled = false;
            }

            config.Icon = icon;
            config.PieceTable = PieceTable;
            config.Category = PieceCategory;

            CustomPiece customPiece = new CustomPiece(prefab, false, config);
            if (customPiece == null)
            {
                Jotunn.Logger.LogError($"AddCustomPieces: {PrefabName}'s CustomPiece is null!");
                return null;
            }
            if (customPiece.PiecePrefab == null)
            {
                Jotunn.Logger.LogError($"AddCustomPieces: {PrefabName}'s PiecePrefab is null!");
                return null;
            }

            return customPiece;
        }

        private void SetRecipeReqs(PieceConfig config, ConfigEntry<string> craftingCost)
        {
            // function to add a single material to the recipe
            void addMaterial(string material)
            {
                string[] materialSplit = material.Split(':');
                string materialName = materialSplit[0];
                int materialAmount = int.Parse(materialSplit[1]);
                config.AddRequirement(new RequirementConfig(materialName, materialAmount, 0, true));
            }

            // build the recipe. material config format ex: Wood:5,Stone:1,Resin:1
            if (craftingCost.Value.Contains(','))
            {
                string[] materialList = craftingCost.Value.Split(',');

                foreach (string material in materialList)
                {
                    addMaterial(material);
                }
            }
            else
            {
                addMaterial(craftingCost.Value);
            }
        }
    }
}
