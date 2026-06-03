using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PongLegends
{
    public class GameplayManager : MonoBehaviour
    {
        [SerializeField] private SessionData sessionData;
        [SerializeField] private Ball ball;
        [SerializeField] private Paddle playerPaddle;
        [SerializeField] private Paddle aiPaddle;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private AbilitySystem abilitySystem;
        [SerializeField] private TMPro.TextMeshProUGUI scoreBannerText;
        [SerializeField] private TMPro.TextMeshProUGUI instructionsText;

        private bool        _gameOver;
        private NetworkMode _networkMode;

        private void Start()
        {
            if (sessionData == null)
            {
                Debug.LogError("GameplayManager: SessionData not assigned.");
                return;
            }

            CharacterDefinition playerDef = sessionData.playerCharacter;
            CharacterDefinition aiDef     = sessionData.aiCharacter;
            _networkMode                  = sessionData.networkMode;

            if (playerDef == null || aiDef == null)
            {
                Debug.LogError("GameplayManager: Characters not set in SessionData. Play from CharacterSelect first.");
                return;
            }

            playerPaddle.transform.position = new Vector3(-5.9f, 0f, 0f);
            aiPaddle.transform.position     = new Vector3( 5.9f, 0f, 0f);

            if (_networkMode == NetworkMode.Offline)
            {
                // ── Offline (original path, unchanged) ──────────────────────────
                playerPaddle.Initialize(playerDef, PaddleMode.Player,   ball);
                aiPaddle.Initialize    (aiDef,     PaddleMode.AI,       ball);
            }
            else
            {
                // ── Online modes ─────────────────────────────────────────────────
                switch (_networkMode)
                {
                    case NetworkMode.OnlineHost:
                        // P1 (left) is local; P2 (right) is remote.
                        playerPaddle.Initialize(playerDef, PaddleMode.Player,       ball);
                        aiPaddle.Initialize    (aiDef,     PaddleMode.RemotePlayer, ball);
                        break;

                    case NetworkMode.OnlineClient:
                        // P1 (left) is remote; P2 (right) is local.
                        playerPaddle.Initialize(playerDef, PaddleMode.RemotePlayer, ball);
                        aiPaddle.Initialize    (aiDef,     PaddleMode.Player,       ball);
                        ball.SetNetworkControlled(true);
                        break;

                    case NetworkMode.Spectator:
                        playerPaddle.Initialize(playerDef, PaddleMode.RemotePlayer, ball);
                        aiPaddle.Initialize    (aiDef,     PaddleMode.RemotePlayer, ball);
                        ball.SetNetworkControlled(true);
                        abilitySystem.enabled = false;
                        break;
                }

                // Attach sync components (they register themselves via Instance).
                var ballSync = gameObject.AddComponent<BallNetworkSync>();
                ballSync.Initialize(ball, _networkMode);

                var p1Sync = playerPaddle.gameObject.AddComponent<PaddleNetworkSync>();
                p1Sync.Initialize(playerPaddle, PaddleSide.Player, _networkMode);

                var p2Sync = aiPaddle.gameObject.AddComponent<PaddleNetworkSync>();
                p2Sync.Initialize(aiPaddle, PaddleSide.AI, _networkMode);

                var abilityBridge = gameObject.AddComponent<NetworkAbilityBridge>();
                abilityBridge.Initialize(abilitySystem, playerPaddle, aiPaddle, _networkMode);

                var gameBridge = gameObject.AddComponent<GameNetworkBridge>();
                gameBridge.Initialize(scoreManager, FindAnyObjectByType<WinnerOverlay>(),
                                      ball, _networkMode);
            }

            ball.SetPaddles(playerPaddle, aiPaddle);
            scoreManager.Initialize(playerDef.characterName, aiDef.characterName);
            abilitySystem.Initialize(sessionData, ball, playerPaddle, aiPaddle);

            if (instructionsText != null)
            {
                instructionsText.text = _networkMode == NetworkMode.Offline
                    ? $"↑↓ move  •  A/S/D kicks  •  SPACE for \"{playerDef.abilityType.DisplayName()}\"  •  ESC quit"
                    : _networkMode == NetworkMode.Spectator
                        ? "Spectating"
                        : $"↑↓ move  •  A/S/D kicks  •  SPACE for \"{playerDef.abilityType.DisplayName()}\"";
            }

            ball.OnScore             += HandleScore;
            scoreManager.OnGameOver  += _ => HandleGameOver();

            PaddleSide firstServe = _networkMode == NetworkMode.OnlineClient
                ? PaddleSide.AI    // client waits for host's serve signal
                : PaddleSide.Player;
            StartCoroutine(ServeAfterDelay(1f, firstServe));
        }

        private void HandleGameOver()
        {
            _gameOver                 = true;
            playerPaddle.enabled      = false;
            aiPaddle.enabled          = false;
            abilitySystem.enabled     = false;

            // On host, broadcast game-over to all clients.
            if (_networkMode == NetworkMode.OnlineHost)
            {
                bool p1Won    = scoreManager.IsGameOver; // already true here
                var def       = p1Won ? sessionData.playerCharacter : sessionData.aiCharacter;
                string winner = def != null ? def.characterName : "???";
                if (GameNetworkBridge.Instance != null)
                    GameNetworkBridge.Instance.SendGameOver(p1Won, winner);
            }
        }

        private void HandleScore(PaddleSide scorer)
        {
            scoreManager.HandleScore(scorer);

            if (_networkMode == NetworkMode.OnlineHost)
            {
                // Mirror score to all clients.
                // ScoreManager.HandleScore updated the internal counters — we need to read them back.
                // Expose via a trivial accessor or re-derive from the winner check.
                // We use a small helper on ScoreManager added below.
                GameNetworkBridge.Instance?.SendSyncScore(
                    scoreManager.PlayerScore, scoreManager.AIScore);
            }

            if (!scoreManager.IsGameOver)
            {
                ShowScoreBanner(scorer);
                StartCoroutine(ServeAfterDelay(1.5f, scorer));
            }
        }

        private void ShowScoreBanner(PaddleSide scorer)
        {
            if (scoreBannerText == null) return;
            string name = scorer == PaddleSide.Player
                ? sessionData.playerCharacter.characterName
                : sessionData.aiCharacter.characterName;
            scoreBannerText.text = $"{name} Scores!".ToUpper();
            scoreBannerText.gameObject.SetActive(true);
        }

        private IEnumerator ServeAfterDelay(float delay, PaddleSide lastScorer)
        {
            yield return new WaitForSeconds(delay);
            if (_gameOver) yield break;
            if (scoreBannerText != null) scoreBannerText.gameObject.SetActive(false);

            // Clients don't call Serve() — ball physics runs on host only.
            if (_networkMode == NetworkMode.OnlineClient ||
                _networkMode == NetworkMode.Spectator) yield break;

            PaddleSide serveDir = lastScorer == PaddleSide.Player ? PaddleSide.AI : PaddleSide.Player;
            ball.Serve(serveDir);

            if (_networkMode == NetworkMode.OnlineHost)
                GameNetworkBridge.Instance?.SendServeSignal((int)serveDir);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.escapeKey.wasPressedThisFrame) return;

            // ESC: offline goes back to CharacterSelect; online only host can leave.
            if (_networkMode == NetworkMode.Offline)
            {
                SceneManager.LoadScene("CharacterSelect");
            }
            else if (_networkMode == NetworkMode.OnlineHost)
            {
                GameNetworkBridge.HandleReturnToLobby();
            }
            // Client/Spectator: ESC does nothing — host controls navigation.
        }
    }
}
