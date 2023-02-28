using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class FarmingPylon : Structure
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> UpdateInterval;
        public static ConfigEntry<string> PickableList;

        private const string DefaultPickables =
            "Pickable_Barley,Pickable_Barley_Wild,Pickable_Carrot,Pickable_Dandelion,Pickable_Flax,Pickable_Flax_Wild,Pickable_Mushroom,Pickable_Mushroom_blue,Pickable_Mushroom_JotunPuffs,Pickable_Mushroom_Magecap,Pickable_Mushroom_yellow,Pickable_Onion,Pickable_SeedCarrot,Pickable_SeedOnion,Pickable_SeedTurnip,Pickable_Thistle,Pickable_Turnip";

        private int _itemMask;
        private int _pieceMaskNonSolid;
        private List<string> _pickableList;

        private List<Container> _containers;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "FineWood:15,IronNails:15,SurtlingCore:1",
            IconName = "chebgonaz_farmingpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_farmingpylon_name",
            PieceDescription = "$chebgonaz_farmingpylon_desc",
            PrefabName = "ChebGonaz_FarmingPylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FarmingPylonAllowed", true,
                "Whether making a Farming Pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FarmingPylonBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build a Farming Pylon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SightRadius = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FarmingPylonSightRadius", 30f,
                "How far a Farming Pylon can reach  and crops.", plugin.FloatQuantityValue, true);

            UpdateInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FarmingPylonUpdateInterval", 5f,
                "How long a Farming Pylon waits between checking containers and crops (lower values may negatively impact performance).",
                plugin.FloatQuantityValue, true);

            PickableList = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FarmingPylonPickableList", DefaultPickables,
                "A list of pickable IDs.", null, true);
        }
        
        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        private void Awake()
        {
            _itemMask = LayerMask.GetMask("item");
            _pieceMaskNonSolid = LayerMask.GetMask("piece_nonsolid");
            _pickableList = PickableList.Value.Split(',').ToList();
            StartCoroutine(LookForCrops());
        }

        IEnumerator LookForCrops()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(UpdateInterval.Value);

                //_containers = GetNearbyContainers();
                
                PickPickables();
            }
        }
        
        // private List<Container> GetNearbyContainers()
        // {
        //     // gets nearby containers, and also picks pickables
        //     var containers = new List<Container>();
        //     
        //     // get empty containers in order of closest to furthest
        //     Collider[] nearbyPieces =
        //         Physics.OverlapSphere(transform.position + Vector3.up, SightRadius.Value, _pieceMask);
        //     if (nearbyPieces.Length < 1) return containers;
        //     
        //     nearbyPieces
        //         .OrderBy(piece => Vector3.Distance(transform.position, piece.transform.position))
        //         .Where(piece =>
        //         {
        //             var container = piece.GetComponentInParent<Container>();
        //             if (container != null && container.GetInventory().GetEmptySlots() > 0)
        //             {
        //                 containers.Add(container);
        //                 return true;
        //             }
        //             return false;
        //         });
        //     return containers;
        // }

        private void PickPickables()
        {
            // pick all nearby pickables
            var position = transform.position;
            var nearbyPickables =
                Physics.OverlapSphere(position, SightRadius.Value, _pieceMaskNonSolid)
                    .Concat(Physics.OverlapSphere(position, SightRadius.Value, _itemMask)).ToArray();
            if (nearbyPickables.Length < 1) return;

            foreach (var col in nearbyPickables)
            {
                var pickable = col.GetComponentInParent<Pickable>();
                if (pickable != null && _pickableList.Exists(item => pickable.name.Contains(item)))//.Contains(pickable.m_itemPrefab.name))
                {
                    pickable.m_nview.InvokeRPC("Pick");
                }
            }
        }
    }
}