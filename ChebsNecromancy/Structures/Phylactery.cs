using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private const string PhylacteryCheckString1 = "CG_1";
        private const string PhylacteryCheckString2 = "CG_2";
        private const string PhylacteryConsumeFuelString1 = "CG_3";

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

        private static Player GetPlayerFromSender(long sender)
        {
            foreach (var playerInfo in ZNet.instance.m_players)
            {
                var playerInfoSender = playerInfo.m_characterID.UserID;
                if (playerInfoSender == sender)
                {
                    return Player.s_players.Find(player => player.GetPlayerName() == playerInfo.m_name);
                }
            }

            return null;
        }
        
        //private static
        
        private static IEnumerator PhylacteryCheckRPCServerReceive(long sender, ZPackage package)
        {
            if (ZNet.instance == null) yield return null;
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // in the case of IsLocalInstance, client and server are the same so we need to filter out messages
                // that are coming from ourselves
                // if (ZDOMan.s_compareReceiver == sender) yield return null;
                
                // client only ever sends empty byte array, so abort if not this
                var payload = package.GetArray();
                if (payload.SequenceEqual(Encoding.UTF8.GetBytes(PhylacteryCheckString1)))
                {
                    Logger.LogInfo($"Received request from {sender} to check for an existing phylactery.");
                    
                    foreach (var playerInfo in ZNet.instance.m_players)
                    {
                        var playerInfoSender = playerInfo.m_characterID.UserID;
                        if (playerInfoSender == sender)
                        {
                            var playerCreatorID =
                                Player.s_players.Find(player => player.GetPlayerName() == playerInfo.m_name)?.GetPlayerID();
                            if (playerCreatorID != null)
                            {
                                var playerPhylactery = _phylacteries.Find(phylactery =>
                                    phylactery.TryGetComponent(out Piece piece)
                                    && piece.m_creator == playerCreatorID
                                    && phylactery.HasFuel());
                                var location = playerPhylactery != null
                                    ? Encoding.UTF8.GetBytes(PhylacteryCheckString2 + playerPhylactery.transform.position)
                                    : Encoding.UTF8.GetBytes(PhylacteryCheckString2);
                                PhylacteryCheckRPC.SendPackage(sender, new ZPackage(location));
                                break;
                            }
                        }
                    }
                }
                else if (payload.SequenceEqual(Encoding.UTF8.GetBytes(PhylacteryConsumeFuelString1)))
                {
                    Logger.LogInfo($"Received request from {sender} to consume fuel.");
                    var consumptionSuccessful = false;
                    foreach (var playerInfo in ZNet.instance.m_players)
                    {
                        var playerInfoSender = playerInfo.m_characterID.UserID;
                        if (playerInfoSender == sender)
                        {
                            var playerCreatorID =
                                Player.s_players.Find(player => player.GetPlayerName() == playerInfo.m_name)?.GetPlayerID();
                            if (playerCreatorID != null)
                            {
                                var playerPhylactery = _phylacteries.Find(phylactery =>
                                    phylactery.TryGetComponent(out Piece piece)
                                    && piece.m_creator == playerCreatorID);
                                if (playerPhylactery == null)
                                {
                                    Logger.LogError($"Trying to consume fuel for {playerInfo.m_name}'s phylactery, but it is null.");
                                }
                                else
                                {
                                    consumptionSuccessful = playerPhylactery.ConsumeFuel(1);
                                }
                                break;
                            }
                        }
                    }
                    if (!consumptionSuccessful) Logger.LogWarning($"Received request from {sender} to consume fuel, but was unable to do so.");
                }
                else if (payload.Length >= 3)
                {
                    var decoded = Encoding.UTF8.GetString(payload);
                    if (decoded.StartsWith(PhylacteryCheckString2))
                    {
                        Logger.LogInfo($"Received request from {sender} for phylactery location.");
                        ReceivePhylacteryLocation(decoded, sender);
                    }
                }
            }

            yield return null;
        }

        private static void ReceivePhylacteryLocation(string decoded, long sender)
        {
            Logger.LogInfo($"ReceivePhylacteryLocation {decoded} {sender}");

            // cthulhu, help me
            var phylacteryPositionStr = decoded.Replace(PhylacteryCheckString2, "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "");
            var phylacteryPositionXYZStr = phylacteryPositionStr.Split(',');
            if (phylacteryPositionXYZStr.Length != 3)
            {
                // if no vector is sent with it, then no phylactery must exist
                HasPhylactery = false;
                return;
            }
            Logger.LogInfo($"{phylacteryPositionXYZStr[0]} {phylacteryPositionXYZStr[1]} {phylacteryPositionXYZStr[2]}");
            var phylacteryVector3 = new Vector3(
                float.Parse(phylacteryPositionXYZStr[0]), 
                float.Parse(phylacteryPositionXYZStr[1]), 
                float.Parse(phylacteryPositionXYZStr[2])
            );
            var player = GetPlayerFromSender(sender);
            if (player != null)
            {
                HasPhylactery = true;
                PhylacteryLocation = phylacteryVector3;
            }
        }

        public static IEnumerator PhylacteryCheckRPCClientReceive(long sender, ZPackage package)
        {
            Logger.LogMessage($"PhylacteryCheckRPCClientReceive");
            var payload = package.GetArray();
            if (payload.Length >= 3)
            {
                var decoded = Encoding.UTF8.GetString(payload);
                if (decoded.StartsWith(PhylacteryCheckString2))
                {
                    ReceivePhylacteryLocation(decoded, sender);
                }
            }
            yield return null;
        }
        
        public static IEnumerator PhylacteriesCheck()
        {
            // Client should constantly check with the server for phylacteries
            while (true)
            {
                yield return new WaitUntil(() => ZNet.instance != null && Player.m_localPlayer != null);
                if (ZNet.instance.IsClientInstance() || ZNet.instance.IsLocalInstance())
                {
                    var package = new ZPackage(Encoding.UTF8.GetBytes(PhylacteryCheckString1));
                    PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
                }
                yield return new WaitForSeconds(5);
            }
        }

        public static void RequestConsumptionOfFuelForPlayerPhylactery()
        {
            var package = new ZPackage(Encoding.UTF8.GetBytes(PhylacteryConsumeFuelString1));
            PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
        }

        private ItemDrop GetFuelItemDrop()
        {
            var fuelPrefab = PrefabManager.Instance.GetPrefab(FuelPrefab.Value);
            if (fuelPrefab == null)
            {
                Logger.LogError("Phylactery.GetFuelItemDrop: fuelPrefab is null");
                return null;
            }
            
            if (!fuelPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                Logger.LogError("Phylactery.GetFuelItemDrop: fuelPrefab has no ItemDrop");
                return null;
            }

            return itemDrop;
        }

        private bool HasFuel()
        {
            var itemDrop = GetFuelItemDrop();
            return itemDrop != null && _inventory.HaveItem(itemDrop.m_itemData.m_shared.m_name);
        }

        public bool ConsumeFuel(int amount)
        {
            var itemDrop = GetFuelItemDrop();
            if (itemDrop != null && _inventory.CountItems(itemDrop.m_itemData.m_shared.m_name) >= amount)
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