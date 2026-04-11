using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace GladiatorManagerAccess
{
    public class TeamSelectionHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _currentIndex = 0;
        private List<Gladiator> _availableGlads = new List<Gladiator>();
        private List<Gladiator> _enemyGlads = new List<Gladiator>();

        public string GetHelpText()
        {
            return $"Team Selection: Use Up and Down to navigate available fighters. Press Enter to assign. Ctrl + 1 to 6 for your lineup, Alt + 1 to 6 for enemy stats. F3 for selected stats. F2 for status. Space to confirm and start battle.";
        }

        public void Update()
        {
            bool isOpen = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TeamSelection";

            if (isOpen && !_wasOpen)
            {
                if (AccessStateManager.TryEnter(AccessStateManager.State.TeamSelection))
                {
                    OnOpen();
                    _wasOpen = true;
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.TeamSelection);
                _wasOpen = false;
            }

            if (!isOpen || !AccessStateManager.IsIn(AccessStateManager.State.TeamSelection)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            _currentIndex = 0;
            RefreshAvailableGlads();
            RefreshEnemyGlads();
            ScreenReader.Say($"Team Selection. You must pick {Calendar.numOfGladsThisWeek} gladiators.");
            AnnounceCurrent();
        }

        private void RefreshAvailableGlads()
        {
            _availableGlads = DataManager.allThePGladiators
                .Where(x => x.Recruited && x.Alive && !x.Injured)
                .ToList();
        }

        private void RefreshEnemyGlads()
        {
            _enemyGlads.Clear();
            for (int i = 0; i < 6; i++)
            {
                int id = Calendar.chosenGladiator[i];
                if (id != 0)
                {
                    var g = DataManager.allTheEGladiators.Find(x => x.ID == id);
                    if (g != null) _enemyGlads.Add(g);
                }
            }
        }

        private void ProcessInput()
        {
            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    {
                        AnnouncePlayerInSlot(i);
                        return;
                    }
                    else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    {
                        AnnounceEnemyInSlot(i);
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (_availableGlads.Count == 0) return;
                _currentIndex = (_currentIndex - 1 + _availableGlads.Count) % _availableGlads.Count;
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (_availableGlads.Count == 0) return;
                _currentIndex = (_currentIndex + 1) % _availableGlads.Count;
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                AssignSelected();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                ConfirmSelection();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                if (_availableGlads.Count == 0) return;
                var g = _availableGlads[_currentIndex];
                ScreenReader.Say($"Selected: {FormatGladiatorStats(g, true)}");
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                AnnounceStatus();
            }
        }

        private void AnnouncePlayerInSlot(int index)
        {
            if (index < 0 || index >= Calendar.numOfGladsThisWeek) return;
            int id = PlayerSelector.playerSelected[index];
            if (id == 0) ScreenReader.Say($"Slot {index + 1} is empty.");
            else
            {
                var g = DataManager.allThePGladiators.Find(x => x.ID == id);
                if (g != null) ScreenReader.Say($"Lineup Slot {index + 1}: {FormatGladiatorStats(g, true)}");
            }
        }

        private void AnnounceEnemyInSlot(int index)
        {
            if (index < 0 || index >= 6) return;
            int id = Calendar.chosenGladiator[index];
            if (id == 0) ScreenReader.Say($"No enemy in slot {index + 1}.");
            else
            {
                var g = DataManager.allTheEGladiators.Find(x => x.ID == id);
                if (g != null) ScreenReader.Say($"Enemy in Slot {index + 1}: {FormatGladiatorStats(g, false)}");
            }
        }

        private string FormatGladiatorStats(Gladiator g, bool isPlayer)
        {
            string info = $"{g.FirstName} {g.Surname} ({g.Class}, Level {g.Level}). ";
            
            if (!isPlayer)
            {
                string advantages = GetClassAdvantageInfo(g.Class);
                if (!string.IsNullOrEmpty(advantages)) info += advantages + " ";
            }

            if (isPlayer)
            {
                info += $"Salary: {g.Salary} gold. ";
                info += $"Str {g.Strength}, Agi {g.Agility}, Skill {g.Sword}, Tou {g.Toughness}, Bra {g.Bravery}, Lea {g.Leadership}, Ini {g.Initiative}, Spe {g.Speed}, Sta {g.Stamina}, Rec {g.Recovery}, Dis {g.Discipline}.";
            }
            else
            {
                // For enemies, we should ideally check visibility, but user might want to know everything if they ask.
                // Looking at game's behavior, usually you only see what you scout.
                // Let's check Visibility.
                var v = DataManager.allTheEVisStats.Find(x => x.ID == g.ID);
                if (v == null) 
                {
                    // Fallback: Show everything? Or just basic?
                    // Given the user said "I don't know what we know about them in advance",
                    // showing what is visible is probably most authentic.
                    info += "Stats unknown.";
                }
                else
                {
                    List<string> stats = new List<string>();
                    if (v.Strength) stats.Add($"Str {g.Strength}");
                    if (v.Agility) stats.Add($"Agi {g.Agility}");
                    if (v.Sword) stats.Add($"Skill {g.Sword}");
                    if (v.Toughness) stats.Add($"Tou {g.Toughness}");
                    if (v.Bravery) stats.Add($"Bra {g.Bravery}");
                    if (v.Leadership) stats.Add($"Lea {g.Leadership}");
                    if (v.Initiative) stats.Add($"Ini {g.Initiative}");
                    if (v.Speed) stats.Add($"Spe {g.Speed}");
                    if (v.Stamina) stats.Add($"Sta {g.Stamina}");
                    if (v.Recovery) stats.Add($"Rec {g.Recovery}");
                    if (v.Discipline) stats.Add($"Dis {g.Discipline}");
                    
                    if (stats.Count == 0) info += "No stats scouted.";
                    else info += string.Join(", ", stats) + ".";
                }
            }
            return info;
        }

        private string GetClassAdvantageInfo(string className)
        {
            switch (className)
            {
                case "Barbarian": return "Strong against: Rogues. Vulnerable to: Defenders.";
                case "Rogue": return "Strong against: Retarii. Vulnerable to: Barbarians.";
                case "Retarius": return "Strong against: Gladiators. Vulnerable to: Rogues.";
                case "Gladiator": return "Strong against: Leaders. Vulnerable to: Retarii.";
                case "Leader": return "Strong against: Defenders. Vulnerable to: Gladiators.";
                case "Defender": return "Strong against: Barbarians. Vulnerable to: Leaders.";
                default: return "";
            }
        }

        private void AnnounceCurrent()
        {
            if (_availableGlads.Count == 0)
            {
                ScreenReader.Say("No healthy gladiators available for selection!");
                return;
            }

            var g = _availableGlads[_currentIndex];
            ScreenReader.Say($"{g.FirstName} {g.Surname}, {g.Class}, Current Ability: {g.CurrentAbility}, {_currentIndex + 1} of {_availableGlads.Count}.");
        }

        private void AssignSelected()
        {
            if (_currentIndex < 0 || _currentIndex >= _availableGlads.Count) return;
            var g = _availableGlads[_currentIndex];

            var selector = Object.FindObjectOfType<PlayerSelector>();
            if (selector != null)
            {
                selector.confirmTableSelection(g.ID);
                AnnounceStatus();
            }
        }

        private void ConfirmSelection()
        {
            var selector = Object.FindObjectOfType<PlayerSelector>();
            if (selector != null && selector.confirmButton != null && selector.confirmButton.interactable)
            {
                ScreenReader.Say("Confirming lineup. Entering the arena...");
                selector.confirmButton.onClick.Invoke();
            }
            else
            {
                ScreenReader.Say($"Cannot start. You must pick {Calendar.numOfGladsThisWeek} gladiators.");
            }
        }

        private void ClearSelection()
        {
            var selector = Object.FindObjectOfType<PlayerSelector>();
            if (selector != null)
            {
                selector.ClearSelection();
                ScreenReader.Say("Selection cleared.");
            }
        }

        private void AnnounceStatus()
        {
            int filled = 0;
            string lineup = "";
            for (int i = 0; i < Calendar.numOfGladsThisWeek; i++)
            {
                int id = PlayerSelector.playerSelected[i];
                if (id != 0)
                {
                    filled++;
                    var g = DataManager.allThePGladiators.Find(x => x.ID == id);
                    if (g != null) lineup += $"Slot {i+1}: {g.FirstName}. ";
                }
                else
                {
                    lineup += $"Slot {i+1}: Empty. ";
                }
            }

            ScreenReader.Say($"Lineup: {filled} of {Calendar.numOfGladsThisWeek} selected. {lineup}");
        }
    }
}
