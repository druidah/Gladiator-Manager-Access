using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace GladiatorManagerAccess
{
    public class HomeScreenHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _currentIndex = 0;
        private List<Selectable> _mainButtons = new List<Selectable>();

        public string GetHelpText()
        {
            return "Home Screen: This is your main base. Use Up and Down arrows to navigate management screens like Barracks, Market, and Finances. Press Enter to select. Press F2 to hear your current status, including week, year, and balance.";
        }

        public static bool IsHomeOpen()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Home";
        }

        public void Update()
        {
            bool isOpen = IsHomeOpen();

            if (isOpen && !_wasOpen)
            {
                OnOpen();
                _wasOpen = true;
            }
            else if (!isOpen && _wasOpen)
            {
                _wasOpen = false;
            }

            if (!isOpen) return;

            // Auto-re-enter Home state if we are here but state was lost (e.g. after week summary dismissal)
            if (AccessStateManager.Current == AccessStateManager.State.None)
            {
                // Only if no other scene-independent overlay is active (currently none except WeekEndSummary which handles itself)
                AccessStateManager.TryEnter(AccessStateManager.State.Home);
            }

            if (!AccessStateManager.IsIn(AccessStateManager.State.Home)) return;

            // Handle InputField focus
            var current = _mainButtons.Count > 0 && _currentIndex >= 0 && _currentIndex < _mainButtons.Count ? _mainButtons[_currentIndex] : null;
            if (current is InputField input && input.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Return))
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    ScreenReader.Say("Finished editing.");
                    AnnounceCurrentItem();
                }
                return;
            }

            ProcessInput();
        }

        private void OnOpen()
        {
            DebugLogger.LogState("Home screen opened");
            _currentIndex = 0;
            RefreshButtons();
            
            AnnounceStatus();
            AnnounceCurrentItem();
        }

        private void AnnounceStatus()
        {
            string week = "Week " + Calendar.currentWeek;
            string year = "Year " + Calendar.currentYear;
            string money = MoneyManager.playerBalance + " gold";
            
            string popStr = "";
            var playerTeam = DataManager.allTheTeams?.Find(x => x.PlayerTeam);
            if (playerTeam != null)
            {
                string popDesc = GeneralUtilities.PopularityIntToDescripton(playerTeam.Popularity);
                string repDesc = GeneralUtilities.ReputationIntToDescripton(playerTeam.Reputation);
                popStr = $". Popularity: {popDesc} ({playerTeam.Popularity}). Reputation: {repDesc} ({playerTeam.Reputation})";
            }

            ScreenReader.Say($"Home Screen. {week}, {year}. Balance: {money}{popStr}.");
        }

        private void RefreshButtons()
        {
            _mainButtons.Clear();
            
            var allButtons = Object.FindObjectsOfType<Button>();
            foreach (var btn in allButtons)
            {
                if (btn == null || !btn.gameObject.activeInHierarchy || !btn.interactable) continue;
                
                // Exclude small side buttons or debug tools
                string name = btn.name.ToLower();
                if (name.Contains("close")) continue;
                
                // Filter out debug tools that are active but not intended for players
                if (name.Contains("weekchange") || name.Contains("changeweek")) continue;

                // Scale check: common Unity trick to "hide" things is setting scale to 0
                if (btn.transform.localScale.sqrMagnitude < 0.01f) continue;

                _mainButtons.Add(btn);
            }

            var allInputs = Object.FindObjectsOfType<InputField>();
            foreach (var input in allInputs)
            {
                if (input == null || !input.gameObject.activeInHierarchy || !input.interactable) continue;
                if (input.name.ToLower().Contains("weekchange")) continue;
                if (input.transform.localScale.sqrMagnitude < 0.01f) continue;

                _mainButtons.Add(input);
            }

            _mainButtons.Sort((a, b) => {
                float ay = a.transform.position.y;
                float by = b.transform.position.y;
                if (Mathf.Abs(ay - by) > 10f) return by.CompareTo(ay);
                return a.transform.position.x.CompareTo(b.transform.position.x);
            });
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) Navigate(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) Navigate(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) Navigate(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) Navigate(1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ActivateCurrent();
            else if (Input.GetKeyDown(KeyCode.F2)) AnnounceStatus();
        }

        private void Navigate(int direction)
        {
            if (_mainButtons.Count == 0) return;
            _currentIndex = (_currentIndex + direction + _mainButtons.Count) % _mainButtons.Count;
            AnnounceCurrentItem();
        }

        private void ActivateCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _mainButtons.Count) return;
            var element = _mainButtons[_currentIndex];
            string label = GetButtonLabel(element);

            if (element is Button btn)
            {
                if (label == "Barracks")
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Barracks);
                }
                else if (label == "Market")
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Shop);
                }
                else if (label == "Finances")
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Finance);
                }
                else if (label == "Options")
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Options);
                }
                else if (label == "The League")
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.League);
                }
                else if (label == "League Table")
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Records);
                }
                else if (label.ToLower().Contains("help"))
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Help);
                }

                if (AccessStateManager.IsBattleDoneThisWeek && label == "Continue to Next Week")
                {
                    var lm = Object.FindObjectOfType<LevelManager>();
                    if (lm != null)
                    {
                        ScreenReader.Say("Continuing to league results.");
                        lm.LoadLevel("TeamResults");
                    }
                    else
                    {
                        var cal = Object.FindObjectOfType<Calendar>();
                        if (cal != null)
                        {
                            cal.ToggleProcessing();
                            ScreenReader.Say("Advancing to next week.");
                        }
                    }
                    return;
                }

                btn.onClick.Invoke();

                if (btn.name.ToLower().Contains("rename"))
                {
                    ScreenReader.Say("Rename activated. Navigate to the text input to type.");
                    RefreshButtons();
                }
                else if (btn.name.ToLower().Contains("choice"))
                {
                    // If we made a choice, the buttons usually disappear
                    MelonCoroutines.Start(DelayedRefresh());
                }
            }
            else if (element is InputField input)
            {
                input.ActivateInputField();
                ScreenReader.Say("Editing. Press Enter to finish.");
            }
        }

        private System.Collections.IEnumerator DelayedRefresh()
        {
            yield return new WaitForSeconds(0.2f);
            RefreshButtons();
            AnnounceCurrentItem();
        }

        private void AnnounceCurrentItem()
        {
            if (_mainButtons.Count == 0) return;
            var btn = _mainButtons[_currentIndex];
            string label = GetButtonLabel(btn);
            string type = GetTypeName(btn);

            // Special handling for Choice buttons to read the event text
            if (label.Contains("Choice"))
            {
                string choiceText = GetEventChoiceText(btn);
                ScreenReader.Say($"{label}, {type}, {_currentIndex + 1} of {_mainButtons.Count}. {choiceText}");
            }
            else
            {
                ScreenReader.Say($"{label}, {type}, {_currentIndex + 1} of {_mainButtons.Count}.");
            }
        }

        private string GetEventChoiceText(Selectable s)
        {
            // The choice button itself usually has the option text (e.g. "Bribe")
            // But we should also read the main event description if possible
            string btnText = "";
            var textComp = s.GetComponentInChildren<Text>();
            if (textComp != null) btnText = textComp.text;

            // Try to find the explanation text for this specific choice
            var tutorial = Object.FindObjectOfType<TutorialPopUps>();
            if (tutorial != null)
            {
                string exp = "";
                if (s.name.Contains("1")) exp = TutorialPopUps.eventChoiceExp1TextText;
                else if (s.name.Contains("2")) exp = TutorialPopUps.eventChoiceExp2TextText;

                if (!string.IsNullOrEmpty(exp)) return $"{btnText}. Impact: {exp}";
            }

            return btnText;
        }

        private string GetButtonLabel(Selectable s)
        {
            var text = s.GetComponentInChildren<Text>();
            string originalText = (text != null && !string.IsNullOrEmpty(text.text)) ? text.text : "";

            if (AccessStateManager.IsBattleDoneThisWeek && originalText.Contains("Start Next Battle"))
            {
                return "Continue to Next Week";
            }

            if (!string.IsNullOrEmpty(originalText)) return originalText;

            string name = s.name.ToLower();
            if (name.Contains("continue")) return "Continue to Next Week";
            if (name.Contains("barracks")) return "Barracks";
            if (name.Contains("market")) return "Market";
            if (name.Contains("finance")) return "Finances";
            if (name.Contains("league")) return "League Table";
            if (name.Contains("save")) return "Save Game";
            if (name.Contains("load")) return "Load Game";
            if (name.Contains("option")) return "Options";
            if (name.Contains("exit") || name.Contains("quit")) return "Quit to Main Menu";
            if (name.Contains("rename")) return "Rename Team";
            if (name.Contains("naminginput")) return "Team Name Input Field";
            if (name.Contains("choicebutton1")) return "Choice 1";
            if (name.Contains("choicebutton2")) return "Choice 2";

            return s.name;
        }

        private string GetTypeName(Selectable s)
        {
            if (s is InputField) return "Text Input";
            if (s is Button) return "Button";
            return "Element";
        }
    }
}
