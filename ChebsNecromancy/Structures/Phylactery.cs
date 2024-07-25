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

            // read items from ZDO
            //
            // Copied from: Container.Load()
            var zdoItemsBase64String = zdo.GetString(ZDOVars.s_items);
            var zPackage = new ZPackage(zdoItemsBase64String);
            // Copied from: Inventory.Load(ZPackage pkg)
            var num1 = zPackage.ReadInt();
            var num2 = zPackage.ReadInt();
            if (num1 == 106)
            {
                for (var index1 = 0; index1 < num2; ++index1)
                {
                    var name = zPackage.ReadString();
                    var stack = zPackage.ReadInt();
                    var durability = zPackage.ReadSingle();
                    var pos = zPackage.ReadVector2i();
                    var equipped = zPackage.ReadBool();
                    var quality = zPackage.ReadInt();
                    var variant = zPackage.ReadInt();
                    var crafterID = zPackage.ReadLong();
                    var crafterName = zPackage.ReadString();
                    var customData = new Dictionary<string, string>();
                    var num3 = zPackage.ReadInt();
                    for (var index2 = 0; index2 < num3; ++index2)
                        customData[zPackage.ReadString()] = zPackage.ReadString();
                    var worldLevel = zPackage.ReadInt();
                    var pickedUp = zPackage.ReadBool();
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Found {name} in phylactery's inventory");
                    if (name == fuelPrefab) fuelFound++;
                }
            }
            else
            {
                for (var index3 = 0; index3 < num2; ++index3)
                {
                    var name = zPackage.ReadString();
                    var stack = zPackage.ReadInt();
                    var durability = zPackage.ReadSingle();
                    var pos = zPackage.ReadVector2i();
                    var equipped = zPackage.ReadBool();
                    var quality = 1;
                    if (num1 >= 101)
                        quality = zPackage.ReadInt();
                    var variant = 0;
                    if (num1 >= 102)
                        variant = zPackage.ReadInt();
                    long crafterID = 0;
                    var crafterName = "";
                    if (num1 >= 103)
                    {
                        crafterID = zPackage.ReadLong();
                        crafterName = zPackage.ReadString();
                    }

                    var customData = new Dictionary<string, string>();
                    if (num1 >= 104)
                    {
                        var num4 = zPackage.ReadInt();
                        for (var index4 = 0; index4 < num4; ++index4)
                        {
                            var key = zPackage.ReadString();
                            var str = zPackage.ReadString();
                            customData[key] = str;
                        }
                    }

                    var worldLevel = 0;
                    if (num1 >= 105)
                        worldLevel = zPackage.ReadInt();
                    var pickedUp = false;
                    if (num1 >= 106)
                        pickedUp = zPackage.ReadBool();
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Found {name} in phylactery's inventory");
                    if (name == fuelPrefab) fuelFound++;
                }
            }

            return fuelFound > 0;
        }

        private struct ItemInfoHolder
        {
            public string Name;
            public int Stack;
            public float Durability;
            public Vector2i GridPosition;
            public bool Equipped;
            public int Quality;
            public int Variant;
            public long CrafterID;
            public string CrafterName;
            public Dictionary<string, string> CustomData;
            public int WorldLevel;
            public bool PickedUp;

            public ItemInfoHolder(string name, int stack, float durability, Vector2i gridPosition, bool equipped,
                int quality,
                int variant, long crafterID, string crafterName, Dictionary<string, string> customData, int worldLevel,
                bool pickedUp)
            {
                Name = name;
                Stack = stack;
                Durability = durability;
                GridPosition = gridPosition;
                Equipped = equipped;
                Quality = quality;
                Variant = variant;
                CrafterID = crafterID;
                CrafterName = crafterName;
                CustomData = customData;
                WorldLevel = worldLevel;
                PickedUp = pickedUp;
            }
        }

        public static void RemoveFuelFromPhylactery(ZDO zdo)
        {
            var fuelPrefab = FuelPrefab.Value;

            // To remove one item:
            // 1. read all items
            // 2. write all items, except for one
            var items = new List<ItemInfoHolder>();
            var fuelConsumed = false;

            // read items from ZDO
            //
            // Copied from: Container.Load()
            var zdoItemsBase64String = zdo.GetString(ZDOVars.s_items);
            var zPackage = new ZPackage(zdoItemsBase64String);
            // Copied from: Inventory.Load(ZPackage pkg)
            var num1 = zPackage.ReadInt();
            var num2 = zPackage.ReadInt();
            if (num1 == 106)
            {
                for (var index1 = 0; index1 < num2; ++index1)
                {
                    var name = zPackage.ReadString();
                    var stack = zPackage.ReadInt();
                    var durability = zPackage.ReadSingle();
                    var pos = zPackage.ReadVector2i();
                    var equipped = zPackage.ReadBool();
                    var quality = zPackage.ReadInt();
                    var variant = zPackage.ReadInt();
                    var crafterID = zPackage.ReadLong();
                    var crafterName = zPackage.ReadString();
                    var customData = new Dictionary<string, string>();
                    var num3 = zPackage.ReadInt();
                    for (var index2 = 0; index2 < num3; ++index2)
                        customData[zPackage.ReadString()] = zPackage.ReadString();
                    var worldLevel = zPackage.ReadInt();
                    var pickedUp = zPackage.ReadBool();
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Found {name} in phylactery's inventory");
                    if (name == fuelPrefab && !fuelConsumed)
                    {
                        // omit
                        fuelConsumed = true;
                    }
                    else
                    {
                        // write
                        items.Add(new ItemInfoHolder(name, stack, durability, pos, equipped, quality, variant,
                            crafterID, crafterName, customData, worldLevel, pickedUp));
                    }
                }
            }
            else
            {
                for (var index3 = 0; index3 < num2; ++index3)
                {
                    var name = zPackage.ReadString();
                    var stack = zPackage.ReadInt();
                    var durability = zPackage.ReadSingle();
                    var pos = zPackage.ReadVector2i();
                    var equipped = zPackage.ReadBool();
                    var quality = 1;
                    if (num1 >= 101)
                        quality = zPackage.ReadInt();
                    var variant = 0;
                    if (num1 >= 102)
                        variant = zPackage.ReadInt();
                    long crafterID = 0;
                    var crafterName = "";
                    if (num1 >= 103)
                    {
                        crafterID = zPackage.ReadLong();
                        crafterName = zPackage.ReadString();
                    }

                    var customData = new Dictionary<string, string>();
                    if (num1 >= 104)
                    {
                        var num4 = zPackage.ReadInt();
                        for (var index4 = 0; index4 < num4; ++index4)
                        {
                            var key = zPackage.ReadString();
                            var str = zPackage.ReadString();
                            customData[key] = str;
                        }
                    }

                    var worldLevel = 0;
                    if (num1 >= 105)
                        worldLevel = zPackage.ReadInt();
                    var pickedUp = false;
                    if (num1 >= 106)
                        pickedUp = zPackage.ReadBool();
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Found {name} in phylactery's inventory");
                    if (name == fuelPrefab && !fuelConsumed)
                    {
                        // omit
                        fuelConsumed = true;
                    }
                    else
                    {
                        // write
                        items.Add(new ItemInfoHolder(name, stack, durability, pos, equipped, quality, variant,
                            crafterID, crafterName, customData, worldLevel, pickedUp));
                    }
                }
            }

            // write all items
            var zPackageWrite = new ZPackage();
            zPackageWrite.Write(106);
            zPackageWrite.Write(items.Count);
            foreach (var itemInfoHolder in items)
            {
                zPackageWrite.Write(itemInfoHolder.Name);
                zPackageWrite.Write(itemInfoHolder.Stack);
                zPackageWrite.Write(itemInfoHolder.Durability);
                zPackageWrite.Write(itemInfoHolder.GridPosition);
                zPackageWrite.Write(itemInfoHolder.Equipped);
                zPackageWrite.Write(itemInfoHolder.Quality);
                zPackageWrite.Write(itemInfoHolder.Variant);
                zPackageWrite.Write(itemInfoHolder.CrafterID);
                zPackageWrite.Write(itemInfoHolder.CrafterName);
                zPackageWrite.Write(itemInfoHolder.CustomData.Count);
                foreach (KeyValuePair<string, string> keyValuePair in itemInfoHolder.CustomData)
                {
                    zPackageWrite.Write(keyValuePair.Key);
                    zPackageWrite.Write(keyValuePair.Value);
                }

                zPackageWrite.Write(itemInfoHolder.WorldLevel);
                zPackageWrite.Write(itemInfoHolder.PickedUp);
            }

            var writeItemsBase64String = zPackageWrite.GetBase64();
            zdo.Set(ZDOVars.s_items, writeItemsBase64String);
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
                        if (BasePlugin.HeavyLogging.Value) Logger.LogInfo("Phylactery found belonging to player.");
                        var location = PhylacteryZDOHasFuel(phylacteryBelongingToPlayer)
                            ? Encoding.UTF8.GetBytes(PhylacteryCheckString2 + phylacteryBelongingToPlayer.m_position)
                            : Encoding.UTF8.GetBytes(PhylacteryCheckString2);
                        PhylacteryCheckRPC.SendPackage(sender, new ZPackage(location));
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