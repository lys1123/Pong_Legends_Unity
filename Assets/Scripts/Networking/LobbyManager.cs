// ─── LOBBY SCENE SETUP ───────────────────────────────────────────────────────
// Create a new scene called "Lobby" and add it to Build Settings.
// Add a Canvas with the following hierarchy:
//
//   Canvas
//   ├── Panel_ModeSelect          (shown on start)
//   │   ├── Button "PLAY VS AI"   → calls LobbyManager.PlayOffline()
//   │   └── Button "PLAY ONLINE"  → calls LobbyManager.ShowHostJoinPanel()
//   │
//   ├── Panel_HostJoin            (hidden)
//   │   ├── Button "CREATE GAME"  → calls LobbyManager.CreateRoom()
//   │   ├── TMP_InputField        → wire to joinCodeField
//   │   ├── Button "JOIN GAME"    → calls LobbyManager.JoinRoom()
//   │   ├── TMP_InputField        → wire to spectateCodeField
//   │   ├── Button "SPECTATE"     → calls LobbyManager.JoinAsSpectator()
//   │   └── Button "BACK"         → calls LobbyManager.ShowModePanel()
//   │
//   └── Panel_WaitingRoom         (hidden)
//       ├── TextMeshPro           → wire to roomCodeText   (shows the invite code)
//       ├── TextMeshPro           → wire to shareURLText   (shows full shareable URL)
//       ├── Button "COPY LINK"    → calls LobbyManager.CopyLink()
//       ├── TextMeshPro           → wire to statusText
//       ├── [CharacterSlot x8]    → wire allCharacters + slots arrays
//       ├── Button "CONFIRM"      → calls LobbyManager.ConfirmCharacter()
//       └── Button "CANCEL"       → calls LobbyManager.LeaveRoom()
//
// Add a NetworkManager GameObject to the scene (or let it DontDestroyOnLoad from earlier).
// Wire the LobbyManager's sessionData field to your SessionData ScriptableObject asset.
// Wire allCharacters[] to the 8 CharacterDefinition assets (same order as CharacterSelect).
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace PongLegends
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
    using Photon.Realtime;
    using ExitGames.Client.Photon;

    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        public static LobbyManager Instance { get; private set; }

        [Header("Session")]
        [SerializeField] private SessionData sessionData;
        [SerializeField] private CharacterDefinition[] allCharacters;

        [Header("Panels")]
        [SerializeField] private GameObject panelModeSelect;
        [SerializeField] private GameObject panelHostJoin;
        [SerializeField] private GameObject panelWaitingRoom;

        [Header("Host/Join UI")]
        [SerializeField] private TMP_InputField joinCodeField;
        [SerializeField] private TMP_InputField spectateCodeField;

        [Header("Waiting Room UI")]
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private TextMeshProUGUI shareURLText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private CharacterSlot[] slots;

        private int  _localCharIdx      = 0;
        private bool _charConfirmed;
        private bool _isSpectator;
        private string _shareURL;

        private const string PropCharIdx    = "charIdx";
        private const string PropSpectator  = "isSpectator";

        // ── Lifecycle ───────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;

            // Ensure NetworkManager exists in this scene.
            if (NetworkManager.Instance == null)
                new GameObject("NetworkManager").AddComponent<NetworkManager>();

            NetworkManager.Instance.OnRoomCreated  += HandleRoomCreated;
            NetworkManager.Instance.OnJoinFailed   += HandleJoinFailed;
            NetworkManager.Instance.OnOpponentJoined += HandleOpponentJoined;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomCreated   -= HandleRoomCreated;
                NetworkManager.Instance.OnJoinFailed    -= HandleJoinFailed;
                NetworkManager.Instance.OnOpponentJoined -= HandleOpponentJoined;
            }
        }

        private void Start()
        {
            ShowPanel(panelModeSelect);

            // Build character grid.
            for (int i = 0; i < slots.Length && i < allCharacters.Length; i++)
            {
                int captured = i;
                slots[i].Initialize(allCharacters[i], i, idx =>
                {
                    _localCharIdx = idx;
                    RefreshSlotSelection();
                });
            }
            RefreshSlotSelection();

            // Auto-fill code if arriving via a URL link.
            string roomFromURL = URLParams.Get("room");
            if (!string.IsNullOrEmpty(roomFromURL))
            {
                ShowPanel(panelHostJoin);
                if (joinCodeField  != null) joinCodeField.text  = roomFromURL.ToUpper();
            }

            // Connect to Photon if not already connected.
            if (!PhotonNetwork.IsConnected)
                NetworkManager.Instance.Connect();
        }

        private void RefreshSlotSelection()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetSelected(i == _localCharIdx);
        }

        // ── Panel navigation ────────────────────────────────────────────────

        private void ShowPanel(GameObject panel)
        {
            panelModeSelect?.SetActive(panel == panelModeSelect);
            panelHostJoin?.SetActive(panel == panelHostJoin);
            panelWaitingRoom?.SetActive(panel == panelWaitingRoom);
        }

        public void ShowModePanel()    => ShowPanel(panelModeSelect);
        public void ShowHostJoinPanel()=> ShowPanel(panelHostJoin);

        // ── Button handlers ─────────────────────────────────────────────────

        public void PlayOffline()
        {
            sessionData.networkMode = NetworkMode.Offline;
            SceneManager.LoadScene("CharacterSelect");
        }

        public void CreateRoom()
        {
            _isSpectator = false;
            SetStatus("Creating room…");
            NetworkManager.Instance.CreateRoom();
        }

        public void JoinRoom()
        {
            string code = joinCodeField?.text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code)) return;
            _isSpectator = false;
            SetStatus($"Joining {code.ToUpper()}…");
            NetworkManager.Instance.JoinRoom(code);
        }

        public void JoinAsSpectator()
        {
            string code = spectateCodeField?.text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(code)) return;
            _isSpectator = true;
            SetStatus($"Joining as spectator…");
            NetworkManager.Instance.JoinRoom(code);
        }

        public void ConfirmCharacter()
        {
            if (_charConfirmed) return;
            _charConfirmed = true;

            var props = new Hashtable { { PropCharIdx, _localCharIdx } };
            if (_isSpectator) props[PropSpectator] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            SetStatus(_isSpectator ? "Watching…" : "Ready! Waiting for opponent…");
        }

        public void LeaveRoom()
        {
            _charConfirmed = false;
            NetworkManager.Instance.LeaveRoom();
            ShowPanel(panelModeSelect);
        }

        public void CopyLink()
        {
            if (!string.IsNullOrEmpty(_shareURL))
                URLParams.CopyToClipboard(_shareURL);
        }

        // ── Photon callbacks ────────────────────────────────────────────────

        private void HandleRoomCreated(string code)
        {
            _shareURL = $"{URLParams.PageOrigin()}?room={code}";
            ShowPanel(panelWaitingRoom);
            if (roomCodeText != null) roomCodeText.text = code;
            if (shareURLText != null) shareURLText.text = _shareURL;
            SetStatus("Waiting for opponent…");
        }

        private void HandleJoinFailed(string message)
        {
            SetStatus($"Could not join: {message}");
        }

        private void HandleOpponentJoined()
        {
            SetStatus("Opponent connected! Pick your character and hit Confirm.");
        }

        public override void OnJoinedRoom()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                // Client joined — show waiting room so they can pick a character.
                string code = NetworkManager.Instance.RoomCode;
                _shareURL = $"{URLParams.PageOrigin()}?room={code}";
                ShowPanel(panelWaitingRoom);
                if (roomCodeText != null) roomCodeText.text = code;
                if (shareURLText != null) shareURLText.text = _shareURL;
                SetStatus("Pick your character and hit Confirm.");
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            TryStartGame();
        }

        private void TryStartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2) return;

            Player host  = PhotonNetwork.MasterClient;
            Player guest = null;
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber != host.ActorNumber &&
                    !(p.CustomProperties.ContainsKey(PropSpectator) &&
                      (bool)p.CustomProperties[PropSpectator]))
                {
                    guest = p;
                    break;
                }
            }
            if (guest == null) return;

            if (!host.CustomProperties.ContainsKey(PropCharIdx)) return;
            if (!guest.CustomProperties.ContainsKey(PropCharIdx)) return;

            int p1Idx = (int)host.CustomProperties[PropCharIdx];
            int p2Idx = (int)guest.CustomProperties[PropCharIdx];

            // Tell all machines which characters to load, then each loads the scene.
            NetworkManager.RaiseAll(NetEvent.StartGame,
                new object[] { p1Idx, p2Idx }, reliable: true, cache: true);
        }

        // Called by NetworkManager.OnEvent when StartGame arrives.
        public void HandleStartGame(int p1CharIdx, int p2CharIdx)
        {
            sessionData.playerCharacter = allCharacters[p1CharIdx];
            sessionData.aiCharacter     = allCharacters[p2CharIdx];
            sessionData.networkMode     = _isSpectator
                ? NetworkMode.Spectator
                : PhotonNetwork.IsMasterClient
                    ? NetworkMode.OnlineHost
                    : NetworkMode.OnlineClient;

            SceneManager.LoadScene("Game");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }
    }

#else
    // Stub when Photon PUN 2 is not installed.
    public class LobbyManager : UnityEngine.MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        [SerializeField] private SessionData sessionData;
        [SerializeField] private CharacterDefinition[] allCharacters;
        [SerializeField] private UnityEngine.GameObject panelModeSelect;
        [SerializeField] private UnityEngine.GameObject panelHostJoin;
        [SerializeField] private UnityEngine.GameObject panelWaitingRoom;
        [SerializeField] private TMP_InputField joinCodeField;
        [SerializeField] private TMP_InputField spectateCodeField;
        [SerializeField] private TextMeshProUGUI roomCodeText;
        [SerializeField] private TextMeshProUGUI shareURLText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private CharacterSlot[] slots;
        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void PlayOffline()       => UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
        public void ShowModePanel()     { }
        public void ShowHostJoinPanel() { }
        public void CreateRoom()        { }
        public void JoinRoom()          { }
        public void JoinAsSpectator()   { }
        public void ConfirmCharacter()  { }
        public void LeaveRoom()         { }
        public void CopyLink()          { }
        public void HandleStartGame(int p1CharIdx, int p2CharIdx) { }
    }
#endif
}
