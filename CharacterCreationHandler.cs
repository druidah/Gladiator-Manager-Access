using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using DuloGames.UI;

namespace GladiatorManagerAccess
{
    public class CharacterCreationHandler : IAccessibleHandler
    {
        private int _currentIndex = 0;
        private bool _wasOpen = false;
        private List<Selectable> _elements = new List<Selectable>();

        public string GetHelpText()
        {
            return "Character Creation: Use Up and Down arrows to navigate through the 31 available elements (Appearance, Name, Class, etc.). For Class and Mortality, the mod will automatically read their descriptions. Use Left and Right arrows to cycle options for appearance and dropdowns. Press Enter to confirm and start.";
        }

        // Exact sequence provided by the user (31 items)
        private static readonly string[] _hardcodedLabels = new string[]
        {
            "Previous Gender", "Next Gender", "First Name", "Previous Skin", "Next Skin",
            "Randomize Name", "Previous Clothing", "Next Clothing", "Surname", "Previous Hair",
            "Next Hair", "Previous Mouth", "Next Mouth", "Class", "Randomize Class",
            "Previous Eyes", "Next Eyes", "Previous Eyebrows", "Next Eyebrows", "Previous Beard",
            "Next Beard", "Previous Nose", "Next Nose", "Attribute Focus", "Randomize Attribute Focus",
            "Previous Marks", "Next Marks", "Randomize Appearance", "Previous Moustache",
            "Next Moustache", "Character Status (Mortality)"
        };

        public bool IsOpen()
        {
            var gen = Object.FindObjectOfType<GladiatorGenerator>();
            return gen != null && gen.characterCreationScreen;
        }

        public void Update()
        {
            bool isOpen = IsOpen();

            if (isOpen && !_wasOpen)
            {
                if (AccessStateManager.TryEnter(AccessStateManager.State.CharacterCreation))
                {
                    OnOpen();
                    _wasOpen = true;
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.CharacterCreation);
                OnClose();
                _wasOpen = false;
            }

            if (!AccessStateManager.IsIn(AccessStateManager.State.CharacterCreation)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            DebugLogger.LogState("Character Creation handler activated");
            _currentIndex = 0;
            RefreshElements();
            ScreenReader.Say("Character Creation Screen");
            AnnounceCurrentItem();
        }

        private void OnClose() => DebugLogger.LogState("Character Creation handler deactivated");

        private void RefreshElements()
        {
            _elements.Clear();
            var all = Object.FindObjectsOfType<Selectable>();
            var gen = Object.FindObjectOfType<GladiatorGenerator>();
            
            foreach (var s in all)
            {
                if (s == null) continue;
                
                // Aggressively exclude the class and mortality explanation texts
                if (gen != null)
                {
                    if (gen.classExplanationText != null && (s.gameObject == gen.classExplanationText.gameObject || s.transform.IsChildOf(gen.classExplanationText.transform)))
                        continue;
                    if (gen.mortalityExplanationText != null && (s.gameObject == gen.mortalityExplanationText.gameObject || s.transform.IsChildOf(gen.mortalityExplanationText.transform)))
                        continue;
                }

                // Exclude the class dropdown options
                if (s.name == "Item" && s.transform.parent.name == "Content") continue;

                // Be more permissive with the dropdowns if they are "obscured"
                string lname = s.name.ToLower();
                bool isSpecial = lname.Contains("classdrop") || lname.Contains("mortality") || lname.Contains("attributedrop");
                if (!isSpecial && (!s.gameObject.activeInHierarchy || !s.interactable)) continue;
                
                _elements.Add(s);
            }

            // Force add the dropdowns if we missed them
            if (gen != null)
            {
                if (gen.classDropdown != null && !_elements.Contains(gen.classDropdown)) _elements.Add(gen.classDropdown);
                if (gen.mortalityDrop != null && !_elements.Contains(gen.mortalityDrop)) _elements.Add(gen.mortalityDrop);
                if (gen.attributeDropdown != null && !_elements.Contains(gen.attributeDropdown)) _elements.Add(gen.attributeDropdown);
            }

            // Sort with row grouping
            _elements.Sort((a, b) => {
                float ay = a.transform.position.y;
                float by = b.transform.position.y;
                if (Mathf.Abs(ay - by) > 20f) return by.CompareTo(ay);
                return a.transform.position.x.CompareTo(b.transform.position.x);
            });
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) Navigate(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) Navigate(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) AdjustCurrent(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) AdjustCurrent(1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ActivateCurrent();
        }

        private void Navigate(int direction)
        {
            if (_elements.Count == 0) return;
            _currentIndex = (_currentIndex + direction + _elements.Count) % _elements.Count;
            AnnounceCurrentItem();
        }

        private void AdjustCurrent(int direction)
        {
            if (_currentIndex < 0 || _currentIndex >= _elements.Count) return;
            var element = _elements[_currentIndex];

            if (element is Dropdown dropdown)
            {
                dropdown.value = (dropdown.value + direction + dropdown.options.Count) % dropdown.options.Count;
                AnnounceCurrentItem();
            }
            else if (element is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                AnnounceCurrentItem();
            }
            else
            {
                Navigate(direction);
            }
        }

        private void ActivateCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _elements.Count) return;
            var element = _elements[_currentIndex];
            
            if (element is Button btn)
            {
                btn.onClick.Invoke();
                
                string label = GetLabel(element);
                string value = GetValue(element);
                
                if (IsCosmeticButton(label))
                {
                    // Immediately announce the new value for appearance buttons
                    ScreenReader.Say(value);
                }
                else
                {
                    AnnounceCurrentItem();
                }
            }
            else if (element is Dropdown dropdown)
            {
                dropdown.Show(); 
            }
            else if (element is Toggle toggle) { toggle.isOn = !toggle.isOn; AnnounceCurrentItem(); }
            else if (element is InputField input)
            {
                input.ActivateInputField();
                ScreenReader.Say("Editing " + GetLabel(element));
            }
        }

        private void AnnounceCurrentItem()
        {
            if (_elements.Count == 0) return;
            var element = _elements[_currentIndex];
            string label = GetLabel(element);
            string value = GetValue(element);
            string type = GetTypeName(element);

            // Standard Format: Item name, Type
            string announcement = $"{label}, {type}";

            // Only show value during navigation if it's NOT a cosmetic cycle button
            // (We announce the value when they press Enter to change it instead)
            if (!IsCosmeticButton(label) && !string.IsNullOrEmpty(value))
            {
                announcement += $", {value}";
            }

            // Append descriptions for Class and Status (only when navigating TO them)
            if (label == "Class")
            {
                string desc = GetClassDescription(value);
                if (!string.IsNullOrEmpty(desc)) announcement += ". " + desc;
            }
            else if (label.Contains("Status"))
            {
                string desc = GetMortalityDescription(value);
                if (!string.IsNullOrEmpty(desc)) announcement += ". " + desc;
            }

            // Add index info back
            announcement += $", {_currentIndex + 1} of {_elements.Count}.";

            ScreenReader.Say(announcement);
        }

        private bool IsCosmeticButton(string label)
        {
            string l = label.ToLower();
            return l.Contains("gender") || l.Contains("skin") || l.Contains("clothing") || 
                   l.Contains("hair") || l.Contains("mouth") || l.Contains("eyes") || 
                   l.Contains("eyebrows") || l.Contains("beard") || l.Contains("nose") || 
                   l.Contains("marks") || l.Contains("moustache");
        }

        private string GetLabel(Selectable s)
        {
            int index = _elements.IndexOf(s);
            if (index >= 0 && index < _hardcodedLabels.Length)
            {
                return _hardcodedLabels[index];
            }

            string name = s.name.ToLower();
            if (name.Contains("confirm") || name.Contains("start") || name.Contains("progress") || name.Contains("continue")) return "Confirm and Start";
            if (name.Contains("back") || name.Contains("cancel")) return "Back";
            
            return "Option " + (index + 1);
        }

        private string GetClassDescription(string className)
        {
            return Loc.GetClassDescription(className);
        }

        private string GetMortalityDescription(string status)
        {
            if (status == "Immortal")
                return "(Your starting fighter cannot be killed or permanently maimed, unlike all others. They start with extremely poor skills but have the potential to become one of the best fighters in the game with training and experience.)";
            else if (status == "Talented but Mortal")
                return "(Your starting fighter is mortal and can be killed or permanently maimed, like any other fighter. They start with extremely poor skills but have the potential to become one of the best fighters in the game with training and experience.)";
            else if (status == "Average but Mortal" || status.Contains("Average") || status.Contains("Ordinary") || status.Contains("Mortal"))
                return "(Your starting fighter is mortal and can be killed or permanently maimed, like any other fighter. They are an ordinary fighter generated according to the generation RNG you selected in the last screen. They are likely to be a very bad fighter with little potential. They do not benefit from the focus attribute selected on this screen.)";
            
            return "";
        }

        private string GetValue(Selectable s)
        {
            if (s is Dropdown d) return d.options[d.value].text;
            if (s is Toggle t) return t.isOn ? "On" : "Off";
            if (s is InputField i) return i.text;
            
            string label = GetLabel(s);
            if (label.Contains("Gender"))
            {
                return Portraitor.tempPortrait?.Gender ?? "Unknown";
            }

            var portraitor = Object.FindObjectOfType<Portraitor>();
            if (portraitor != null && portraitor.IDLabels != null)
            {
                int idIndex = -1;
                if (label.Contains("Skin")) idIndex = 1;
                else if (label.Contains("Clothing")) idIndex = 2;
                else if (label.Contains("Hair")) idIndex = 3;
                else if (label.Contains("Mouth")) idIndex = 4;
                else if (label.Contains("Nose")) idIndex = 5;
                else if (label.Contains("Eyes")) idIndex = 6;
                else if (label.Contains("Eyebrows")) idIndex = 7;
                else if (label.Contains("Marks")) idIndex = 8;
                else if (label.Contains("Beard")) idIndex = 9;
                else if (label.Contains("Moustache")) idIndex = 10;

                if (idIndex != -1 && idIndex < portraitor.IDLabels.Length)
                {
                    var valLabel = portraitor.IDLabels[idIndex];
                    if (valLabel != null && int.TryParse(valLabel.text, out int id))
                    {
                        bool isFemale = Portraitor.tempPortrait?.Gender == "Female";
                        return AppearanceUtilities.GetFeatureDescription(label, id, isFemale);
                    }
                }
            }
            
            return "";
        }

        private string GetTypeName(Selectable s)
        {
            if (s is Dropdown) return "Dropdown";
            if (s is Toggle) return "Toggle";
            if (s is InputField) return "Text input";
            if (s is Button) return "Button";
            return "Element";
        }
    }
}
