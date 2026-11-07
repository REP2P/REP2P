using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;

namespace REP2P.Facepunch.Steamworks
{
    public class P2PFacepunchService : P2PService
    {
        public const int CHANNEL = 420;
        public bool isHost => isConnected && CurrentLobby.IsOwnedBy(SteamClient.SteamId);
        public static Lobby CurrentLobby;
        private bool _isConnected;
        public bool isConnected => _isConnected;

        public P2PConnection host => connections[CurrentLobby.Owner];
        public readonly Dictionary<Friend, P2PFacepunchConnection> connections = new Dictionary<Friend, P2PFacepunchConnection>();
        public event Action createdRoom;
        public event Action<P2PConnection> joinedRoom;
        public event Action disconnected;
        public event Action connectionFailed;
        public event Action<P2PConnection> clientConnected;
        public event Action<P2PConnection> clientDisconnected;
        public event P2PDataReceivedDelegate dataReceived;
        private bool runCallbacks;
        public P2PFacepunchService(bool runCallbacks = false)
        {
            this.runCallbacks = runCallbacks;
            _isConnected = false;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEnter;
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberKicked += (lobby, friend, _) => OnLobbyMemberDisconnected(lobby, friend);
            SteamMatchmaking.OnLobbyMemberBanned += (lobby, friend, _) => OnLobbyMemberDisconnected(lobby, friend);

            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
            SteamNetworking.OnP2PSessionRequest += (id) => SteamNetworking.AcceptP2PSessionWithUser(id);
        }

        private void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            var steamID = friend.Id;
            if (friend.Id != SteamClient.SteamId) clientConnected?.Invoke(CreateConnection(friend));
        }

        private void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            var steamID = friend.Id;
            if (friend.Id != SteamClient.SteamId) {
                connections[friend]._isConnected = false;
                clientDisconnected?.Invoke(connections[friend]);
                connections.Remove(friend);
            }
        }

        private void OnLobbyEnter(Lobby lobby)
        {
            _isConnected = true;
            CurrentLobby = lobby;
            int length = lobby.MemberCount;
            foreach(var friend in lobby.Members)
            {
                if (friend.Id != SteamClient.SteamId)
                {
                    clientConnected?.Invoke(CreateConnection(friend));
                }
            }
            joinedRoom?.Invoke(CreateConnection(new Friend(SteamClient.SteamId)));
        }

        private void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result == Result.OK)
            {
                CurrentLobby = lobby;
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
                var result = SteamMatchmaking.JoinLobbyAsync(ulong.Parse(address));
                return P2PConnectionError.None;
            }
            catch (Exception ex)
            {
                P2PLogger.Exception(ex);
                return P2PConnectionError.Unknown;
            }
        }

        public async void CreateRoom(int maxConnections, P2PVisibility visibility = P2PVisibility.Public)
        {
            P2PLogger.Log("Starting Game!");
            var lobbyTask = SteamMatchmaking.CreateLobbyAsync(maxConnections);
            await lobbyTask;
            if (!lobbyTask.Result.HasValue) return;
            var lobby = lobbyTask.Result.Value;
            switch (visibility)
            {
                case P2PVisibility.Public:
                     lobby.SetPublic();
                    break;
                case P2PVisibility.Private:
                    lobby.SetPrivate();
                    break;
                case P2PVisibility.FriendsOnly:
                    lobby.SetFriendsOnly();
                    break;
                case P2PVisibility.Invisible:
                    lobby.SetInvisible();
                    break;
            }
        }

        public void Poll()
        {
            if(runCallbacks)SteamClient.RunCallbacks();
            ReceiveMessages();
        }
        public void ReceiveMessages()
        {
            while (SteamNetworking.IsP2PPacketAvailable())
            {
                var packet = SteamNetworking.ReadP2PPacket();
                if (packet.HasValue)
                {
                    var sender = packet.Value.SteamId;
                    var data = packet.Value.Data;
                    try
                    {
                        dataReceived(connections[new Friend(sender)], data);
                    }
                    catch (Exception ex)
                    {
                        P2PLogger.Error($"Error receiving data from {sender}: {ex.Message} {ex.StackTrace}");
                    }

                }
            }
        }

        public P2PFacepunchConnection CreateConnection(Friend friend)
        {
            P2PLogger.Log($"Creating Connection for {friend.Id.Value}");
            connections.Add(friend, new P2PFacepunchConnection(friend) { _isConnected = true });
            return connections[friend];
        }
        public void Disconnect()
        {
            P2PLogger.Log("Disconnecting!");
            connections.Clear();
            CurrentLobby.Leave();
            _isConnected = false;
            disconnected?.Invoke();
        }

        public void SetRoomData(string key, string value)
        {
            CurrentLobby.SetData(key, value);
        }

        public string GetRoomData(string key)
        {
            return CurrentLobby.GetData(key);
        }
    }
}
