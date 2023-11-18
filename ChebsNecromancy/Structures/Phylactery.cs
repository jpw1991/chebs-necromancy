using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Structures;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Structures
{
    internal class Phylactery : Structure
    {
        public static ConfigEntry<string> FuelPrefab;

        // updated by client with info from server
        public static bool HasPhylactery;
        public static Vector3 PhylacteryLocation = Vector3.zero;

        // updated by server periodically
        private static List<Phylactery> _phylacteries = new ();
        
        private Container _container;
        private Inventory _inventory;
        
        public static CustomRPC PhylacteryCheckRPC;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:100,Coal:100",
            IconName = "chebgonaz_phylactery_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_phylactery_name",
            PieceDescription = "$chebgonaz_phylactery_desc",
            PrefabName = "ChebGonaz_Phylactery.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PhylacteryAllowed", true,
                "Whether making a Phylactery  is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "PhylacteryBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build a Phylactery . None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);
            
            FuelPrefab = plugin.Config.Bind(ChebsRecipeConfig.ObjectName, "Fuel",
                "DragonEgg", new ConfigDescription("The prefab name that is consumed as fuel.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        public static void ConfigureRPC()
        {
            PhylacteryCheckRPC = NetworkManager.Instance.AddRPC("PhylacteryCheckRPC",
                PhylacteryCheckRPCServerReceive, PhylacteryCheckRPCClientReceive);
        }
        
        private static IEnumerator PhylacteryCheckRPCServerReceive(long sender, ZPackage package)
        {
            Logger.LogInfo("receive 1");
            if (ZNet.instance == null) yield return null;
            Logger.LogInfo("receive 2");
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                var playerPhylactery = _phylacteries.Find(phylactery =>
                    phylactery.TryGetComponent(out Piece piece)
                    && piece.m_creator == sender
                    && phylactery.HasFuel());
                var location = playerPhylactery != null
                    ? Encoding.UTF8.GetBytes(playerPhylactery.transform.position.ToString())
                    : Array.Empty<byte>();
                PhylacteryCheckRPC.SendPackage(sender, new ZPackage(location));
            }

            yield return null;
        }

        public static IEnumerator PhylacteryCheckRPCClientReceive(long sender, ZPackage package)
        {
            var locationString = package.ReadString();
            Logger.LogMessage($"Phylactery found: {locationString}");
            //HasPhylactery = package.ReadString() == "y";
            yield return null;
        }
        
        public static IEnumerator PhylacteriesCheck()
        {
            // Client should constantly check with the server for phylacteries
            while (true)
            {
                Jotunn.Logger.LogInfo("tick");
                yield return new WaitUntil(() => ZNet.instance != null && Player.m_localPlayer != null);
                if (ZNet.instance.IsClientInstance() || ZNet.instance.IsLocalInstance())
                {
                    Jotunn.Logger.LogInfo("tock");
                    var package = new ZPackage(Array.Empty<byte>());
                    PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
                }
                yield return new WaitForSeconds(5);
            }
        }

        private bool HasFuel()
        {
            var fuelPrefab = PrefabManager.Instance.GetPrefab(FuelPrefab.Value);
            if (fuelPrefab == null)
            {
                Logger.LogError("Phylactery.ConsumeFuel: fuelPrefab is null");
                return false;
            }
            
            if (!fuelPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                Logger.LogError("Phylactery.ConsumeFuel: fuelPrefab has no ItemDrop");
                return false;
            }

            return _inventory.HaveItem(itemDrop.m_itemData.m_shared.m_name);
        }

        public bool ConsumeFuel(int amount)
        {
            var fuelPrefab = PrefabManager.Instance.GetPrefab(FuelPrefab.Value);
            if (fuelPrefab == null)
            {
                Logger.LogError("Phylactery.ConsumeFuel: fuelPrefab is null");
                return false;
            }
            
            if (!fuelPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                Logger.LogError("Phylactery.ConsumeFuel: fuelPrefab has no ItemDrop");
                return false;
            }

            if (_inventory.CountItems(itemDrop.m_itemData.m_shared.m_name) >= amount)
            {
                _inventory.RemoveItem(itemDrop.m_itemData.m_shared.m_name, amount);
                return true;
            }
            return false;
        }

        private void Awake()
        {
            StartCoroutine(Wait());
        }

        private void OnDestroy()
        {
            _phylacteries.Remove(this);
        }

        private IEnumerator Wait()
        {
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());
            
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // add to phylactery list if on server
                if (!_phylacteries.Contains(this))
                    _phylacteries.Add(this);
            }

            _container = gameObject.AddComponent<Container>();
            _container.m_name = "$chebgonaz_phylactery_name";

            _inventory = _container.GetInventory();
            _inventory.m_name = Localization.instance.Localize(_container.m_name);
        }
    }
}