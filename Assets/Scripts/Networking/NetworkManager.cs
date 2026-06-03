// ─── SETUP INSTRUCTIONS ──────────────────────────────────────────────────────
// 1. In the Unity Editor, go to Window → Asset Store and import "PUN 2 — Free"
// 2. A wizard will open — enter your Photon AppID (create one free at photon.engine)
// 3. In PhotonServerSettings, set Protocol to WebSocket (required for WebGL)
// 4. Add a NetworkManager component to a persistent GameObject in your first scene
//    OR let it self-initialize (it creates itself on first use via Instance)
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

namespace PongLegends
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
    using Photon.Realtime;
    using ExitGames.Client.Photon;

    public class NetworkManager : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static NetworkManager Instance { get; private set; }

        private const int MaxPlayersPerRoom = 12; // 2 players + up to 10 spectators

        // Fired when room creation succeeds — LobbyManager subscribes.
        public System.Action<string> OnRoomCreated;
        // Fired when join fails.
        public System.Action<string> OnJoinFailed;
        // Fired when a second player enters (for host waiting-room UI).
        public System.Action OnOpponentJoined;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = false;
        }

        private void OnEnable()  => PhotonNetwork.AddCallbackTarget(this);
        private void OnDisable() => PhotonNetwork.RemoveCallbackTarget(this);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Connection ──────────────────────────────────────────────────────

        public void Connect()
        {
            if (PhotonNetwork.IsConnected) return;

            // WebSocket is required for WebGL — also configured in PhotonServerSettings.
            PhotonNetwork.NetworkingClient.LoadBalancingPeer.TransportProtocol =
                ExitGames.Client.Photon.ConnectionProtocol.WebSocket;

            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnConnectedToMaster()
        {
            // Join the default lobby so we can create/join rooms.
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            // Lobby is ready — LobbyManager can now call CreateRoom / JoinRoom.
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[NetworkManager] Disconnected: {cause}");
        }

        // ── Room operations ─────────────────────────────────────────────────

        public void CreateRoom()
        {
            var options = new RoomOptions
            {
                MaxPlayers  = MaxPlayersPerRoom,
                IsVisible   = false, // invite-only
                IsOpen      = true,
            };
            // Passing null lets Photon generate a random 6-char room name.
            PhotonNetwork.CreateRoom(null, options);
        }

        public void JoinRoom(string code)
        {
            PhotonNetwork.JoinRoom(code.ToUpper().Trim());
        }

        public void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        }

        public bool IsHost         => PhotonNetwork.IsMasterClient;
        public bool IsInRoom       => PhotonNetwork.InRoom;
        public string RoomCode     => PhotonNetwork.CurrentRoom?.Name ?? string.Empty;
        public int PlayerCount     => PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;

        // ── Room callbacks ──────────────────────────────────────────────────

        public override void OnCreatedRoom()
        {
            OnRoomCreated?.Invoke(PhotonNetwork.CurrentRoom.Name);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            OnJoinFailed?.Invoke(message);
        }

        public override void OnJoinedRoom()
        {
            // Nothing to do here — LobbyManager handles the UI transition.
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (PhotonNetwork.IsMasterClient)
                OnOpponentJoined?.Invoke();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // If host left mid-game, notify via game-over bridge.
            if (!PhotonNetwork.IsMasterClient)
                GameNetworkBridge.Instance?.HandleHostDisconnect();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            // Host left — the client is now master, but the game can't continue.
            GameNetworkBridge.Instance?.HandleHostDisconnect();
        }

        // ── RaiseEvent helpers ──────────────────────────────────────────────

        // Sends an event to all OTHER clients (not self) — use for state that the
        // sender already applied locally.
        public static void RaiseOthers(byte code, object data, bool reliable = false)
        {
            if (!PhotonNetwork.IsConnected) return;
            PhotonNetwork.RaiseEvent(code, data,
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
        }

        // Sends an event to ALL clients including self — use for lobby signals
        // where every machine (including host) needs to react.
        public static void RaiseAll(byte code, object data, bool reliable = true,
                                    bool cache = false)
        {
            if (!PhotonNetwork.IsConnected) return;
            var caching = cache ? EventCaching.AddToRoomCache : EventCaching.DoNotCache;
            PhotonNetwork.RaiseEvent(code, data,
                new RaiseEventOptions { Receivers = ReceiverGroup.All, CachingOption = caching },
                reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
        }

        // IOnEventCallback — dispatch to game-scene handlers.
        public void OnEvent(EventData photonEvent)
        {
            BallNetworkSync.Instance?.OnNetworkEvent(photonEvent);
            PaddleNetworkSync.DispatchEvent(photonEvent);
            GameNetworkBridge.Instance?.OnNetworkEvent(photonEvent);
            NetworkAbilityBridge.Instance?.OnNetworkEvent(photonEvent);

            // Lobby start-game event — every machine loads the game scene.
            if (photonEvent.Code == NetEvent.StartGame)
            {
                var data       = (object[])photonEvent.CustomData;
                int p1CharIdx  = (int)data[0];
                int p2CharIdx  = (int)data[1];
                LobbyManager.Instance?.HandleStartGame(p1CharIdx, p2CharIdx);
            }
        }
    }

#else
    // Stub when Photon PUN 2 is not installed — all methods are no-ops.
    public class NetworkManager : UnityEngine.MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }
        public System.Action<string> OnRoomCreated;
        public System.Action<string> OnJoinFailed;
        public System.Action OnOpponentJoined;
        private void Awake() { Instance = this; DontDestroyOnLoad(gameObject); }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Connect()    { }
        public void CreateRoom() { }
        public void JoinRoom(string code) { }
        public void LeaveRoom()  { }
        public bool   IsHost       => true;
        public bool   IsInRoom     => false;
        public string RoomCode     => string.Empty;
        public int    PlayerCount  => 0;
        public static void RaiseOthers(byte code, object data, bool reliable = false) { }
        public static void RaiseAll(byte code, object data, bool reliable = true, bool cache = false) { }
    }
#endif
}
