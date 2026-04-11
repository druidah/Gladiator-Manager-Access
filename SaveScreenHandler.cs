using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;

namespace GladiatorManagerAccess
{
    public class SaveScreenHandler : IAccessibleHandler
    {
        private int _currentIndex = 0;
        private bool _wasOpen = false;
        private LevelManager _levelManager;
        private List<Button> _buttons = new List<Button>();

        public string GetHelpText()
        {
            return "Save Selection: Use Up and Down arrows to select a save slot. Press Enter to load or start a new game in that slot. Press Escape to return to the Main Menu.";
        }

        public static bool IsSaveScreenOpen()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SaveScreen";
        }

        public void Update()
        {
            bool isOpen = IsSaveScreenOpen();

            if (isOpen && !_wasOpen)
            {
                AccessStateManager.TryEnter(AccessStateManager.State.SaveScreen);
                OnOpen();
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.SaveScreen);
                OnClose();
            }

            _wasOpen = isOpen;

            if (!isOpen || !AccessStateManager.IsIn(AccessStateManager.State.SaveScreen)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            DebugLogger.LogState("Save Screen opened");
            _currentIndex = 0;
            _buttons.Clear();
            _levelManager = Object.FindObjectOfType<LevelManager>();
            
            RefreshButtons();
            
            ScreenReader.Say(Loc.Get("save_screen_opened"));
            
            // Announce explanation if exists
            if (_levelManager != null && _levelManager.saveExplanation != null)
            {
                ScreenReader.SayQueued(_levelManager.saveExplanation.text);
            }

            AnnounceCurrentItem();
        }

        private void OnClose()
        {
            DebugLogger.LogState("Save Screen closed");
        }

        private void RefreshButtons()
        {
            _buttons.Clear();
            if (_levelManager != null && _levelManager.saveGames != null)
            {
                foreach (var btn in _levelManager.saveGames)
                {
                    if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
                    {
                        _buttons.Add(btn);
                    }
                }
            }
            
            // Add any Back/Cancel button
            var allButtons = Object.FindObjectsOfType<Button>();
            foreach (var btn in allButtons)
            {
                if (btn == null || !btn.gameObject.activeInHierarchy) continue;
                string name = btn.name.ToLower();
                var textComp = btn.GetComponentInChildren<Text>();
                string btnText = textComp != null ? textComp.text.ToLower() : "";

                if (name.Contains("back") || btnText.Contains("back") || name.Contains("cancel") || btnText.Contains("cancel"))
                {
                    if (!_buttons.Contains(btn)) _buttons.Add(btn);
                }
            }
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
                
                // If we are starting a new game, the game might change the button text to a warning.
                // We re-announce the current item after a short delay to capture the change.
                MelonCoroutines.Start(DelayedReannounce());
            }
        }

        private System.Collections.IEnumerator DelayedReannounce()
        {
            yield return new WaitForSeconds(0.1f);
            AnnounceCurrentItem();
        }

        private void AnnounceCurrentItem()
        {
            if (_buttons.Count == 0) 
            {
                ScreenReader.Say(Loc.Get("no_slots_available"));
                return;
            }

            var btn = _buttons[_currentIndex];
            string text = "";
            
            var textComp = btn.GetComponentInChildren<Text>();
            if (textComp != null && !string.IsNullOrEmpty(textComp.text))
            {
                text = textComp.text;
                // If the game has changed the button text to the overwrite warning, ensure it's read clearly.
                if (text.Contains("Are you sure"))
                {
                    text = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty); // Strip formatting
                }
            }
            else 
            {
                text = "Empty Slot " + (System.Array.IndexOf(_levelManager.saveGames, btn) + 1);
            }

            ScreenReader.Say(Loc.Get("menu_item", _currentIndex + 1, _buttons.Count, text));
        }
    }
}
