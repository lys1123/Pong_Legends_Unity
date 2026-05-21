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

        private bool _gameOver;

        private void Start()
        {
            if (sessionData == null)
            {
                Debug.LogError("GameplayManager: SessionData not assigned.");
                return;
            }

            CharacterDefinition playerDef = sessionData.playerCharacter;
            CharacterDefinition aiDef     = sessionData.aiCharacter;

            if (playerDef == null || aiDef == null)
            {
                Debug.LogError("GameplayManager: Characters not set in SessionData. Play from the CharacterSelect scene first.");
                return;
            }

            playerPaddle.transform.position = new Vector3(-5.9f, 0f, 0f);
            aiPaddle.transform.position     = new Vector3( 5.9f, 0f, 0f);

            playerPaddle.Initialize(playerDef, PaddleMode.Player, ball);
            aiPaddle.Initialize(aiDef,         PaddleMode.AI,     ball);

            ball.SetPaddles(playerPaddle, aiPaddle);

            scoreManager.Initialize(playerDef.characterName, aiDef.characterName);
            abilitySystem.Initialize(sessionData, ball, playerPaddle, aiPaddle);

            if (instructionsText != null)
                instructionsText.text = $"↑↓ to move  •  A/S/D for kicks  •  SPACE for \"{playerDef.abilityType.DisplayName()}\"  •  ESC to quit";

            ball.OnScore    += HandleScore;
            scoreManager.OnGameOver += _ => HandleGameOver();

            StartCoroutine(ServeAfterDelay(1f, PaddleSide.Player));
        }

        private void HandleGameOver()
        {
            _gameOver = true;
            playerPaddle.enabled  = false;
            aiPaddle.enabled      = false;
            abilitySystem.enabled = false;
        }

        private void HandleScore(PaddleSide scorer)
        {
            scoreManager.HandleScore(scorer);
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
            PaddleSide serveDirection = lastScorer == PaddleSide.Player ? PaddleSide.AI : PaddleSide.Player;
            ball.Serve(serveDirection);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                SceneManager.LoadScene("CharacterSelect");
        }
    }
}
