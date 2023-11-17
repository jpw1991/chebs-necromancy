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
        //public static List<Phylactery> Phylacteries = new ();

        public static ConfigEntry<string> FuelPrefab;

        private Container _container;
        private Inventory _inventory;
        
        public static CustomRPC PhylacteryCheckRPC;
        // updated by server periodically if the client has a phylactery
        public static bool HasPhylactery = false;

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

        // public IEnumerator PhylacteryCheckRPCServerReceive(long sender, ZPackage package)
        // {
        //     var phylacteryHash = "ChebGonaz_Phylactery".GetStableHashCode();
        //     var phylacteryFound = "n";
        //     foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
        //     {
        //         if (zdo.GetPrefab() == phylacteryHash && zdo.GetOwner() == package.ReadLong())
        //         {
        //             phylacteryFound = "y";
        //             break;
        //         }
        //     }
        //     
        //     PhylacteryCheckRPC.SendPackage(sender, new ZPackage(phylacteryFound));
        //
        //     yield return null;
        // }

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
                var phylacteryHash = "ChebGonaz_Phylactery".GetStableHashCode();
                var phylacteryFound = "n";
                foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
                {
                    var packageContent = package.ReadByteArray();
                    var s = Encoding.Convert(Encoding.UTF8, Encoding.UTF8, packageContent);
                    if (zdo.GetPrefab() == phylacteryHash && zdo.GetOwner().ToString() == s.ToString())
                    {
                        var prefab = ObjectDB.instance.GetItemPrefab(zdo.GetPrefab());
                        if (prefab != null && prefab.TryGetComponent(out Phylactery phylactery)
                                           && phylactery.ConsumeFuel(0))
                        {
                            phylacteryFound = "y";
                            break;
                        }
                    }
                }
            
                PhylacteryCheckRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(phylacteryFound));
            }

            yield return null;
        }

        public static IEnumerator PhylacteryCheckRPCClientReceive(long sender, ZPackage package)
        {
            Logger.LogMessage($"Phylactery found: {package.ReadString()}");
            HasPhylactery = package.ReadString() == "y";
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
                    var package = new ZPackage(Encoding.UTF8.GetBytes(Player.m_localPlayer.GetPlayerID().ToString()));
                    //package.Write(Player.m_localPlayer.GetPlayerID());
                    PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
                }
                yield return new WaitForSeconds(5);
            }
            // yield return new WaitUntil(() => ZNet.instance != null);
            // if (ZNet.instance.IsClientInstance())
            // {
            //     while (true)
            //     {
            //         var package = new ZPackage();
            //         package.Write(Player.m_localPlayer.GetPlayerID());
            //         PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
            //         yield return new WaitForSeconds(5);   
            //     }
            // }
        }

        public bool ConsumeFuel(int amount = 1)
        {
            var fuelPrefab = ZNetScene.instance.GetPrefab(FuelPrefab.Value);
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

            // why in the nine hells won't this work?
            //return _inventory.RemoveOneItem(fuelPrefab.GetComponent<ItemDrop>().m_itemData);

            if (_inventory.HaveItem(itemDrop.m_itemData.m_shared.m_name))
            {
                if (amount > 0)
                {
                    _inventory.RemoveItem(itemDrop.m_itemData.m_shared.m_name, amount);   
                }
                return true;
            }
            return false;
        }

        // public static bool TeleportPossible(Player player)
        // {
        //     var connectionZdoid = player.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
        //     if (connectionZdoid == ZDOID.None)
        //         return false;
        //     if (ZDOMan.instance.GetZDO(connectionZdoid) != null)
        //         return true;
        //     ZDOMan.instance.RequestZDO(connectionZdoid);
        //     return false;
        // }

        private void Awake()
        {
            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());
            
            // if (!Phylacteries.Contains(this))
            //     Phylacteries.Add(this);
            
            _container = gameObject.AddComponent<Container>();
            _container.m_name = "$chebgonaz_phylactery_name";

            _inventory = _container.GetInventory();
            _inventory.m_name = Localization.instance.Localize(_container.m_name);
            
            // register portal - see TeleportWorld.RPC_SetTag. We set the phylactery up as a one-way portal
            // var zdo = Player.m_localPlayer.m_nview.GetZDO();
            // var connectionZdoid = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            // zdo.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
            // ZDOMan.instance.GetZDO(connectionZdoid)?.UpdateConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
            // zdo.Set(ZDOVars.s_tag, ZDOVars.s_tag);
            // zdo.Set(ZDOVars.s_tagauthor, ZDOVars.s_tagauthor);
        }
    }
}