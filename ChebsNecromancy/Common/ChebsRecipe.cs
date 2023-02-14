using System.Linq;
using System;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;
using ChebsNecromancy.Items;
using System.Reflection;

namespace ChebsNecromancy.Common
{
    public class ChebsRecipe
    {
        public string RecipeValue = "<Prefab1>:<quantity>[[,<PreFab2>:<quantity>], ...]";
        public ConfigEntry<bool> Allowed { get; set; }
        public ConfigEntry<string> CraftingCost { get; set; }
        public ConfigEntry<EcraftingTable> CraftingStationRequired { get; set; }
        public ConfigEntry<int> CraftingStationLevel {  get; set; }

        public enum EcraftingTable
        {
            None,
            [InternalName("piece_workbench")] Workbench,
            [InternalName("piece_cauldron")] Cauldron,
            [InternalName("forge")] Forge,
            [InternalName("piece_artisanstation")] ArtisanTable,
            [InternalName("piece_stonecutter")] StoneCutter
        }

        public string DefaultRecipe { get; set; }
        public string PieceTable { get; set; }
        public string PieceCategory { get; set; }
        public string RecipeName { get; set; }
        public string ItemName { get; set; }
        public string RecipeDescription { get; set; }
        public string PrefabName { get; set; }
        public string IconName { get; set; }
        public string ObjectName { get; set; }
        public EcraftingTable CraftingTable { get; set; }

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
            } else if (config.GetType() == typeof(ItemConfig))
            {
                config = new ItemConfig()
                {
                    Name = RecipeName,
                    Description = RecipeDescription
                };
            } else
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
                } else if (config.GetType() == typeof(ItemConfig))
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

                return (T) Convert.ChangeType(null, typeof(T));
            }

            if (config.GetType() == typeof(PieceConfig))
            {
                if ((customObjectFromPrefab as CustomPiece).PiecePrefab == null)
                {
                    Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s PiecePrefab is null!");
                    return (T)Convert.ChangeType(null, typeof(T));
                } else
                {
                    return (T)Convert.ChangeType(customObjectFromPrefab, typeof(T));
                }
            } else if (config.GetType() == typeof(ItemConfig))
            {
                if ((customObjectFromPrefab as CustomItem).ItemPrefab == null)
                {
                    Logger.LogError($"GetCustomItemFromPrefab: {PrefabName}'s ItemPrefab is null!");
                    return (T)Convert.ChangeType(null, typeof(T));
                } else
                {
                    return (T)Convert.ChangeType(customObjectFromPrefab, typeof(T));
                }
            } else
            {
                return (T)Convert.ChangeType(null, typeof(T));
            }
        }

        private void SetRecipeReqs<T>(
            T config,
            ConfigEntry<string> craftingCost,
            ConfigEntry<EcraftingTable> craftingStationRequired = null,
            ConfigEntry<int> craftingStationLevel = null
            )
        {
            // function to add a single material to the recipe
            void addMaterial(string material)
            {
                string[] materialSplit = material.Split(':');
                string materialName = materialSplit[0];
                int materialAmount = int.Parse(materialSplit[1]);
                if (config.GetType() == typeof(ItemConfig))
                {
                    (config as ItemConfig).AddRequirement(new RequirementConfig(materialName, materialAmount, 0, true));
                }
                else if (config.GetType() == typeof(PieceConfig))
                {
                    (config as PieceConfig).AddRequirement(new RequirementConfig(materialName, materialAmount, 0, true));
                }
            }

            // Settings for item configs only
            if (craftingStationRequired is not null)
            {              
                (config as ItemConfig).CraftingStation = ((InternalName)typeof(EcraftingTable).GetMember(
                    craftingStationRequired.Value.ToString())[0].GetCustomAttributes(
                    typeof(InternalName)).First()).Name;

                (config as ItemConfig).MinStationLevel = (craftingStationLevel is null ? 1 : craftingStationLevel.Value);
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
