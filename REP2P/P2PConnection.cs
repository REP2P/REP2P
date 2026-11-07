using LiteNetLib.Utils;
using System;

namespace REP2P
{
    /// <summary>
    /// Remote P2P Connection that receives messages
    /// </summary>
    public abstract class P2PConnection
    {
        /// <summary>
        /// Their Id
        /// </summary>
        public Guid guid { get; internal set; }
        /// <summary>
        /// Are they the host?
        /// </summary>
        public abstract bool isHost { get; }
        /// <summary>
        /// Are they still with us?
        /// </summary>
        public abstract bool isConnected { get; }

        /// <summary>
        /// Sends a message to that connection
        /// </summary>
        /// <param name="messageId">message id</param>
        /// <param name="stream">the stream being sent to connection</param>
        /// <param name="sendMode">is it reliable or nah</param>
        /// <param name="shouldRelease">disposes the message after use</param>
        public void Send(int messageId, NetDataWriter stream, P2PSendMode sendMode, bool shouldRelease = true)
        {
            NetDataWriter message = new NetDataWriter();
            message.Put(messageId);
            message.Put(stream.Data);
            if (P2PNetwork.debug) P2PLogger.Log($"Sending Message {messageId} to Client {guid}");
            Send(message.Data, sendMode);
        }
        /// <summary>
        /// low level message to that connection
        /// </summary>
        /// <param name="data">message into a byte array</param>
        /// <param name="sendMode">is the packet reliable</param>
        protected abstract void Send(byte[] data, P2PSendMode sendMode);

        public abstract void SetData(string key, string value);
        public abstract string GetData(string key);
    }
}
