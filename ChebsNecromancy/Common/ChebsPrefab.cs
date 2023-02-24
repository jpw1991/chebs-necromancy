using Jotunn.Configs;
using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Piece;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Common
{
    internal class ChebsPrefab
    {
        public bool Allowed { get; set; }
        public string RecipeName { get; set; }
        public string RecipeDescription { get; set; }
        public string PieceTable { get; set; }
        public string PieceCategory { get; set; }
        public string DefaultRecipe { get; set; }
        public string CraftingCost { get; set; }
        public string PrefabName { get; set; }

        public CustomPiece GetCustomPieceFromPrefab<T>(GameObject prefab, Sprite icon = null)
        {
            return GetCustomRecipeFromPrefab<T>(prefab, typeof(PieceConfig), icon) as CustomPiece;
        }

        public CustomItem GetCustomItemFromPrefab<T>(GameObject prefab, Sprite icon = null)
        {
            return GetCustomRecipeFromPrefab<T>(prefab, typeof(ItemConfig), icon) as CustomItem;
        }

        private T GetCustomRecipeFromPrefab<T>(GameObject prefab, Type type, Sprite icon = null)
        {
            object config = Activator.CreateInstance(type);
            if (config.GetType() == typeof(PieceConfig))
            {
                config = new PieceConfig()
                {
                    Name = RecipeName,
                    Description = RecipeDescription,
                    Icon = icon,
                    PieceTable = PieceTable,
                    Category = PieceCategory
                };
            }
            else if (config.GetType() == typeof(ItemConfig))
            {
                config = new ItemConfig()
                {
                    Name = RecipeName,
                    Description = RecipeDescription
                };
            }
            else
            {
                return (T)Convert.ChangeType(null, typeof(T));
            }

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
                if (config.GetType() == typeof(PieceConfig))
                {
                    (config as PieceConfig).Enabled = false;
                }
                else if (config.GetType() == typeof(ItemConfig))
                {
                    (config as ItemConfig).Enabled = false;
                }

                return (T)Convert.ChangeType(null, typeof(T));
            }

            object customObjectFromPrefab = type.GetType() == typeof(PieceConfig) ? Activator.CreateInstance(
                typeof(CustomPiece), prefab, false, config) : Activator.CreateInstance(
                typeof(CustomItem), prefab, false, config);

            if (customObjectFromPrefab == null)
            {
                Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s customObjectFromPrefab is null!");

                return (T)Convert.ChangeType(null, typeof(T));
            }

            if (config.GetType() == typeof(PieceConfig))
            {
                if ((customObjectFromPrefab as CustomPiece).PiecePrefab == null)
                {
                    Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s PiecePrefab is null!");
                    return (T)Convert.ChangeType(null, typeof(T));
                }
                else
                {
                    return (T)Convert.ChangeType(customObjectFromPrefab, typeof(T));
                }
            }
            else if (config.GetType() == typeof(ItemConfig))
            {
                if ((customObjectFromPrefab as CustomItem).ItemPrefab == null)
                {
                    Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s ItemPrefab is null!");
                    return (T)Convert.ChangeType(null, typeof(T));
                }
                else
                {
                    return (T)Convert.ChangeType(customObjectFromPrefab, typeof(T));
                }
            }
            else
            {
                return (T)Convert.ChangeType(null, typeof(T));
            }
        }
    }
}
