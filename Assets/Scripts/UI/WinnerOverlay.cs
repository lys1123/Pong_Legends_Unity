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

        private void Awake()
        {
            panel?.SetActive(false);
        }

        public void Show(bool playerWon, string winnerName)
        {
            _showing = true;
            panel?.SetActive(true);

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

        private void Update()
        {
            if (!_showing) return;
            var kb = Keyboard.current;
            if (kb != null && kb.enterKey.wasPressedThisFrame)
                SceneManager.LoadScene("CharacterSelect");
        }
    }
}
