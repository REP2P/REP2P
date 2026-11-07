using System;

namespace REP2P
{
    /// <summary>
    /// P2P Transport template
    /// </summary>
    public interface P2PService
    {
        /// <summary>
        /// invokes when a room starts
        /// </summary>
        public event Action createdRoom;
        /// <summary>
        /// invokes when connected
        /// </summary>
        public event Action<P2PConnection> joinedRoom;
        /// <summary>
        /// invokes when disconnected
        /// </summary>
        public event Action disconnected;
        /// <summary>
        /// invokes when connection fails
        /// </summary>
        public event Action connectionFailed;  
        /// <summary>
        /// invokes when remote client connected
        /// </summary>
        public event Action<P2PConnection> clientConnected;
        /// <summary>
        /// invokes when remote client disconnected
        /// </summary>
        public event Action<P2PConnection> clientDisconnected;
        /// <summary>
        /// invokes when data received
        /// </summary>
        public event P2PDataReceivedDelegate dataReceived;

        /// <summary>
        /// are you host?
        /// </summary>
        public bool isHost { get; }
        /// <summary>
        /// are you connected
        /// </summary>
        public bool isConnected { get; }
        /// <summary>
        /// host's connection
        /// </summary>
        public P2PConnection host { get; }
        /// <summary>
        /// starts the game
        /// </summary>
        /// <param name="maxConnections">max connections</param>
        /// <param name="visibility">how visible</param>
        public void CreateRoom(int maxConnections, P2PVisibility visibility = P2PVisibility.Public);
        /// <summary>
        /// Joins a room and returns an error if occured
        /// </summary>
        /// <param name="address">end address</param>
        public P2PConnectionError JoinRoom(string address);

        /// <summary>
        /// Updates the network transport state
        /// </summary>
        public void Poll();
        /// <summary>
        /// Disconnects you from the room
        /// </summary>
        public void Disconnect();

        /// <summary>
        /// Sets a data for the room
        /// </summary>
        /// <param name="key">the key of the data</param>
        /// <param name="value">the value of the data</param>
        public void SetRoomData(string key, string value);
        /// <summary>
        /// Returns the data from the room
        /// </summary>
        /// <param name="key">the key of the data</param>
        public string GetRoomData(string key);
    }
}
