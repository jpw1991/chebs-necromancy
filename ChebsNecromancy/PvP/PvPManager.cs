using System.Collections;
using System.Collections.Generic;
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
        
        private static Dictionary<string, List<string>> _playerFriends = new();

        public static bool Friendly(string minionMasterA, string minionMasterB)
        {
            return _playerFriends.TryGetValue(minionMasterA, out List<string> friends)
                   && friends.Contains(minionMasterB);
        }

        public static void ConfigureRPC()
        {
            _pvPrpc = NetworkManager.Instance.AddRPC("PvPrpc",
                PvP_RPCServerReceive, PvP_RPCClientReceive);
        }
        
        public static void RequestPlayerFriendsDict()
        {
            if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"RequestPlayerFriendsDict");
            var package = new ZPackage(Encoding.UTF8.GetBytes(GetDictString));
            _pvPrpc.SendPackage(ZRoutedRpc.instance.GetServerPeerID(), package);
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
                    var serializedDict = SimpleJson.SimpleJson.SerializeObject(_playerFriends);
                    _pvPrpc.SendPackage(sender, new ZPackage(Encoding.UTF8.GetBytes(GetDictString+";"+serializedDict)));
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
                    _playerFriends[senderNameString] = friendsList.ToList();
                    
                    // update all connected peers with the new dictionary
                    var serializedDict = SimpleJson.SimpleJson.SerializeObject(_playerFriends);
                    var returnPayload = GetDictString + ";" + serializedDict;
                    if (BasePlugin.HeavyLogging.Value) Logger.LogMessage($"PvP_RPCServerReceive {UpdateDictString} sending to all peers: {returnPayload}");
                    _pvPrpc.SendPackage(ZNet.instance.m_peers, new ZPackage(Encoding.UTF8.GetBytes(returnPayload)));
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
                    _playerFriends = serialized;                    
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