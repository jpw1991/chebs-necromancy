using System.Collections;
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
        public static readonly int PhylacteryHash = "ChebGonaz_Phylactery".GetStableHashCode();

        public static ConfigEntry<string> FuelPrefab;

        // updated by client with info from server
        public static bool HasPhylactery;
        public static Vector3 PhylacteryLocation = Vector3.zero;

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
            ObjectName = MethodBase.GetCurrentMethod()?.DeclaringType?.Name
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

        private static bool PhylacteryZDOHasFuel(ZDO zdo)
        {
            var fuelPrefab = FuelPrefab.Value;
            var fuelFound = 0;
            
            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery fuelPrefab={fuelPrefab}");

            // read items from ZDO
            var byteArray = zdo.GetByteArray(ZDOVars.s_items);
            if (byteArray == null)
            {
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Failed to get phylactery's items: byte array is null");
                return false;
            }
            var pkg = new ZPackage(byteArray);
            
            // Copied from: Inventory.Load(ZPackage pkg)
            var obj = (Version.Item)pkg.ReadInt();
            if (obj >= Version.Item.Smaller)
            {
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery zdo's obj version={obj}");
                var num = (int) pkg.ReadUShort();
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery num={num}");
                for (var index = 0; index < num; ++index)
                {
                    (var prefabHash, var itemData) = ItemDrop.ItemData.Load(pkg, obj);
                    if (prefabHash != 0)
                    {
                        var prefab = ZNetScene.instance.GetPrefab(prefabHash);
                        if (prefab == null)
                        {
                            Logger.LogError($"Failed to get prefab from hash={prefabHash}");
                        }
                        var name = prefab.GetComponent<ItemDrop>().name;
                        if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Found {name} in phylactery's inventory");
                        if (name == fuelPrefab)
                        {
                            if (name == fuelPrefab) fuelFound++;
                        }
                    }
                    else if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery prefabHash=0");
                }
            }
            else if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery zdo's obj version={obj}");

            return fuelFound > 0;
        }

        public static void RemoveFuelFromPhylactery(ZDO zdo)
        {
            var fuelPrefab = FuelPrefab.Value;
            var fuelConsumed = false;

            // read items from ZDO
            //
            // Copied from: Container.Load()
            var byteArray = zdo.GetByteArray(ZDOVars.s_items);
            if (byteArray == null)
            {
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Failed to get phylactery's items: byte array is null");
                return;
            }
            var readPkg = new ZPackage(byteArray);

            // Copied from: Inventory.Load(ZPackage pkg)
            var obj = (Version.Item)readPkg.ReadInt();
            if (obj < Version.Item.Smaller)
            {
                Logger.LogError($"Phylactery items are stored in an unsupported old format (version={obj}), unable to remove fuel.");
                return;
            }

            var num = (int)readPkg.ReadUShort();
            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery num={num}");

            // To remove one item:
            // 1. read all items
            // 2. write back all items, except for one matching fuelPrefab
            var keptItems = new List<ItemDrop.ItemData>();
            for (var index = 0; index < num; ++index)
            {
                (var prefabHash, var itemData) = ItemDrop.ItemData.Load(readPkg, obj);
                if (prefabHash == 0)
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery prefabHash=0");
                    continue;
                }

                var prefab = ZNetScene.instance.GetPrefab(prefabHash);
                if (prefab == null)
                {
                    Logger.LogError($"Failed to get prefab from hash={prefabHash}");
                    continue;
                }

                var name = prefab.GetComponent<ItemDrop>().name;
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Found {name} in phylactery's inventory");
                if (!fuelConsumed && name == fuelPrefab)
                {
                    // omit
                    fuelConsumed = true;
                    continue;
                }

                // write: ItemData.Save() only serializes the prefab hash if m_dropPrefab is set
                itemData.m_dropPrefab = prefab;
                keptItems.Add(itemData);
            }

            if (!fuelConsumed)
            {
                Logger.LogWarning("RemoveFuelFromPhylactery: no fuel item was found in the phylactery's inventory to consume.");
            }

            // write all items back
            //
            // Copied from: Inventory.Save(ZPackage pkg) / Container.Save()
            var writePkg = new ZPackage();
            writePkg.Write((int)Version.Item.ChunksNCheats);
            writePkg.Write((ushort)keptItems.Count);
            foreach (var itemData in keptItems)
            {
                itemData.Save(writePkg);
            }

            zdo.Set(ZDOVars.s_items, writePkg.GetArray());
        }

        private static IEnumerator PhylacteryCheckRPCServerReceive(long sender, ZPackage package)
        {
            if (ZNet.instance == null) yield return null;
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                var payload = package.GetArray();
                var payloadDecoded = Encoding.UTF8.GetString(payload);
                if (payloadDecoded.StartsWith(PhylacteryCheckString1))
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Received request from {sender} to check for an existing phylactery ({payloadDecoded}).");

                    var split = payloadDecoded.Split(' ');
                    long playerCreatorID = 0;
                    if (split.Length < 2 || !long.TryParse(split[1], out playerCreatorID))
                    {
                        Logger.LogError($"Failed to parse playerCreatorID out of payload ({split.Length})");
                    }
                    
                    var phylacteryBelongingToPlayer = ZDOMan.instance.m_objectsByID
                        .Values
                        .ToList()
                        .FindAll(zdo => zdo.GetPrefab() == PhylacteryHash)
                        .Where(zdo => zdo.GetLong(ZDOVars.s_creator) == playerCreatorID)
                        .ToList()
                        .FirstOrDefault();
                    if (phylacteryBelongingToPlayer != null)
                    {
                        var phylacteryHasFuel = PhylacteryZDOHasFuel(phylacteryBelongingToPlayer);
                        var location = phylacteryHasFuel
                            // check string 2 + location = phylactery exists, rescue player
                            ? Encoding.UTF8.GetBytes(PhylacteryCheckString2 + phylacteryBelongingToPlayer.m_position)
                            // check string 2 without location = no phylactyer, let them die
                            : Encoding.UTF8.GetBytes(PhylacteryCheckString2);
                        PhylacteryCheckRPC.SendPackage(sender, new ZPackage(location));
                        
                        if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Phylactery found belonging to player: {phylacteryBelongingToPlayer.m_position}, has fuel={phylacteryHasFuel}");
                    }
                    else
                    {
                        if (BasePlugin.HeavyLogging.Value) Logger.LogInfo("no phylactery found for player.");
                    }
                }
                else if (payloadDecoded.StartsWith(PhylacteryConsumeFuelString1))
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Received request from {sender} to consume fuel.");

                    var split = payloadDecoded.Split(' ');
                    long playerCreatorID = 0;
                    if (split.Length < 2 || !long.TryParse(split[1], out playerCreatorID))
                    {
                        Logger.LogError($"Failed to parse playerCreatorID out of payload ({split.Length})");
                    }
                    
                    var phylacteryBelongingToPlayer = ZDOMan.instance.m_objectsByID
                        .Values
                        .ToList()
                        .FindAll(zdo => zdo.GetPrefab() == PhylacteryHash)
                        .Where(zdo => zdo.GetLong(ZDOVars.s_creator) == playerCreatorID)
                        .ToList()
                        .FirstOrDefault();
                    if (phylacteryBelongingToPlayer != null)
                    {
                        RemoveFuelFromPhylactery(phylacteryBelongingToPlayer);
                    }
                    
                }
                else if (payloadDecoded.StartsWith(PhylacteryCheckString2))
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Received request from {sender} for phylactery location.");
                    ReceivePhylacteryLocation(payloadDecoded, sender);
                }
            }
            else
            {
                if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Received request from {sender} for phylactery location, but {sender} is not the host.");
            }

            yield return null;
        }

        private static void ReceivePhylacteryLocation(string decoded, long sender)
        {
            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"ReceivePhylacteryLocation {decoded} {sender}");

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

            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo(
                $"{phylacteryPositionXYZStr[0]} {phylacteryPositionXYZStr[1]} {phylacteryPositionXYZStr[2]}");
            var phylacteryVector3 = new Vector3(
                float.Parse(phylacteryPositionXYZStr[0]),
                float.Parse(phylacteryPositionXYZStr[1]),
                float.Parse(phylacteryPositionXYZStr[2])
            );
            
            HasPhylactery = true;
            PhylacteryLocation = phylacteryVector3;
        }

        public static IEnumerator PhylacteryCheckRPCClientReceive(long sender, ZPackage package)
        {
            if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"PhylacteryCheckRPCClientReceive");
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
                    var package =
                        new ZPackage(Encoding.UTF8.GetBytes(PhylacteryCheckString1 + " " +
                                                            Player.m_localPlayer.GetPlayerID()));
                    PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
                }

                yield return new WaitForSeconds(5);
            }
        }

        public static void RequestConsumptionOfFuelForPlayerPhylactery()
        {
            var package = new ZPackage(Encoding.UTF8.GetBytes(PhylacteryConsumeFuelString1 + " " +
                                                              Player.m_localPlayer.GetPlayerID()));
            PhylacteryCheckRPC.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
        }

        private void Awake()
        {
            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            _container = gameObject.AddComponent<Container>();
            _container.m_name = "$chebgonaz_phylactery_name";

            _inventory = _container.GetInventory();
            _inventory.m_name = Localization.instance.Localize(_container.m_name);
        }
    }
}