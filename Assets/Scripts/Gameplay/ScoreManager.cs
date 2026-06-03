using System;
using TMPro;
using UnityEngine;

namespace PongLegends
{
    public class ScoreManager : MonoBehaviour
    {
        private const int WinScore = 5;

        public event Action<bool> OnGameOver; // true = player won

        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI aiScoreText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI aiNameText;
        [SerializeField] private WinnerOverlay winnerOverlay;

        private int _playerScore;
        private int _aiScore;
        private bool _gameOver;
        private string _playerName;
        private string _aiName;

        public bool IsGameOver  => _gameOver;
        public int  PlayerScore => _playerScore;
        public int  AIScore     => _aiScore;

        public void Initialize(string playerName, string aiName)
        {
            _playerScore = 0;
            _aiScore     = 0;
            _gameOver    = false;
            _playerName  = playerName;
            _aiName      = aiName;

            if (playerNameText != null) playerNameText.text = playerName.ToUpper();
            if (aiNameText     != null) aiNameText.text     = aiName.ToUpper();

            UpdateUI();
        }

        public void HandleScore(PaddleSide scorer)
        {
            if (_gameOver) return;

            if (scorer == PaddleSide.Player) _playerScore++;
            else                             _aiScore++;

            UpdateUI();
            CheckWin(scorer);
        }

        private void CheckWin(PaddleSide lastScorer)
        {
            if (_playerScore >= WinScore)
            {
                _gameOver = true;
                winnerOverlay?.Show(true,  _playerName);
                OnGameOver?.Invoke(true);
            }
            else if (_aiScore >= WinScore)
            {
                _gameOver = true;
                winnerOverlay?.Show(false, _aiName);
                OnGameOver?.Invoke(false);
            }
        }

        // Called on client/spectator when receiving SyncScore event from host.
        // Updates display only — does not trigger win-check logic.
        public void ForceSetScore(int p1Score, int p2Score)
        {
            _playerScore = p1Score;
            _aiScore     = p2Score;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (playerScoreText != null) playerScoreText.text = _playerScore.ToString();
            if (aiScoreText     != null) aiScoreText.text     = _aiScore.ToString();
        }
    }
}
