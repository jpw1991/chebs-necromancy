using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Structures;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class FarmingPylon : Structure
    {
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> UpdateInterval;
        public static MemoryConfigEntry<string, List<string>> PickableList;

        private const string DefaultPickables =
            "Pickable_Barley,Pickable_Barley_Wild,Pickable_Carrot,Pickable_Dandelion,Pickable_Flax,Pickable_Flax_Wild,Pickable_Mushroom,Pickable_Mushroom_blue,Pickable_Mushroom_JotunPuffs,Pickable_Mushroom_Magecap,Pickable_Mushroom_yellow,Pickable_Onion,Pickable_SeedCarrot,Pickable_SeedOnion,Pickable_SeedTurnip,Pickable_Thistle,Pickable_Turnip";

        private int _itemMask;
        private int _pieceMaskNonSolid;

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

            var pickableList = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "FarmingPylonPickableList",
                DefaultPickables,
                "A list of pickable IDs.", null, true);
            PickableList = new MemoryConfigEntry<string, List<string>>(pickableList, s => s?.Split(',').Select(str => str.Trim()).ToList());
        }

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        private void Awake()
        {
            _itemMask = LayerMask.GetMask("item");
            _pieceMaskNonSolid = LayerMask.GetMask("piece_nonsolid");
            if (TryGetComponent(out ArmorStand armorStand))
            {
                // for some reason, you gotta go through each slot and adjust the name otherwise it will remain as
                // the basic armorstand localization rather than the cheb one
                armorStand.m_slots.ForEach(slot => { slot.m_switch.m_hoverText = armorStand.m_name; });
            }

            StartCoroutine(LookForCrops());
        }

        IEnumerator LookForCrops()
        {
            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(UpdateInterval.Value);

                if (!piece.m_nview.IsOwner()) continue;

                var playersInRange = new List<Player>();
                Player.GetPlayersInRange(transform.position, PlayerDetectionDistance, playersInRange);
                if (playersInRange.Count < 1) continue;

                yield return new WaitWhile(() => playersInRange[0].IsSleeping());

                PickPickables();
            }
            // ReSharper disable once IteratorNeverReturns
        }

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
                if (pickable != null && PickableList.Value.Exists(item => pickable.name.Contains(item)))
                {
                    pickable.m_nview.InvokeRPC("RPC_Pick");
                }
            }
        }
    }
}