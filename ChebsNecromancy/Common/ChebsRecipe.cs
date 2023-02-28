using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ChebsNecromancy.Items;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Common
{
    public class ChebsRecipe
    {
        public string RecipeValue = "<Prefab1>:<quantity>[[,<PreFab2>:<quantity>], ...]";
        public ConfigEntry<bool> Allowed { get; set; }
        public ConfigEntry<string> CraftingCost { get; set; }
        public string DefaultRecipe { get; set; }
        public string PieceTable { get; set; }
        public string PieceCategory { get; set; }
        public string PieceName { get; set; }
        public string PieceDescription { get; set; }
        public string PrefabName { get; set; }
        public string IconName { get; set; }
        public string ObjectName { get; set; }

        public virtual void UpdateRecipe(ConfigEntry<string> craftingCost)
        {
            var prefabNameNoExt = PrefabName.Split('.')[0];
            Logger.LogInfo($"Updating crafting cost for {prefabNameNoExt}");
            var piece = PieceManager.Instance.GetPiece(prefabNameNoExt).Piece;
            var newRequirements = new List<Piece.Requirement>();
            foreach (string material in craftingCost.Value.Split(','))
            {
                var materialSplit = material.Split(':');
                var materialName = materialSplit[0];
                var materialAmount = int.Parse(materialSplit[1]);
                newRequirements.Add(new Piece.Requirement()
                {
                    m_amount = materialAmount,
                    m_amountPerLevel = materialAmount * 2,
                    m_resItem = ZNetScene.instance.GetPrefab(materialName).GetComponent<ItemDrop>(),
                });
            }

            piece.m_resources = newRequirements.ToArray();
        }

        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new()
            {
                Name = PieceName,
                Description = PieceDescription
            };

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

            CustomPiece customPiece = new(prefab, false, config);
            if (customPiece == null)
            {
                Logger.LogError($"AddCustomPieces: {PrefabName}'s CustomPiece is null!");
                return null;
            }
            if (customPiece.PiecePrefab == null)
            {
                Logger.LogError($"AddCustomPieces: {PrefabName}'s PiecePrefab is null!");
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
