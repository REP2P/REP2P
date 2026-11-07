using Steamworks;

namespace REP2P.Facepunch.Steamworks
{
    public class P2PFacepunchConnection : P2PConnection
    {
        public override bool isHost => P2PFacepunchService.CurrentLobby.IsOwnedBy(friend.Id);

        internal bool _isConnected;
        public override bool isConnected => _isConnected;

        public Friend friend { get; protected set; }

        internal P2PFacepunchConnection(Friend friend)
        {
            this.friend = friend;
            P2PLogger.Log($"Created Connection for {friend.Id.Value}");
        }

        protected override void Send(byte[] data, P2PSendMode sendMode)
        {
            SteamNetworking.SendP2PPacket(friend.Id, data, sendType: sendMode == P2PSendMode.Reliable ? P2PSend.Reliable : P2PSend.Unreliable);
        }

        public override void SetData(string key, string value)
        {
            P2PFacepunchService.CurrentLobby.SetMemberData(key, value);
        }

        public override string GetData(string key) => P2PFacepunchService.CurrentLobby.GetMemberData(friend, key);
    }
}