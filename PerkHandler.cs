using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using MelonLoader;
using DuloGames.UI;

namespace GladiatorManagerAccess
{
    public class PerkHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _currentIndex = 0;
        private List<GameObject> _elements = new List<GameObject>();

        public string GetHelpText()
        {
            return "Level Up: Choose a perk or attribute increase for your gladiator. Use Up and Down arrows to select an option. Press Enter to confirm. This choice is permanent.";
        }

        public bool IsOpen()
        {
            var pop = Object.FindObjectOfType<Populator>();
            return pop != null && pop.perkScreen != null && pop.perkScreen.IsOpen;
        }

        public void Update()
        {
            bool isOpen = IsOpen();

            if (isOpen && !_wasOpen)
            {
                if (AccessStateManager.TryEnter(AccessStateManager.State.PerkScreen)) // Using Inventory as proxy for sub-window
                {
                    OnOpen();
                    _wasOpen = true;
                    return; // Prevent processing input in the same frame state was entered
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.PerkScreen);
                OnClose();
                _wasOpen = false;
                return; // Prevent processing input in the same frame state was exited
            }

            if (!AccessStateManager.IsIn(AccessStateManager.State.PerkScreen)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            DebugLogger.LogState("Perk screen opened");
            _currentIndex = 0;
            RefreshElements();
            ScreenReader.Say("Gladiator Details and Perks.");
            AnnounceCurrentItem();
        }

        private void OnClose() => DebugLogger.LogState("Perk screen closed");

        private void RefreshElements()
        {
            _elements.Clear();
            var pop = Object.FindObjectOfType<Populator>();
            if (pop == null) return;

            List<GameObject> all = new List<GameObject>();
            
            // 1. Get everything from the perk window
            if (pop.perkScreen != null)
            {
                var selectables = pop.perkScreen.GetComponentsInChildren<Selectable>(true);
                var slots = pop.perkScreen.GetComponentsInChildren<UISlotBase>(true);
                foreach (var s in selectables) if (s != null && !all.Contains(s.gameObject)) all.Add(s.gameObject);
                foreach (var s in slots) if (s != null && !all.Contains(s.gameObject)) all.Add(s.gameObject);
            }

            // 2. Search for any buttons that might be back/exit buttons if not found in the window (fallback)
            if (all.Count < 2)
            {
                var allButtons = Object.FindObjectsOfType<Button>();
                foreach (var b in allButtons)
                {
                    string n = b.name.ToLower();
                    if (n.Contains("perk") || n.Contains("back") || n.Contains("close"))
                    {
                        if (!all.Contains(b.gameObject)) all.Add(b.gameObject);
                    }
                }
            }

            foreach (var go in all)
            {
                if (go == null) continue;
                
                // Must be active in hierarchy
                if (!go.activeInHierarchy) continue;

                // Scale check (0 means hidden/locked in this game)
                if (go.transform.localScale.sqrMagnitude < 0.01f) continue;
                
                // For selectables, check interactable
                var s = go.GetComponent<Selectable>();
                if (s != null && !s.interactable) continue;
                
                // Avoid duplicates and add
                if (!_elements.Contains(go)) _elements.Add(go);
            }

            _elements.Sort((a, b) => {
                float ay = a.transform.position.y;
                float by = b.transform.position.y;
                if (Mathf.Abs(ay - by) > 15f) return by.CompareTo(ay);
                return a.transform.position.x.CompareTo(b.transform.position.x);
            });

            if (Main.DebugMode)
            {
                MelonLogger.Msg($"PerkHandler: Found {_elements.Count} visible unique elements.");
                foreach (var el in _elements) MelonLogger.Msg($"  - {el.name} at {el.transform.position}");
            }
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) Navigate(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) Navigate(1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) Navigate(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) Navigate(1);
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ActivateCurrent();
            else if (Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void Navigate(int direction)
        {
            if (_elements.Count == 0) return;
            _currentIndex = (_currentIndex + direction + _elements.Count) % _elements.Count;
            AnnounceCurrentItem();
        }

        private void ActivateCurrent()
        {
            if (_currentIndex < 0 || _currentIndex >= _elements.Count) return;
            var go = _elements[_currentIndex];
            
            if (Main.DebugMode) MelonLogger.Msg($"Activating perk element: {go.name}");

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                string name = btn.name.ToLower();
                btn.onClick.Invoke();

                if (name.Contains("close") || name.Contains("back") || name.Contains("exit"))
                {
                    ScreenReader.Say("Back to Barracks.");
                }
                else
                {
                    MelonCoroutines.Start(DelayedRefresh());
                }
                return;
            }

            var slot = go.GetComponent<UISlotBase>();
            if (slot != null)
            {
                var talent = slot as UITalentSlot;
                if (talent != null)
                {
                    // CRITICAL: The game uses this static ID inside OnPointerClick
                    UITalentSlot.thisTalentSlotID = GetSlotId(talent);
                    if (Main.DebugMode) MelonLogger.Msg($"Set UITalentSlot.thisTalentSlotID to {UITalentSlot.thisTalentSlotID}");
                }

                slot.OnPointerClick(new PointerEventData(EventSystem.current));
                
                // Refresh to catch changes (like points decreasing)
                MelonCoroutines.Start(DelayedRefresh());
            }
        }

        private System.Collections.IEnumerator DelayedRefresh()
        {
            yield return new WaitForSeconds(0.2f);
            RefreshElements();
            AnnounceCurrentItem();
        }

        private System.Collections.IEnumerator DelayedAnnounce()
        {
            yield return new WaitForSeconds(0.1f);
            AnnounceCurrentItem();
        }

        private void Close()
        {
            var pop = Object.FindObjectOfType<Populator>();
            
            // Find the back button and click it, or hide window as fallback
            bool foundButton = false;
            foreach (var go in _elements)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null && (go.name.ToLower().Contains("back") || go.name.ToLower().Contains("close")))
                {
                    btn.onClick.Invoke();
                    foundButton = true;
                    break;
                }
            }

            if (!foundButton && pop != null && pop.perkScreen != null)
            {
                pop.perkScreen.Hide();
            }

            ScreenReader.Say("Back to Barracks.");
        }

        private void AnnounceCurrentItem()
        {
            if (_elements.Count == 0) 
            {
                ScreenReader.Say("No elements found on level up screen.");
                return;
            }
            var go = _elements[_currentIndex];
            string label = GetLabel(go);
            
            ScreenReader.Say($"{label}, {GetTypeName(go)}, {_currentIndex + 1} of {_elements.Count}.");
        }

        private string GetLabel(GameObject go)
        {
            var talent = go.GetComponent<UITalentSlot>();
            if (talent != null)
            {
                var pop = Object.FindObjectOfType<Populator>();
                int gladId = -1;
                
                if (pop != null)
                {
                    int displayAdjustment = Populator.bottomHalfDisplay ? 12 : 0;
                    int idx = Populator.gladiatorDisplayID + displayAdjustment;
                    if (idx >= 0 && idx < Populator.rearrangeGrid.Length)
                    {
                        gladId = Populator.rearrangeGrid[idx];
                    }
                }

                if (gladId != -1)
                {
                    UITalentSlot.thisGladID = gladId;
                    UISpellSlot.thisGladID = gladId;
                }

                UITalentSlot.thisTalentSlotID = GetSlotId(talent);
                
                string title = UISpellSlot.PerkTitle(UITalentSlot.thisTalentSlotID);
                string desc = UISpellSlot.PerkDescription(title, UITalentSlot.thisTalentSlotID);
                
                if (string.IsNullOrEmpty(title) || title == "None" || title == "Class Advantage")
                {
                    var g = DataManager.allThePGladiators.Find(x => x.ID == gladId);
                    if (g != null && (UITalentSlot.thisTalentSlotID == 0 || UITalentSlot.thisTalentSlotID == 1))
                    {
                        return $"{g.Class}. {desc}";
                    }
                    
                    return $"Perk Slot {UITalentSlot.thisTalentSlotID}";
                }
                
                return $"{title}. {desc}";
            }

            var text = go.GetComponentInChildren<Text>();
            if (text != null && !string.IsNullOrEmpty(text.text)) return text.text;

            string name = go.name.ToLower();
            if (name.Contains("close") || name.Contains("back") || name.Contains("exit")) return "Back to Barracks";
            
            return go.name;
        }

        private int GetSlotId(UITalentSlot talent)
        {
            string tag = talent.gameObject.tag;
            if (tag == "eOne") return 0;
            if (tag == "eTwo") return 1;
            if (tag == "eThree") return 2;
            if (tag == "eFour") return 3;
            if (tag == "eFive") return 4;
            if (tag == "eSix") return 5;
            if (tag == "pOne") return 6;
            if (tag == "pTwo") return 7;
            if (tag == "pThree") return 8;
            if (tag == "pFour") return 9;
            if (tag == "pFive") return 10;
            if (tag == "pSix") return 11;
            if (tag == "egOne") return 12;
            if (tag == "egTwo") return 13;
            if (tag == "egThree") return 14;
            if (tag == "egFour") return 15;
            if (tag == "pgOne") return 16;
            if (tag == "pgTwo") return 17;
            if (tag == "pgThree") return 18;
            if (tag == "pgFour") return 19;
            if (tag == "InHand") return 20;
            if (tag == "Tile") return UISpellSlot.perkBarNumbers[0];
            if (tag == "DiscFail") return UISpellSlot.perkBarNumbers[1];
            if (tag == "Dazed") return UISpellSlot.perkBarNumbers[2];
            if (tag == "Charge") return UISpellSlot.perkBarNumbers[3];
            if (tag == "Rest") return UISpellSlot.perkBarNumbers[4];
            if (tag == "DodgeFail") return UISpellSlot.perkBarNumbers[5];
            if (tag == "Shield") return UISpellSlot.perkBarNumbers[6];
            return 0;
        }

        private string GetTypeName(GameObject go)
        {
            if (go.GetComponent<Button>() != null) return "Button";
            if (go.GetComponent<UITalentSlot>() != null) return "Perk Slot";
            if (go.GetComponent<UISlotBase>() != null) return "Slot";
            return "Element";
        }
    }
}
