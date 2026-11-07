using LiteNetLib.Utils;

namespace REP2P
{
    /// <summary>
    /// Method for when you receive a message
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="messageId"></param>
    /// <param name="stream"></param>
    public delegate void P2PMessageHandler(P2PConnection sender, int messageId, NetDataReader stream);
}
