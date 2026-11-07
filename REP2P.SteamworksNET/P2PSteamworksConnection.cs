using Steamworks;
using System;
using System.Runtime.InteropServices;

namespace REP2P.SteamworksNET
{
    public class P2PSteamworksConnection : P2PConnection
    {
        public override bool isHost => SteamMatchmaking.GetLobbyOwner(P2PSteamworksService.CurrentLobby) == steamId;

        internal bool _isConnected;
        public override bool isConnected => _isConnected;

        public CSteamID steamId { get; protected set; }

        internal P2PSteamworksConnection(CSteamID id) {
            steamId = id; 
            P2PLogger.Log($"Created Connection for {steamId.m_SteamID}");
        }
        protected override void Send(byte[] data, P2PSendMode sendMode)
        {
            SteamNetworkingIdentity id = new SteamNetworkingIdentity();
            id.SetSteamID(steamId);
            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr pData = pinnedArray.AddrOfPinnedObject();
            int sendFlags = 0;
            switch (sendMode)
            {
                case P2PSendMode.Unreliable:
                    sendFlags = Constants.k_nSteamNetworkingSend_Unreliable;
                    break;
                case P2PSendMode.Reliable:
                    sendFlags = Constants.k_nSteamNetworkingSend_Reliable;
                    break;
            }
            var result = SteamNetworkingMessages.SendMessageToUser(ref id, pData, (uint)data.Length, sendFlags | Constants.k_nSteamNetworkingSend_AutoRestartBrokenSession, P2PSteamworksService.CHANNEL);
            if (result != EResult.k_EResultOK)
            {
                P2PLogger.Error($"Error sending message to user {steamId}: {result}");
            }
            pinnedArray.Free();
        }

        public override void SetData(string key, string value)
        {
            if(SteamUser.GetSteamID() == steamId)
            {
                SteamMatchmaking.SetLobbyMemberData(P2PSteamworksService.CurrentLobby, key, value);
            }
        }

        public override string GetData(string key)
        {
            return SteamMatchmaking.GetLobbyMemberData(P2PSteamworksService.CurrentLobby, steamId, key);
        }
    }
}
