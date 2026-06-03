using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PongLegends
{
    public class WinnerOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI promptText;

        private bool _showing;
        private bool _suppressInput; // true on client/spectator — host controls navigation

        public void Show(bool playerWon, string winnerName)
        {
            _showing = true;
            panel?.SetActive(true);
            SoundManager.Play("game_over");

            if (resultText != null)
            {
                resultText.text  = $"{winnerName} Wins!".ToUpper();
                resultText.color = new Color(1f, 0.84f, 0f);
            }

            if (nameText != null)
                nameText.gameObject.SetActive(false);

            StartCoroutine(FlashPrompt());
        }

        private IEnumerator FlashPrompt()
        {
            while (_showing)
            {
                if (promptText != null) promptText.alpha = 1f;
                yield return new WaitForSeconds(0.5f);
                if (promptText != null) promptText.alpha = 0f;
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Called by GameNetworkBridge on client/spectator machines instead of Show().
        public void ShowFromNetwork(bool p1Won, string winnerName)
        {
            _suppressInput = true;
            Show(p1Won, winnerName);
        }

        // Called by GameNetworkBridge when host sends ReturnToLobby.
        public void ReturnToLobby()
        {
            _showing = false;
            SceneManager.LoadScene("Lobby");
        }

        private void Update()
        {
            if (!_showing || _suppressInput) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.enterKey.wasPressedThisFrame)
                GameNetworkBridge.HandleReturnToLobby();
        }
    }
}
