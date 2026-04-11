using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;

namespace GladiatorManagerAccess
{
    public class MainMenuHandler : IAccessibleHandler
    {
        private int _currentIndex = 0;
        private bool _wasOpen = false;
        private LevelManager _levelManager;
        private List<Button> _buttons = new List<Button>();

        public string GetHelpText()
        {
            return "Main Menu: Use Up and Down arrows to navigate options. Press Enter to select an option.";
        }

        public static bool IsMainMenuOpen()
        {
            // Level 1 is the main menu ("New Game" scene name)
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return sceneName == "New Game";
        }

        public void Update()
        {
            bool isOpen = IsMainMenuOpen();

            if (isOpen && !_wasOpen)
            {
                OnOpen();
            }
            else if (!isOpen && _wasOpen)
            {
                OnClose();
            }

            _wasOpen = isOpen;

            if (!isOpen) return;

            if (AccessStateManager.Current != AccessStateManager.State.MainMenu)
            {
                AccessStateManager.TryEnter(AccessStateManager.State.MainMenu);
            }

            ProcessInput();
        }

        private void OnOpen()
        {
            DebugLogger.LogState("Main Menu opened");
            _currentIndex = 0;
            _buttons.Clear();
            _levelManager = Object.FindObjectOfType<LevelManager>();
            
            RefreshButtons();
            
            ScreenReader.Say(Loc.Get("main_menu_opened"));
            AnnounceCurrentItem();
        }

        private void OnClose()
        {
            DebugLogger.LogState("Main Menu closed");
            AccessStateManager.Exit(AccessStateManager.State.MainMenu);
        }

        private void RefreshButtons()
        {
            _buttons.Clear();
            
            // First, try to find all buttons in the scene
            Button[] allButtons = Object.FindObjectsOfType<Button>();
            DebugLogger.LogState($"Found {allButtons.Length} total buttons in scene.");

            foreach (var btn in allButtons)
            {
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;

                // We want buttons that are either assigned to LevelManager OR have recognizable text
                bool shouldAdd = false;
                string btnText = "";

                var textComp = btn.GetComponentInChildren<Text>();
                if (textComp != null) btnText = textComp.text.ToLower();

                // 1. Check LevelManager fields
                if (_levelManager != null)
                {
                    if (btn == _levelManager.continueButton && btn.interactable) shouldAdd = true;
                    else if (btn == _levelManager.loadButton && btn.interactable) shouldAdd = true;
                    else if (btn == _levelManager.newGameButton) shouldAdd = true;
                }

                // 2. Check by name or text if not already added (fallback)
                if (!shouldAdd)
                {
                    string name = btn.name.ToLower();
                    if (name.Contains("new") || btnText.Contains("new")) shouldAdd = true;
                    else if ((name.Contains("load") || btnText.Contains("load")) && btn.interactable) shouldAdd = true;
                    else if ((name.Contains("continue") || btnText.Contains("continue")) && btn.interactable) shouldAdd = true;
                    else if (name.Contains("exit") || name.Contains("quit") || btnText.Contains("exit") || btnText.Contains("quit")) shouldAdd = true;
                    else if (name.Contains("option") || btnText.Contains("option")) shouldAdd = true;
                    else if (name.Contains("credit") || btnText.Contains("credit")) shouldAdd = true;
                }

                if (shouldAdd && !_buttons.Contains(btn))
                {
                    _buttons.Add(btn);
                    DebugLogger.LogState($"Added button: {btn.name} (Text: '{btnText}')");
                }
            }

            // Sort buttons by vertical position (top to bottom) if they have RectTransforms
            _buttons.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                Navigate(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                Navigate(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ActivateCurrent();
            }
        }

        private void Navigate(int direction)
        {
            if (_buttons.Count == 0) return;

            _currentIndex += direction;
            if (_currentIndex < 0) _currentIndex = _buttons.Count - 1;
            if (_currentIndex >= _buttons.Count) _currentIndex = 0;

            AnnounceCurrentItem();
        }

        private void ActivateCurrent()
        {
            if (_currentIndex >= 0 && _currentIndex < _buttons.Count)
            {
                var btn = _buttons[_currentIndex];
                DebugLogger.LogInput("Enter", $"Activating {btn.name}");
                btn.onClick.Invoke();
            }
        }

        private void AnnounceCurrentItem()
        {
            if (_buttons.Count == 0) return;

            var btn = _buttons[_currentIndex];
            string text = "";
            
            var textComp = btn.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                text = textComp.text;
            }
            else 
            {
                text = btn.name;
            }

            ScreenReader.Say(Loc.Get("menu_item", _currentIndex + 1, _buttons.Count, text));
        }
    }
}
