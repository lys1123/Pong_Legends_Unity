using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace PongLegends
{
    public class CharacterSelectManager : MonoBehaviour
    {
        [SerializeField] private SessionData sessionData;
        [SerializeField] private CharacterDefinition[] allCharacters; // 8 entries, set in Inspector
        [SerializeField] private CharacterSlot[] slots;               // 8 slots, set in Inspector

        private int _selectedIndex;
        private bool _confirmed;

        private void Start()
        {
            EnsureEventSystem();

            for (int i = 0; i < slots.Length && i < allCharacters.Length; i++)
                slots[i].Initialize(allCharacters[i], i, OnSlotClicked);

            UpdateSelection();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        private void Update()
        {
            if (_confirmed) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            int prev = _selectedIndex;

            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
                _selectedIndex = (_selectedIndex + 1) % slots.Length;
            if (kb.leftArrowKey.wasPressedThisFrame  || kb.aKey.wasPressedThisFrame)
                _selectedIndex = (_selectedIndex - 1 + slots.Length) % slots.Length;
            if (kb.downArrowKey.wasPressedThisFrame  || kb.sKey.wasPressedThisFrame)
                _selectedIndex = (_selectedIndex + 4) % slots.Length;
            if (kb.upArrowKey.wasPressedThisFrame    || kb.wKey.wasPressedThisFrame)
                _selectedIndex = (_selectedIndex - 4 + slots.Length) % slots.Length;

            if (_selectedIndex != prev)
                UpdateSelection();

            if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                Confirm();
        }

        public void OnSlotClicked(int index)
        {
            if (_confirmed) return;
            _selectedIndex = index;
            UpdateSelection();
            Confirm();
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetSelected(i == _selectedIndex);
        }

        private void Confirm()
        {
            _confirmed = true;

            sessionData.playerCharacter = allCharacters[_selectedIndex];

            // Pick a random AI opponent that is different from the player
            int aiIndex;
            do { aiIndex = Random.Range(0, allCharacters.Length); }
            while (aiIndex == _selectedIndex);
            sessionData.aiCharacter = allCharacters[aiIndex];

            SceneManager.LoadScene("Game");
        }
    }
}
