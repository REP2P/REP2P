using LiteNetLib.Utils;
using System;
using System.Collections.Generic;

namespace REP2P
{
    /// <summary>
    /// Class that handles the Connections/Data transfer between peers in a p2p mesh
    /// </summary>
    public static class P2PNetwork
    {
        /// <summary>
        /// The Network Transport
        /// </summary>
        public static P2PService instance { get; private set; }

        /// <summary>
        /// All the Connections in the room
        /// </summary>
        public static readonly Dictionary<Guid, P2PConnection> connections = new Dictionary<Guid, P2PConnection>();
        /// <summary>
        /// Your local connection
        /// </summary>
        public static P2PConnection localConnection { get; private set; } = null;
        /// <summary>
        /// Allow Debug Logs
        /// </summary>
        public static bool debug { get; set; }
        /// <summary>
        /// Initialize the P2P Network
        /// </summary>
        /// <param name="transport">Your selected transport</param>
        public static void Initialize(P2PService transport)
        {
            instance = transport;
            instance.clientConnected += Instance_ClientConnected;
            instance.clientDisconnected += Instance_ClientDisconnected;
            instance.createdRoom += Instance_Created;
            instance.joinedRoom += Instance_Joined;
            instance.disconnected += Instance_Disconnected;
            instance.dataReceived += Instance_DataReceived;
        }

        /// <summary>
        /// Are you host?
        /// </summary>
        /// <returns>true if host</returns>
        public static bool isHost => instance.isHost;

        /// <summary>
        /// Are you host?
        /// </summary>
        /// <returns>true if host</returns>
        public static bool isConnected => instance.isConnected;

        /// <summary>
        /// Message Handlers
        /// </summary>
        public readonly static Dictionary<int, P2PMessageHandler> messageHandlers = new Dictionary<int, P2PMessageHandler>();

        private static void Instance_Created()
        {
            P2PLogger.Log("Created Session!");
        }
        private static void Instance_Joined(P2PConnection connection)
        {
            connection.guid = Guid.NewGuid();
            P2PLogger.Log("Joined Session!");
        }
        private static void Instance_Disconnected()
        {
            localConnection = null;
            P2PLogger.Log("Disconnected!");
        }

        private static void Instance_ClientConnected(P2PConnection connection)
        {
            connection.guid = GetNextGUID();
            connections.Add(connection.guid, connection);
            P2PLogger.Log($"Client {connection.guid} Connected!");
        }

        private static Guid GetNextGUID()
        {
            Guid guid = Guid.NewGuid();
            while(connections.ContainsKey(guid))
            {
                guid = Guid.NewGuid();
            }
            return guid;
        }

        private static void Instance_ClientDisconnected(P2PConnection connection)
        {
            connections.Remove(connection.guid);
            P2PLogger.Log($"Client {connection.guid} Disconnected!");
        }

        private static void Instance_DataReceived(P2PConnection sender, byte[] data)
        {
            NetDataReader reader = new NetDataReader(data);
            int messageId = reader.GetInt();
            messageHandlers[messageId]?.Invoke(sender, messageId, reader);
            if (debug) P2PLogger.Log($"Received Message {messageId} from Client {sender.guid}");
            reader.Clear();
        }
        /// <summary>
        /// Creates Room with the given max connections and visibility
        /// </summary>
        /// <param name="maxConnections">Max Connections to the room</param>
        /// <param name="visibility">the privacy of that room</param>
        public static void CreateRoom(int maxConnections = 10, P2PVisibility visibility = P2PVisibility.Public) => instance.CreateRoom(maxConnections, visibility);
        /// <summary>
        /// Connects to a room
        /// </summary>
        /// <param name="address">room address</param>
        public static P2PConnectionError JoinRoom(string address) {
            var error = instance.JoinRoom(address);
            if(error != P2PConnectionError.None) P2PLogger.Error($"Connection error {error}");
            return error;
        }
        /// <summary>
        /// Send a message to every one in the room
        /// </summary>
        /// <param name="messageId">your message id</param>
        /// <param name="stream">your data stream</param>
        /// <param name="sendMode">how reliable</param>
        /// <param name="shouldRelease">dispose after use</param>
        public static void SendToAll(int messageId, NetDataWriter stream, P2PSendMode sendMode, bool shouldRelease = true)
        {
            if (debug) P2PLogger.Log($"Sending Message {messageId} to all clients");
            foreach (var connection in connections.Values)
            {
                connection.Send(messageId, stream, sendMode, shouldRelease);
            }
            if (shouldRelease) stream.Reset();
        }
        /// <summary>
        /// Send a message to a client
        /// </summary>
        ///         /// <param name="target">your target</param>
        /// <param name="messageId">your message id</param>
        /// <param name="stream">your data stream</param>
        /// <param name="sendMode">how reliable</param>
        /// <param name="shouldRelease">dispose after use</param>
        public static void Send(Guid target, int messageId, NetDataWriter stream, P2PSendMode sendMode, bool shouldRelease = true) => connections[target].Send(messageId, stream, sendMode, shouldRelease);
        /// <summary>
        /// Send a message to a client
        /// </summary>
        /// <param name="target">your target</param>
        /// <param name="messageId">your message id</param>
        /// <param name="stream">your data stream</param>
        /// <param name="sendMode">how reliable</param>
        /// <param name="shouldRelease">dispose after use</param>
        public static void Send(P2PConnection target, int messageId, NetDataWriter stream, P2PSendMode sendMode, bool shouldRelease = true) => target.Send(messageId, stream, sendMode, shouldRelease);

        /// <summary>
        /// Sets the data for the current room
        /// </summary>
        /// <param name="key">the key of the data</param>
        /// <param name="value">the value of the data</param>
        public static void SetRoomData(string key, string value) => instance.SetRoomData(key, value);

        /// <summary>
        /// Retrieves current room data
        /// </summary>
        /// <param name="key">the key of the data</param>
        public static string GetRoomData(string key) => instance.GetRoomData(key);

        /// <summary>
        /// Updates the Network, Put this in an update function or while loop
        /// </summary>
        public static void Poll() => instance.Poll();

        /// <summary>
        /// Disconnects from current room
        /// </summary>
        public static void Disconnect() => instance.Disconnect();
    }
}
