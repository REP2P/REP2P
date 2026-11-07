using Steamworks;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace REP2P.SteamworksNET
{
    // The source code of https://github.com/RugbugRedfern/Mycelium-Networking-For-Content-Warning/ is used partially under the Attribution-NonCommercial-ShareAlike 4.0 International license
    public class P2PSteamworksService : P2PService
    {
        public const int CHANNEL = 420;
        public bool isHost => isConnected && SteamMatchmaking.GetLobbyOwner(CurrentLobby) == SteamUser.GetSteamID();
        public static CSteamID CurrentLobby;
        private bool _isConnected;
        public bool isConnected => _isConnected;

        public P2PConnection host => connections[SteamMatchmaking.GetLobbyOwner(CurrentLobby)];
        public readonly Dictionary<CSteamID, P2PSteamworksConnection> connections = new Dictionary<CSteamID, P2PSteamworksConnection>();
        public event Action createdRoom;
        public event Action<P2PConnection> joinedRoom;
        public event Action disconnected;
        public event Action connectionFailed;
        public event Action<P2PConnection> clientConnected;
        public event Action<P2PConnection> clientDisconnected;
        public event P2PDataReceivedDelegate dataReceived;

        protected Callback<LobbyEnter_t> lobbyEntered;
        protected Callback<LobbyCreated_t> lobbyCreated;
        protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;
        protected Callback<LobbyDataUpdate_t> lobbyDataUpdate;

        public P2PSteamworksService()
        {
            _isConnected = false;
            lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t param)
        {
            var steamID = new CSteamID(param.m_ulSteamIDUserChanged);
            if (SteamUser.GetSteamID() != steamID) return;
            switch ((EChatMemberStateChange)param.m_rgfChatMemberStateChange)
            {
                case EChatMemberStateChange.k_EChatMemberStateChangeEntered:
                    var connection = CreateConnection(steamID);
                    connection._isConnected = true;
                    clientConnected?.Invoke(connection);
                    break;
                case EChatMemberStateChange.k_EChatMemberStateChangeLeft:
                case EChatMemberStateChange.k_EChatMemberStateChangeDisconnected:
                case EChatMemberStateChange.k_EChatMemberStateChangeKicked:
                case EChatMemberStateChange.k_EChatMemberStateChangeBanned:
                    connections[steamID]._isConnected = false;
                    clientDisconnected?.Invoke(connections[steamID]);
                    connections.Remove(steamID);
                    break;
            }
        }

        private void OnLobbyEnter(LobbyEnter_t param)
        {
            connections.Clear();
            _isConnected = true;
            CurrentLobby = (CSteamID)param.m_ulSteamIDLobby;
            int length = SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
            for (int i = 0; i < length; i++)
            {
                CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i);
                if (memberId != SteamUser.GetSteamID())
                {
                    clientConnected?.Invoke(CreateConnection(memberId));
                }
            }
            joinedRoom?.Invoke(CreateConnection(SteamUser.GetSteamID()));
        }

        private void OnLobbyCreated(LobbyCreated_t param)
        {
            if (param.m_eResult == EResult.k_EResultOK)
            {
                CurrentLobby = (CSteamID)param.m_ulSteamIDLobby;
                createdRoom?.Invoke();
            }
            else
            {
                connectionFailed?.Invoke();
            }  
        }

        public P2PConnectionError JoinRoom(string address)
        {
            P2PLogger.Log("Connecting!");
            try
            {
                var result = SteamMatchmaking.JoinLobby((CSteamID)ulong.Parse(address));
                return P2PConnectionError.None;
            }
            catch (Exception ex)
            {
                P2PLogger.Exception(ex);
                return P2PConnectionError.Unknown;
            }
        }

        public void CreateRoom(int maxConnections, P2PVisibility visibility = P2PVisibility.Public)
        {
            P2PLogger.Log("Starting Game!");
            ELobbyType lobbyType = ELobbyType.k_ELobbyTypePublic;
            switch (visibility)
            {
                case P2PVisibility.Public:
                    lobbyType = ELobbyType.k_ELobbyTypePublic;
                    break;
                case P2PVisibility.Private:
                    lobbyType = ELobbyType.k_ELobbyTypePrivate;
                    break;
                case P2PVisibility.FriendsOnly:
                    lobbyType = ELobbyType.k_ELobbyTypeFriendsOnly;
                    break;
                case P2PVisibility.Invisible:
                    lobbyType = ELobbyType.k_ELobbyTypeInvisible;
                    break;
            }
            SteamMatchmaking.CreateLobby(lobbyType, maxConnections);
        }

        public void Poll()
        {
            SteamAPI.RunCallbacks();
            ReceiveMessages();
        }
        const int MAX_MESSAGES = 500;
        static IntPtr[] outMessages = new IntPtr[MAX_MESSAGES];
        public void ReceiveMessages()
        {
            int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(CHANNEL, outMessages, MAX_MESSAGES);
            if (P2PNetwork.debug && count > 0) P2PLogger.Log($"Inbound {count} Messages from SteamNetworkingMessages");
            for (int i = 0; i < count; i++)
            {
                IntPtr outMessage = outMessages[i];

                int messageSize = Marshal.SizeOf(outMessage);
                if (messageSize > Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend)
                {
                    P2PLogger.Error($"Ignored message because its size was above the max ({messageSize}/{Constants.k_cbMaxSteamNetworkingSocketsMessageSizeSend})");
                    SteamNetworkingMessage_t.Release(outMessages[i]);
                    continue;
                }

                SteamNetworkingMessage_t steamNetworkingMessage = Marshal.PtrToStructure<SteamNetworkingMessage_t>(outMessage);

                CSteamID sender = steamNetworkingMessage.m_identityPeer.GetSteamID();

                // Ignore them if their steam id is null (happens sometimes idk)
                if (sender == null)
                {
                    SteamNetworkingMessage_t.Release(outMessages[i]);
                    continue;
                }

                // Ignore them if they're not in the lobby
                if (connections[sender] == null)
                {
                    SteamNetworkingMessage_t.Release(outMessages[i]);
                    continue;
                }

                byte[] managedArray = new byte[steamNetworkingMessage.m_cbSize];
                Marshal.Copy(steamNetworkingMessage.m_pData, managedArray, 0, steamNetworkingMessage.m_cbSize);
                try
                {
                    dataReceived(connections[sender], managedArray);
                }
                catch (Exception ex)
                {
                    P2PLogger.Error($"Error receiving data from {sender}: {ex.Message} {ex.StackTrace}");
                }

                SteamNetworkingMessage_t.Release(outMessages[i]);
            }
        }

        public P2PSteamworksConnection CreateConnection(CSteamID steamID)
        {
            P2PLogger.Log($"Creating Connection for {steamID.m_SteamID}");
            var result = new P2PSteamworksConnection(steamID);
            connections.Add(steamID, result);
            return result;
        }
        public void Disconnect()
        {
            P2PLogger.Log("Disconnecting!");
            SteamMatchmaking.LeaveLobby(CurrentLobby);
            _isConnected = false;
            CurrentLobby = CSteamID.Nil;
            disconnected?.Invoke();
        }

        public void SetRoomData(string key, string value)
        {
            SteamMatchmaking.SetLobbyData(CurrentLobby, key, value);
        }

        public string GetRoomData(string key)
        {
            return SteamMatchmaking.GetLobbyData(CurrentLobby, key);
        }
    }
}
