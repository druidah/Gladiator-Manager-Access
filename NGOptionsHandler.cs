using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using DuloGames.UI;

namespace GladiatorManagerAccess
{
    public class NGOptionsHandler : IAccessibleHandler
    {
        private int _currentIndex = 0;
        private bool _wasOpen = false;
        private List<Selectable> _elements = new List<Selectable>();

        public string GetHelpText()
        {
            return "New Game Options: Use Up and Down arrows to select a setting. Use Left and Right arrows to change dropdown values. Press Enter to confirm and start your career.";
        }

        public static bool IsNGOptionsOpen()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "NGOptions";
        }

        public void Update()
        {
            bool isOpen = IsNGOptionsOpen();

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

            ProcessInput();
        }

        private void OnOpen()
        {
            DebugLogger.LogState("NG Options opened");
            _currentIndex = 0;
            _elements.Clear();
            
            RefreshElements();
            
            ScreenReader.Say(Loc.Get("scene_ngoptions"));
            AnnounceCurrentItem();
        }

        private void OnClose()
        {
            DebugLogger.LogState("NG Options closed");
        }

        private void RefreshElements()
        {
            _elements.Clear();
            
            // Find all selectables in the scene
            var allSelectables = Object.FindObjectsOfType<Selectable>();
            foreach (var s in allSelectables)
            {
                if (s == null || !s.gameObject.activeInHierarchy || !s.interactable) continue;
                
                // Exclude some things if needed
                _elements.Add(s);
            }

            // Sort by vertical position (top to bottom), then horizontal (left to right)
            _elements.Sort((a, b) => {
                float ay = a.transform.position.y;
                float by = b.transform.position.y;
                if (Mathf.Abs(ay - by) > 0.1f) return by.CompareTo(ay);
                return a.transform.position.x.CompareTo(b.transform.position.x);
            });
            
            DebugLogger.LogState($"Found {_elements.Count} interactive elements in NGOptions.");
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
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                AdjustCurrent(-1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                AdjustCurrent(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ActivateCurrent();
            }
        }

        private void Navigate(int direction)
        {
            if (_elements.Count == 0) return;

            _currentIndex += direction;
            if (_currentIndex < 0) _currentIndex = _elements.Count - 1;
            if (_currentIndex >= _elements.Count) _currentIndex = 0;

            AnnounceCurrentItem();
        }

        private void AdjustCurrent(int direction)
        {
            if (_currentIndex < 0 || _currentIndex >= _elements.Count) return;
            var element = _elements[_currentIndex];

            if (element is Dropdown dropdown)
            {
                int next = dropdown.value + direction;
                if (next < 0) next = dropdown.options.Count - 1;
                if (next >= dropdown.options.Count) next = 0;
                dropdown.value = next;
                AnnounceCurrentItem();
            }
            else if (element is UISelectField selectField)
            {
                int next = selectField.selectedOptionIndex + direction;
                if (next < 0) next = selectField.options.Count - 1;
                if (next >= selectField.options.Count) next = 0;
                selectField.SelectOptionByIndex(next);
                AnnounceCurrentItem();
            }
            else if (element is Slider slider)
            {
                slider.value += (slider.maxValue - slider.minValue) * 0.1f * direction;
                AnnounceCurrentItem();
            }
            else if (element is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                AnnounceCurrentItem();
            }
        }

        private void ActivateCurrent()
        {
            if (_currentIndex >= 0 && _currentIndex < _elements.Count)
            {
                var element = _elements[_currentIndex];
                if (element is Button btn)
                {
                    btn.onClick.Invoke();
                }
                else if (element is Toggle toggle)
                {
                    toggle.isOn = !toggle.isOn;
                    AnnounceCurrentItem();
                }
                else if (element is InputField input)
                {
                    input.ActivateInputField();
                    ScreenReader.Say("Editing " + GetLabel(element));
                }
            }
        }

        private void AnnounceCurrentItem()
        {
            if (_elements.Count == 0) return;

            var element = _elements[_currentIndex];
            string label = GetLabel(element);
            string value = GetValue(element);
            string type = GetTypeName(element);

            string valueStr = string.IsNullOrEmpty(value) ? "" : $", {value}";
            string announcement = $"{label}, {type}{valueStr}, {_currentIndex + 1} of {_elements.Count}.";
            ScreenReader.Say(announcement);
        }

        private string GetLabel(Selectable s)
        {
            // 1. If it's a Button, its own internal text is the absolute best label
            if (s is Button)
            {
                var t = s.GetComponentInChildren<Text>();
                if (t != null && !string.IsNullOrEmpty(t.text) && t.text.Length > 1) 
                    return t.text;
            }

            // 2. Check for specific hardcoded mappings (highest priority)
            string name = s.name.ToLower();
            string placeholder = GetLabelFromPlaceholder(s);
            
            if (name.Contains("teamname") || placeholder.Contains("leave blank")) return "Team Name";
            if (name.Contains("charname") || name.Contains("charactername")) return "Character Name";
            if (name.Contains("charsurname") || name.Contains("charactersurname")) return "Character Surname";
            
            if (name.Contains("teamtheme")) return "Team Theme";
            if (name.Contains("gladtheme")) return "Gladiator Theme";
            if (name.Contains("generatorrng") || name.Contains("fgrng")) return "Generator RNG";
            if (name.Contains("battlerng") || name.Contains("battledrop")) return "Battle RNG";
            if (name.Contains("economicdiff") || name.Contains("econdiff")) return "Economic Difficulty";
            if (name.Contains("battlediff")) return "Battle Difficulty";
            if (name.Contains("classdrop")) return "Class";
            if (name.Contains("attributedrop")) return "Starting Attribute";
            if (name.Contains("back")) return "Back";
            if (name.Contains("quit") || name.Contains("exit")) return "Quit";
            if (name.Contains("start") || name.Contains("progress")) return "Start New Game";
            if (name.Contains("gender")) return "Gender";
            if (name.Contains("potential")) return "Potentials Visibility";
            if (name.Contains("font")) return "Font Size";

            // 3. For InputFields, the Placeholder text is a good hint if it's SHORT
            if (s is InputField input && !string.IsNullOrEmpty(placeholder))
            {
                if (placeholder.Length < 20) return placeholder;
            }

            // 4. Look for the sibling IMMEDIATELY before this one (standard label position)
            if (s.transform.parent != null)
            {
                int myIndex = s.transform.GetSiblingIndex();
                if (myIndex > 0)
                {
                    var prevSibling = s.transform.parent.GetChild(myIndex - 1);
                    var t = prevSibling.GetComponent<Text>();
                    if (t == null) t = prevSibling.GetComponentInChildren<Text>();
                    
                    if (t != null && !string.IsNullOrEmpty(t.text))
                    {
                        if (t.text != GetValue(s) && t.text.Length > 1 && prevSibling.GetComponent<Selectable>() == null)
                        {
                            return t.text;
                        }
                    }
                }
                
                // 5. Try parent's name if it looks like a container for this specific setting
                string parentName = s.transform.parent.name.ToLower();
                if (parentName.Contains("name") || parentName.Contains("theme") || parentName.Contains("difficulty"))
                {
                    return s.transform.parent.name.Replace("_", " ");
                }
            }
            
            // 6. Fallback to cleaned up object name
            string cleanName = s.name.Replace("Dropdown", "").Replace("Input", "").Replace("Button", "").Replace(" (1)", "").Replace("_", " ");
            if (cleanName.ToLower() == "field") cleanName = "Text"; // Avoid just "Field"
            
            string result = "";
            for (int i = 0; i < cleanName.Length; i++)
            {
                if (i > 0 && char.IsUpper(cleanName[i]) && !char.IsUpper(cleanName[i-1])) result += " ";
                result += cleanName[i];
            }
            return result.Trim();
        }

        private string GetValue(Selectable s)
        {
            if (s is Dropdown d) return d.options[d.value].text;
            if (s is UISelectField sf) return sf.value;
            if (s is Toggle t) return t.isOn ? "On" : "Off";
            if (s is Slider sl) return sl.value.ToString("F1");
            if (s is InputField i) return i.text;
            return "";
        }

        private string GetLabelFromPlaceholder(Selectable s)
        {
            if (s is InputField input && input.placeholder != null)
            {
                var placeholderText = input.placeholder.GetComponent<Text>();
                if (placeholderText != null && !string.IsNullOrEmpty(placeholderText.text))
                    return placeholderText.text.ToLower();
            }
            return "";
        }

        private string GetTypeName(Selectable s)
        {
            if (s is Dropdown || s is UISelectField) return "Dropdown";
            if (s is Toggle) return "Toggle";
            if (s is Slider) return "Slider";
            if (s is InputField) return "Text input";
            if (s is Button) return "Button";
            return "Element";
        }
    }
}
