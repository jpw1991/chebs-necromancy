using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Common;
using ChebsNecromancy.Minions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ChebsNecromancy.Structures
{
    internal class TreasurePylonEffect : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return new WaitForSeconds(5);
            ZNetScene.instance.Destroy(gameObject);
        }
    }
    
    internal class TreasurePylon : Structure
    {
        public const string EffectName = "ChebGonaz_TreasurePylonEffect";

        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> UpdateInterval;
        public static ConfigEntry<string> ContainerWhitelist;

        private readonly int pieceMask = LayerMask.GetMask("piece");

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Coins:200,Ruby:2",
            IconName = "chebgonaz_treasurepylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_treasurepylon_name",
            PieceDescription = "$chebgonaz_treasurepylon_desc",
            PrefabName = "ChebGonaz_TreasurePylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "Allowed", true,
                "Whether making a Treasure Pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "BuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build a Treasure Pylon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SightRadius = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "SightRadius", 20f,
                "How far a Treasure Pylon can reach containers.", plugin.FloatQuantityValue, true);

            UpdateInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "UpdateInterval", 30f,
                "How long a Treasure Pylon waits between checking containers (lower values may negatively impact performance).",
                plugin.FloatQuantityValue, true);
            
            ContainerWhitelist = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerWhitelist", "piece_chest_wood",
                "The containers that are sorted. Please use a comma-delimited list of prefab names.",
                null, true);
        }

        private void Awake()
        {
            StartCoroutine(LookForPieces());
        }

        IEnumerator LookForPieces()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());
            
            while (true)
            {
                yield return new WaitForSeconds(UpdateInterval.Value + Random.value);
                yield return new WaitWhile(() => Player.m_localPlayer.m_sleeping);

                var allowedContainers = ContainerWhitelist.Value.Split(',').ToList();
                var nearbyContainers = UndeadMinion.FindNearby<Container>(transform, SightRadius.Value, pieceMask,
                    c => c.m_piece.IsPlacedByPlayer() 
                         && allowedContainers.Contains(c.m_piece.m_nview.GetPrefabName()), true);

                for (int i = 0; i < nearbyContainers.Count; i++)
                {
                    yield return new WaitWhile(() => Player.m_localPlayer.m_sleeping);
                    
                    // make a fancy effect on the container being processed
                    var effect = Instantiate(ZNetScene.instance.GetPrefab(EffectName));
                    effect.transform.position = nearbyContainers[i].transform.position + Vector3.up;
                    effect.AddComponent<TreasurePylonEffect>();

                    //var movedLog = new List<string>();

                    // 1. Make note of contents of current container
                    // 2. Check every other container in the list and if one of these has the same object as the
                    //    current container, then remove it and put it in the current container.
                    // 3. Repeat until:
                    //    a) Container is full
                    //    b) or list has been exhausted
                    // 4. Once finished, repeat for other containers in list
                    var currentContainerInventory = nearbyContainers[i].GetInventory();
                    //var currentContainerItems = currentContainerInventory.GetAllItems();

                    for (int j = 0; j < nearbyContainers.Count; j++)
                    {
                        if (j == i) continue; // skip over self

                        var jInventory = nearbyContainers[j].GetInventory();
                        var jItems = jInventory.GetAllItems();

                        for (int k = jItems.Count - 1; k > -1; k--)
                        {
                            var jItem = jItems[k];
                            var itemsMoved = 0;
                            if (currentContainerInventory.CanAddItem(jItem))
                            {
                                var currentItemCount = currentContainerInventory.CountItems(jItem.m_shared.m_name);
                                currentContainerInventory.AddItem(jItem);
                                itemsMoved = currentContainerInventory.CountItems(jItem.m_shared.m_name) -
                                             currentItemCount;
                            }

                            if (itemsMoved > 0)
                            {
                                //movedLog.Add($"{itemsMoved} {jItem.m_shared.m_name}");
                                jInventory.RemoveItem(jItem, itemsMoved);
                            }
                        }
                    }

                    //Jotunn.Logger.LogInfo($"Moved {string.Join(",", movedLog)} to {currentContainerInventory.GetName()}");
                    yield return new WaitForSeconds(5);
                }
            }
        }
    }
}