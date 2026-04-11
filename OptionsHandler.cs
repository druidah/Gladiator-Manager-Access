using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace GladiatorManagerAccess
{
    public class OptionsHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _currentIndex = 0;
        private List<Selectable> _elements = new List<Selectable>();

        public string GetHelpText()
        {
            return "Options: Use Up and Down arrows to select a setting. Use Left and Right arrows to adjust volume sliders or change dropdown options. Press Enter or Space to toggle switches. Press Escape to return to the Home screen.";
        }

        public void Update()
        {
            bool isOpen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Options";

            if (isOpen && !_wasOpen)
            {
                if (AccessStateManager.TryEnter(AccessStateManager.State.Options))
                {
                    OnOpen();
                    _wasOpen = true;
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.Options);
                _wasOpen = false;
            }

            if (!isOpen || !AccessStateManager.IsIn(AccessStateManager.State.Options)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            _currentIndex = 0;
            
            // SAFELY ensure all controllers are set up correctly before we read them
            if (DataManager.allTheSaveData != null && DataManager.allTheSaveData.Count > 0)
            {
                var saveData = DataManager.allTheSaveData.Find(x => x.SaveID == 1) ?? DataManager.allTheSaveData[0];
                
                var controllers = Object.FindObjectsOfType<OptionsController>();
                foreach (var ctrl in controllers)
                {
                    try { ctrl.SetupDropDowns(); } catch { }
                }

                // Force sync for components the game's SetupDropDowns misses
                var cal = Object.FindObjectOfType<Calendar>();
                if (cal != null && cal.randomNumberToggle != null)
                    cal.randomNumberToggle.isOn = saveData.SpareBool1;

                var gen = Object.FindObjectOfType<GladiatorGenerator>();
                if (gen != null && gen.mortalityToggle != null)
                {
                    var player = DataManager.allThePGladiators.Find(x => x.ID == 1);
                    if (player != null)
                        gen.mortalityToggle.isOn = (player.HitPoints >= 2);
                }
            }

            RefreshElements();
            AnnounceCurrentItem();
        }

        private void RefreshElements()
        {
            _elements.Clear();
            var all = Object.FindObjectsOfType<Selectable>();
            foreach (var s in all)
            {
                if (s == null || !s.gameObject.activeInHierarchy || !s.interactable) continue;
                if (s.transform.localScale.sqrMagnitude < 0.01f) continue;
                _elements.Add(s);
            }

            _elements.Sort((a, b) => {
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
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) AdjustValue(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) AdjustValue(1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ActivateCurrent();
            else if (Input.GetKeyDown(KeyCode.Escape)) ExitToHome();
        }

        private void Navigate(int direction)
        {
            if (_elements.Count == 0) return;
            _currentIndex = (_currentIndex + direction + _elements.Count) % _elements.Count;
            AnnounceCurrentItem();
        }

        private void AdjustValue(int direction)
        {
            if (_currentIndex < 0 || _currentIndex >= _elements.Count) return;
            var element = _elements[_currentIndex];

            if (element is Slider slider)
            {
                float newVal = Mathf.Clamp(slider.value + (direction * 0.1f), slider.minValue, slider.maxValue);
                slider.value = newVal;
                
                // Explicitly call volume set method
                var controllers = Object.FindObjectsOfType<OptionsController>();
                foreach (var ctrl in controllers)
                {
                    try
                    {
                        bool isMusic = element.name.ToLower().Contains("music") || slider == ctrl.musicSlider;
                        ctrl.SetNewVolume(isMusic);
                    }
                    catch { /* Ignore failures on specific controllers */ }
                }

                // Report label and value
                string label = GetLabel(element);
                string value = GetValue(element);
                ScreenReader.Say($"{label}, {value}");
                
                SaveIfNecessary();
            }
            else if (element is Dropdown dropdown)
            {
                dropdown.value = (dropdown.value + direction + dropdown.options.Count) % dropdown.options.Count;
                
                // Trigger game logic for dropdowns - SAFELY
                var controllers = Object.FindObjectsOfType<OptionsController>();
                foreach (var ctrl in controllers)
                {
                    try
                    {
                        if (dropdown == ctrl.fontSizeDropdown) ctrl.SetNewFontSize();
                        else if (dropdown == ctrl.potentialsDrop) ctrl.SetPotentialVisibility();
                    }
                    catch { /* Ignore game logic failures */ }
                }

                ScreenReader.Say(dropdown.options[dropdown.value].text);
                SaveIfNecessary();
            }
        }

        private void ActivateCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _elements.Count) return;
            var element = _elements[_currentIndex];

            if (element is Button btn)
            {
                SaveIfNecessary();
                btn.onClick.Invoke();
                SaveIfNecessary();
            }
            else if (element is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;

                // 1. Explicitly call game methods - SAFELY
                var controllers = Object.FindObjectsOfType<OptionsController>();
                foreach (var ctrl in controllers)
                {
                    try
                    {
                        if (toggle == ctrl.muteToggle) ctrl.MuteCrowd();
                        else if (toggle == ctrl.musicToggle) ctrl.MusicInBattle();
                        else if (toggle == ctrl.detailHoldToggle) ctrl.HoldDetail();
                    }
                    catch { }
                }

                var cal = Object.FindObjectOfType<Calendar>();
                if (cal != null && toggle == cal.randomNumberToggle) 
                {
                    try { cal.ToggleRandomFightNumbers(); } catch { }
                }

                var gen = Object.FindObjectOfType<GladiatorGenerator>();
                if (gen != null && toggle == gen.mortalityToggle)
                {
                    try { gen.ToggleMortalityStatus(); } catch { }
                }

                // 2. MANUALLY Update Memory List to be absolutely sure
                if (DataManager.allTheSaveData != null && DataManager.allTheSaveData.Count > 0)
                {
                    var saveData = DataManager.allTheSaveData.Find(x => x.SaveID == 1) ?? DataManager.allTheSaveData[0];
                    if (saveData != null)
                    {
                        var ctrl = Object.FindObjectOfType<OptionsController>();
                        if (ctrl != null)
                        {
                            if (toggle == ctrl.muteToggle) saveData.SpareBool2 = toggle.isOn;
                            else if (toggle == ctrl.musicToggle) saveData.SpareInt4 = toggle.isOn ? 1 : 0;
                            else if (toggle == ctrl.detailHoldToggle) saveData.SpareString4 = toggle.isOn ? "true" : "false";
                        }
                        
                        if (cal != null && toggle == cal.randomNumberToggle)
                        {
                            saveData.SpareBool1 = toggle.isOn;
                        }

                        if (gen != null && toggle == gen.mortalityToggle)
                        {
                            var player = DataManager.allThePGladiators.Find(x => x.ID == 1);
                            if (player != null) player.HitPoints = toggle.isOn ? 2 : 1;
                        }
                    }
                }

                ScreenReader.Say(toggle.isOn ? "On" : "Off");
                SaveIfNecessary();
            }
        }

        private void SaveIfNecessary()
        {
            if (DataManager.allTheSaveData == null || DataManager.allTheSaveData.Count == 0) return;
            
            var saveData = DataManager.allTheSaveData.Find(x => x.SaveID == 1) ?? DataManager.allTheSaveData[0];
            var db = DataManager.dbManager;
            var dm = Object.FindObjectOfType<DataManager>();

            if (saveData == null || db == null) return;

            try
            {
                // DIRECT SQL UPDATE for ALL Toggles
                db.Execute("UPDATE SaveData SET SpareBool1 = ?, SpareBool2 = ?, SpareInt4 = ?, SpareString4 = ? WHERE SaveID = ?",
                    saveData.SpareBool1 ? 1 : 0,
                    saveData.SpareBool2 ? 1 : 0,
                    saveData.SpareInt4,
                    saveData.SpareString4,
                    saveData.SaveID);

                // Mortality save
                var player = DataManager.allThePGladiators.Find(x => x.ID == 1);
                if (player != null)
                {
                    db.Execute("UPDATE PGladiators SET HitPoints = ? WHERE ID = 1", player.HitPoints);
                }
                
                // Also call standard save
                if (dm != null) dm.SaveSaveData();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"Failed to save options: {ex.Message}");
            }
        }

        private void AnnounceCurrentItem()
        {
            if (_elements.Count == 0) return;
            var element = _elements[_currentIndex];
            string label = GetLabel(element);
            string type = GetTypeName(element);
            string value = GetValue(element);

            string valueStr = string.IsNullOrEmpty(value) ? "" : $", {value}";
            ScreenReader.Say($"{label}, {type}{valueStr}, {_currentIndex + 1} of {_elements.Count}.");
        }

        private string GetLabel(Selectable s)
        {
            // 1. Explicit mapping via component fields (Highest precision)
            var ctrl = Object.FindObjectOfType<OptionsController>();
            if (ctrl != null)
            {
                if (s == ctrl.musicSlider) return "Music Volume";
                if (s == ctrl.effectsSlider) return "Effects Volume";
                if (s == ctrl.fontSizeDropdown) return "Font Size";
                if (s == ctrl.themeDown) return "Gladiator Name Theme";
                if (s == ctrl.FGRNGDropDown) return "Fighter Generation RNG";
                if (s == ctrl.BattleDropDown) return "Battle RNG";
                if (s == ctrl.econDiffDropDown) return "Economy Difficulty";
                if (s == ctrl.battleDiffDropDown) return "Battle Difficulty";
                if (s == ctrl.genderDrop) return "Gladiator Gender";
                if (s == ctrl.potentialsDrop) return "Potentials Visibility";
                if (s == ctrl.muteToggle) return "Mute crowd";
                if (s == ctrl.musicToggle) return "Music in battle";
                if (s == ctrl.detailHoldToggle) return "Hold detail toggle";
            }

            var cal = Object.FindObjectOfType<Calendar>();
            if (cal != null && s == cal.randomNumberToggle) return "Random number of fighters in each fixture";

            var gen = Object.FindObjectOfType<GladiatorGenerator>();
            if (gen != null && s == gen.mortalityToggle) return "Mortal player character";

            if (s is Button && s == _elements.LastOrDefault())
            {
                return "Back to game";
            }

            // 2. Manual Overrides for remaining toggles
            if (s is Toggle)
            {
                int toggleIndex = _elements.Where(e => e is Toggle).ToList().IndexOf(s);
                if (toggleIndex == 0) return "Random number of fighters in each fixture";
                if (toggleIndex == 1) return "Mortal player character";
            }

            // 3. Button check: usually contains its own label
            if (s is Button)
            {
                var t = s.GetComponentInChildren<Text>();
                if (t != null && !string.IsNullOrEmpty(t.text) && t.text.Length > 2) return t.text;
            }

            // 4. Name-based mapping
            string n = s.name.ToLower();
            if (n.Contains("music") && n.Contains("toggle")) return "Music in battle";
            if (n.Contains("mute")) return "Mute crowd";
            if (n.Contains("hold") || n.Contains("detail")) return "Hold detail toggle";
            if (n.Contains("font")) return "Font Size";
            if (n.Contains("theme")) return "Name Theme";
            if (n.Contains("gender")) return "Gender";
            if (n.Contains("econ")) return "Economy Difficulty";
            if (n.Contains("battle") && n.Contains("diff")) return "Battle Difficulty";

            // 5. Sibling label search
            var parent = s.transform.parent;
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    if (child.gameObject == s.gameObject) continue;
                    var t = child.GetComponent<Text>();
                    if (t != null && !string.IsNullOrEmpty(t.text))
                    {
                        if (child.GetComponentInParent<Selectable>() != null && child.GetComponentInParent<Selectable>().gameObject != parent.gameObject) continue;
                        string val = t.text.ToLower();
                        if (val == "on" || val == "off" || val.Contains("%") || val.Contains("/") || val.Length < 3) continue;
                        if (val.Contains("totally random") || val.Contains("ancient") || val.Contains("modern") || val.Contains("standard") || val.Contains("large")) continue;
                        return t.text;
                    }
                }
            }
            
            return s.name;
        }

        private string GetTypeName(Selectable s)
        {
            if (s is Slider) return "Slider";
            if (s is Dropdown) return "Dropdown";
            if (s is Toggle) return "Toggle";
            if (s is Button) return "Button";
            return "Element";
        }

        private string GetValue(Selectable s)
        {
            if (s is Slider slider) return $"{Mathf.RoundToInt(slider.normalizedValue * 100)} percent";
            if (s is Dropdown dropdown) return dropdown.options[dropdown.value].text;
            if (s is Toggle toggle) return toggle.isOn ? "On" : "Off";
            return "";
        }

        private void ExitToHome()
        {
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm != null) lm.LoadLevel("Home");
            else UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
        }
    }
}
