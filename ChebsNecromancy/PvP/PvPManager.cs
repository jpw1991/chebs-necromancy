using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jotunn;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.PvP
{
    public class PvPManager
    {
        // The server stores a dictionary of players and their list of friends and minions check this to figure out
        // who to be hostile to.

        private static CustomRPC _pvPrpc;
        private const string GetDictString = "CG_PvP_1";
        private const string UpdateDictString = "CG_PvP_2";

        private static string AllyFileName => $"{ZNet.instance.GetWorldName()}.{BasePlugin.PluginName}.PvP.json";

        private static Tuple<string, Dictionary<string, List<string>>> _playerFriends;

        private static Dictionary<string, List<string>> PlayerFriends
        {
            // getter logic reloads the file if the world has changed. That way if a player switches worlds the file
            // will be read again. It also ensures no mismatch can be made
            get
            {
                if (_playerFriends == null)
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogInfo("_playerFriends is null, reading from file.");
                    _playerFriends =
                        new Tuple<string, Dictionary<string, List<string>>>(ZNet.instance.GetWorldName(),
                            ReadAllyFile());
                }

                return _playerFriends.Item2;
            }
            set => _playerFriends =
                new Tuple<string, Dictionary<string, List<string>>>(ZNet.instance.GetWorldName(), value);
        }

        public static bool Friendly(string minionMasterA, string minionMasterB)
        {
            return PlayerFriends.TryGetValue(minionMasterA, out List<string> friends)
                   && friends.Contains(minionMasterB);
        }

        public static void ConfigureRPC()
        {
            _pvPrpc = NetworkManager.Instance.AddRPC("PvPrpc",
                PvP_RPCServerReceive, PvP_RPCClientReceive);
        }

        private static void UpdateAllyFile(string content)
        {
            // only used by server, clients just use their in-memory dictionary.
            var filePath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), AllyFileName);

            if (!File.Exists(filePath))
            {
                try
                {
                    using var fs = File.Create(filePath);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error creating {filePath}: {ex.Message}");
                }
            }

            try
            {
                using var writer = new StreamWriter(filePath, false);
                writer.Write(content);
                writer.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error writing to {filePath}: {ex.Message}");
            }
        }

        private static Dictionary<string, List<string>> ReadAllyFile()
        {
            // only used by server, clients just use their in-memory dictionary.
            var filePath = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), AllyFileName);

            if (!File.Exists(filePath))
            {
                try
                {
                    using var fs = File.Create(filePath);
                    fs.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error creating {filePath}: {ex.Message}");
                }
            }

            string content = null;
            try
            {
                using var reader = new StreamReader(filePath);
                content = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error writing to {filePath}: {ex.Message}");
            }

            if (content == null)
            {
                Logger.LogError($"Error reading {filePath}: content is null!");
                return new Dictionary<string, List<string>>();
            }

            if (BasePlugin.HeavyLogging.Value) Logger.LogInfo($"Read from {filePath}: {content}");

            return content == ""
                ? new Dictionary<string, List<string>>()
                : SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, List<string>>>(content);
        }

        public static void UpdatePlayerFriendsDict(string list)
        {
            var content = $"{UpdateDictString};{Player.m_localPlayer.GetPlayerName()};{list}";
            if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"UpdatePlayerFriendsDict {content}");
            var package = new ZPackage(Encoding.UTF8.GetBytes(content));
            _pvPrpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
        }

        private static IEnumerator PvP_RPCServerReceive(long sender, ZPackage package)
        {
            if (ZNet.instance == null) yield return null;
            if (ZNet.instance.IsServerInstance() || ZNet.instance.IsLocalInstance())
            {
                var payload = package.GetArray();
                var payloadDecoded = Encoding.UTF8.GetString(payload);
                if (payloadDecoded.StartsWith(GetDictString))
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"PvP_RPCServerReceive {GetDictString}");
                    var serializedDict = SimpleJson.SimpleJson.SerializeObject(PlayerFriends.ToDictionary(
                        kvp => kvp.Key, kvp => (object)kvp.Value));
                    _pvPrpc.SendPackage(sender,
                        new ZPackage(Encoding.UTF8.GetBytes(GetDictString + ";" + serializedDict)));
                }
                else if (payloadDecoded.StartsWith(UpdateDictString))
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"PvP_RPCServerReceive {UpdateDictString}");
                    var split = payloadDecoded.Split(';');
                    if (split.Length != 3)
                    {
                        Logger.LogError($"Failed to parse payload ({split.Length})");
                    }

                    var senderNameString = split[1];
                    var friendsString = split[2];

                    var friendsList = friendsString.Split(',');
                    PlayerFriends[senderNameString] = friendsList.ToList();

                    // update all connected peers with the new dictionary
                    var serializedDict = SimpleJson.SimpleJson.SerializeObject(PlayerFriends.ToDictionary(
                        kvp => kvp.Key, kvp => (object)kvp.Value));
                    var returnPayload = GetDictString + ";" + serializedDict;
                    if (BasePlugin.HeavyLogging.Value)
                        Logger.LogMessage(
                            $"PvP_RPCServerReceive {UpdateDictString} sending to all peers: {returnPayload}");
                    _pvPrpc.SendPackage(ZNet.instance.m_peers, new ZPackage(Encoding.UTF8.GetBytes(returnPayload)));

                    UpdateAllyFile(serializedDict);
                }
            }

            yield return null;
        }

        public static IEnumerator PvP_RPCClientReceive(long sender, ZPackage package)
        {
            var payload = package.GetArray();
            if (payload.Length > 0)
            {
                var decoded = Encoding.UTF8.GetString(payload);
                if (decoded.StartsWith(GetDictString))
                {
                    if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"PvP_RPCClientReceive decoded: {decoded}");
                    var split = decoded.Split(';');
                    if (split.Length != 2)
                    {
                        Logger.LogError($"Failed to parse payload ({split.Length})");
                    }

                    var data = split[1];
                    var serialized = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, List<string>>>(data);
                    PlayerFriends = serialized;
                }
            }
            else if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"PvP_RPCClientReceive received no data");

            yield return null;
        }

        public static IEnumerator UpdatePlayerFriendsDictWhenPossible(string list)
        {
            yield return new WaitUntil(() => Player.m_localPlayer != null);
            UpdatePlayerFriendsDict(list);
        }
    }
}