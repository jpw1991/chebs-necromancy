using System;
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
        // private static byte[] PhylacteryCheckString1Encoded => Encoding.UTF8.GetBytes(PhylacteryCheckString1);
        // private static byte[] PhylacteryCheckString2Encoded => Encoding.UTF8.GetBytes(PhylacteryCheckString2);

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
        
        private static IEnumerator PhylacteryCheckRPCServerReceive(long sender, ZPackage package)
        {
            Logger.LogInfo($"receive 1 - _phylacteries.Count={_phylacteries.Count}");
            if (ZNet.instance == null) yield return null;
            Logger.LogInfo("receive 2");
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                // in the case of IsLocalInstance, client and server are the same so we need to filter out messages
                // that are coming from ourselves
                // if (ZDOMan.s_compareReceiver == sender) yield return null;
                
                // client only ever sends empty byte array, so abort if not this
                var payload = package.GetArray();
                Logger.LogInfo($"receive 3 (length={payload.Length})");
                if (payload.SequenceEqual(Encoding.UTF8.GetBytes(PhylacteryCheckString1)))
                {
                    // receiving from client
                    
                    foreach (var playerInfo in ZNet.instance.m_players)
                    {
                        var playerInfoSender = playerInfo.m_characterID.UserID;
                        //Logger.LogInfo($"receive 3.1 playerInfoSender={playerInfoSender}, playerInfoName={playerInfo.m_name}");
                        if (playerInfoSender == sender)
                        {
                            var playerCreatorID =
                                Player.s_players.Find(player => player.GetPlayerName() == playerInfo.m_name)?.GetPlayerID();
                            if (playerCreatorID != null)
                            {
                                Logger.LogInfo($"receive 4 - playerInfoSender={playerInfoSender} playerCreatorID={playerCreatorID}");
                                var playerPhylactery = _phylacteries.Find(phylactery =>
                                    phylactery.TryGetComponent(out Piece piece)
                                    && piece.m_creator == playerCreatorID
                                    && phylactery.HasFuel());
                                Logger.LogInfo($"receive 5 - {_phylacteries[0].GetComponent<Piece>().m_creator} {sender}");
                                var location = playerPhylactery != null
                                    ? Encoding.UTF8.GetBytes(PhylacteryCheckString2 + playerPhylactery.transform.position)
                                    : Encoding.UTF8.GetBytes(PhylacteryCheckString2);
                                Logger.LogInfo($"receive 6 - sending {Encoding.UTF8.GetString(location)}");
                                PhylacteryCheckRPC.SendPackage(sender, new ZPackage(location));
                                break;
                            }
                        }
                    }
                }
                else if (payload.Length >= 3)
                {
                    var decoded = Encoding.UTF8.GetString(payload);
                    if (decoded.StartsWith(PhylacteryCheckString2))
                    {
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
                Jotunn.Logger.LogInfo("tick");
                yield return new WaitUntil(() => ZNet.instance != null && Player.m_localPlayer != null);
                if (ZNet.instance.IsClientInstance() || ZNet.instance.IsLocalInstance())
                {
                    Jotunn.Logger.LogInfo("tock");
                    var package = new ZPackage(Encoding.UTF8.GetBytes(PhylacteryCheckString1));
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