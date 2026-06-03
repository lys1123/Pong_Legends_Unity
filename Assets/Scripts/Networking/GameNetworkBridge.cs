using UnityEngine;
using UnityEngine.SceneManagement;

namespace PongLegends
{
    // Handles game-flow events: score sync, game over, serve signal, return-to-lobby.
#if PHOTON_UNITY_NETWORKING
    using Photon.Realtime;
    using ExitGames.Client.Photon;

    public class GameNetworkBridge : MonoBehaviour
    {
        public static GameNetworkBridge Instance { get; private set; }

        private ScoreManager   _scoreManager;
        private WinnerOverlay  _winnerOverlay;
        private Ball           _ball;
        private NetworkMode    _mode;

        public void Initialize(ScoreManager scores, WinnerOverlay overlay,
                               Ball ball, NetworkMode mode)
        {
            Instance       = this;
            _scoreManager  = scores;
            _winnerOverlay = overlay;
            _ball          = ball;
            _mode          = mode;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        // ── Host → Others ───────────────────────────────────────────────────

        public void SendSyncScore(int p1Score, int p2Score)
        {
            NetworkManager.RaiseOthers(NetEvent.SyncScore,
                new object[] { p1Score, p2Score }, reliable: true);
        }

        public void SendGameOver(bool p1Won, string winnerName)
        {
            NetworkManager.RaiseOthers(NetEvent.GameOver,
                new object[] { p1Won, winnerName }, reliable: true);
        }

        public void SendServeSignal(int direction)
        {
            NetworkManager.RaiseOthers(NetEvent.ServeSignal, direction, reliable: true);
        }

        // Called from WinnerOverlay when host presses Enter.
        public static void HandleReturnToLobby()
        {
            if (Instance == null || Instance._mode == NetworkMode.Offline)
            {
                SceneManager.LoadScene("CharacterSelect");
                return;
            }
            if (Instance._mode == NetworkMode.OnlineHost)
            {
                NetworkManager.RaiseAll(NetEvent.ReturnToLobby, null, reliable: true);
                // Local load handled by OnNetworkEvent below.
            }
            // Client/Spectator: wait for host's ReturnToLobby event.
        }

        // ── Incoming event dispatch ─────────────────────────────────────────

        public void OnNetworkEvent(EventData ev)
        {
            switch (ev.Code)
            {
                case NetEvent.SyncScore when _mode != NetworkMode.OnlineHost:
                {
                    var data = (object[])ev.CustomData;
                    _scoreManager?.ForceSetScore((int)data[0], (int)data[1]);
                    break;
                }
                case NetEvent.GameOver when _mode != NetworkMode.OnlineHost:
                {
                    var data       = (object[])ev.CustomData;
                    bool p1Won     = (bool)data[0];
                    string winner  = (string)data[1];
                    _winnerOverlay?.ShowFromNetwork(p1Won, winner);
                    break;
                }
                case NetEvent.ServeSignal when _mode != NetworkMode.OnlineHost:
                {
                    // Prepare ball visual state on client before first sync packet arrives.
                    _ball?.SetColor(UnityEngine.Color.white);
                    break;
                }
                case NetEvent.ReturnToLobby:
                {
                    if (NetworkManager.Instance != null)
                        NetworkManager.Instance.LeaveRoom();
                    SceneManager.LoadScene("Lobby");
                    break;
                }
            }
        }

        // Called by NetworkManager when host disconnects mid-game.
        public void HandleHostDisconnect()
        {
            if (_winnerOverlay != null)
                _winnerOverlay.ShowFromNetwork(false, "Opponent Disconnected");
        }
    }

#else
    public class GameNetworkBridge : UnityEngine.MonoBehaviour
    {
        public static GameNetworkBridge Instance { get; private set; }
        public void Initialize(ScoreManager s, WinnerOverlay o, Ball b, NetworkMode m)
            => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void SendSyncScore(int p1Score, int p2Score)       { }
        public void SendGameOver(bool p1Won, string winnerName)    { }
        public void SendServeSignal(int direction)                  { }
        public static void HandleReturnToLobby()
            => UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
        public void OnNetworkEvent(object ev)                      { }
        public void HandleHostDisconnect()                         { }
    }
#endif
}
