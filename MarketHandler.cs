using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace GladiatorManagerAccess
{
    public class MarketHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _selectedIndex = 0;
        private List<string> _marketItems = new List<string>();
        private bool _viewingProfile = false;
        private int _selectedGladId = -1;
        private List<int> _hirableIndices = new List<int>();

        public string GetHelpText()
        {
            if (_viewingProfile)
                return "Market Recruitment: Use Up and Down arrows to review the gladiator's contract and stats. Press Enter on 'Recruit' to hire them. Press Escape to return to the list.";
            
            return "Market List: Use Up and Down arrows to browse available recruits. Press Enter to view a gladiator's profile and contract details. Press Escape to return home.";
        }

        public void Update()
        {
            var pop = Object.FindObjectOfType<Populator>();
            bool isOpen = pop != null && pop.barracks && AccessStateManager.IsIn(AccessStateManager.State.Shop);

            if (isOpen && !_wasOpen)
            {
                _wasOpen = true;
                _viewingProfile = false;
                _selectedIndex = 0;
                RefreshHirableList();
                BuildMarketItems();
                AnnounceCurrent();
            }
            else if (!isOpen && _wasOpen)
            {
                _wasOpen = false;
            }

            if (isOpen)
            {
                ProcessInput();
            }
        }

        private void RefreshHirableList()
        {
            _hirableIndices.Clear();
            // In this game, hirable gladiators are often in the allThePGladiators list but with Recruited = false
            var all = DataManager.allThePGladiators;
            for (int i = 0; i < all.Count; i++)
            {
                if (!all[i].Recruited && all[i].Alive)
                {
                    _hirableIndices.Add(all[i].ID);
                }
            }
        }

        private void BuildMarketItems()
        {
            _marketItems.Clear();
            if (_viewingProfile)
            {
                BuildProfileItems();
                return;
            }

            var team = DataManager.allTheTeams.Find(x => x.PlayerTeam);
            _marketItems.Add($"--- Market (Money: {team.Money} gold) ---");
            RefreshHirableList();

            if (_hirableIndices.Count == 0)
            {
                _marketItems.Add("No recruits available this week.");
            }
            else
            {
                foreach (int id in _hirableIndices)
                {
                    var g = DataManager.allThePGladiators.Find(x => x.ID == id);
                    if (g != null)
                    {
                        _marketItems.Add($"{g.FirstName} {g.Surname} ({g.Class}) - Fee: {g.SigningFee} gold");
                    }
                }
            }
            _marketItems.Add("Back to Market List");
        }

        private void BuildProfileItems()
        {
            var g = DataManager.allThePGladiators.Find(x => x.ID == _selectedGladId);
            if (g == null)
            {
                _viewingProfile = false;
                BuildMarketItems();
                return;
            }

            _marketItems.Add($"--- {g.FirstName} {g.Surname} Profile ---");
            _marketItems.Add($"Origin: {g.Origin}");
            _marketItems.Add($"Class: {g.Class}");
            _marketItems.Add($"Age: {g.AgeYears} years");
            _marketItems.Add($"Gender: {g.Gender}");
            _marketItems.Add($"Appearance: {AppearanceUtilities.GetAppearance(g.ID, true)}");
            
            // For hirable, stats are often hidden or estimated
            _marketItems.Add($"Contract: {g.ContractType}, {g.ContractLength} weeks");
            _marketItems.Add($"Salary: {g.Salary} gold/week");
            _marketItems.Add($"Signing Fee: {g.SigningFee} gold");

            _marketItems.Add("--- Attributes ---");
            var v = GetVisibility(g.ID);
            _marketItems.Add(FormatStat("Initiative", g.Initiative, false, v?.Initiative ?? true));
            _marketItems.Add(FormatStat("Strength", g.Strength, false, v?.Strength ?? true));
            _marketItems.Add(FormatStat("Agility", g.Agility, false, v?.Agility ?? true));
            _marketItems.Add(FormatStat("Toughness", g.Toughness, false, v?.Toughness ?? true));
            _marketItems.Add(FormatStat("Discipline", g.Discipline, false, v?.Discipline ?? true));
            _marketItems.Add(FormatStat("Weapon Skill", g.Sword, false, v?.Sword ?? true));
            _marketItems.Add(FormatStat("Bravery", g.Bravery, false, v?.Bravery ?? true));
            _marketItems.Add(FormatStat("Recovery", g.Recovery, false, v?.Recovery ?? true));
            _marketItems.Add(FormatStat("Speed", g.Speed, false, v?.Speed ?? true));
            _marketItems.Add(FormatStat("Stamina", g.Stamina, false, v?.Stamina ?? true));
            _marketItems.Add(FormatStat("Leadership", g.Leadership, false, v?.Leadership ?? true));

            _marketItems.Add(FormatStat("Current Ability", g.CurrentAbility, false, v?.CurrentAbility ?? true));
            _marketItems.Add(FormatStat("Potential", g.PotAbility, false, v?.PotentialAbility ?? true));

            _marketItems.Add($"Recruit {g.FirstName} for {g.SigningFee} gold");
            _marketItems.Add("Back to Market List");
            }

            private string FormatStat(string label, int value, bool isOwned, bool visible)
            {
                if (isOwned) return $"{label}: {value}";

                bool isMental = label.Contains("Intelligence") || label.Contains("Work Ethic");
                string desc = isMental ?
                    GeneralUtilities.AttributeIntToDescriptonWorkEthicIntelligence(value) :
                    GeneralUtilities.AttributeIntToDescripton(value);

                if (label == "Current Ability" || label == "Potential")
                {
                    int stars = Mathf.CeilToInt(value / 10f);
                    if (stars < 1) stars = 1;
                    if (stars > 20) stars = 20;
                    
                    if (visible) return $"{label}: {desc} ({stars} stars, value {value})";
                    return $"{label}: {desc} ({stars} stars)";
                }

                if (visible) return $"{label}: {desc} ({value})";
                return $"{label}: {desc}";
            }

            private VisibileStats GetVisibility(int id)
            {
                try { var list = DataManager.dbManager.Query<VisibileStats>("SELECT * FROM RecruitmentVisibility WHERE ID = ?", id); return list != null && list.Count > 0 ? list[0] : null; }
                catch { return null; }
            }

            private void ProcessInput()
            {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = _marketItems.Count - 1;
            AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
            _selectedIndex++;
            if (_selectedIndex >= _marketItems.Count) _selectedIndex = 0;
            AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
            ActivateCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
            if (_viewingProfile)
            {
                _viewingProfile = false;
                _selectedIndex = 0;
                BuildMarketItems();
                AnnounceCurrent();
            }
            else
            {
                ExitToHome();
            }
            }
            }

            private void ExitToHome()
            {
            var lm = Object.FindObjectOfType<LevelManager>();
            if (lm != null)
            {
            lm.LoadLevel("Home");
            }
            else
            {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
            }
            }

            private void SyncGameSelectionWithModSelection(Populator pop)
            {
                if (_selectedGladId == -1) return;

                for (int i = 1; i <= 24; i++)
                {
                    if (Populator.rearrangeGrid[i] == _selectedGladId)
                    {
                        if (i <= 12)
                        {
                            Populator.bottomHalfDisplay = false;
                            Populator.displayAdjustment = 0;
                            pop.changeDisplayID(i);
                        }
                        else
                        {
                            Populator.bottomHalfDisplay = true;
                            Populator.displayAdjustment = 12;
                            pop.changeDisplayID(i - 12);
                        }
                        break;
                    }
                }
            }

            private void AnnounceCurrent()
            {
            if (_selectedIndex >= 0 && _selectedIndex < _marketItems.Count)
            {
            string itemText = _marketItems[_selectedIndex];
            ScreenReader.Say($"{itemText}, {_selectedIndex + 1} of {_marketItems.Count}.");
            }
            }

            private void ActivateCurrent()
            {
            string item = _marketItems[_selectedIndex];

            if (item == "Back to Market List")
            {
            _viewingProfile = false;
            _selectedIndex = 0;
            BuildMarketItems();
            AnnounceCurrent();
            }
            else if (item.Contains("Recruit") && item.Contains(" for "))
            {
            var pop = Object.FindObjectOfType<Populator>();
            if (pop != null && pop.recruitButton != null)
            {
                SyncGameSelectionWithModSelection(pop);
                pop.recruitButton.onClick.Invoke();
                // After recruiting, the list changes
                MelonLoader.MelonCoroutines.Start(DelayedRefreshAfterRecruit());
            }
            }
            else if (!_viewingProfile && item.Contains("Fee:"))
            {
            // Select gladiator from list
            int listIndex = _selectedIndex - 1; // Subtract header
            if (listIndex >= 0 && listIndex < _hirableIndices.Count)
            {
                _selectedGladId = _hirableIndices[listIndex];
                _viewingProfile = true;
                _selectedIndex = 0;

                var pop = Object.FindObjectOfType<Populator>();
                if (pop != null) SyncGameSelectionWithModSelection(pop);

                BuildMarketItems();
                AnnounceCurrent();
            }
            }
            }
        private System.Collections.IEnumerator DelayedRefreshAfterRecruit()
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            _viewingProfile = false;
            _selectedIndex = 0;
            RefreshHirableList();
            BuildMarketItems();
            ScreenReader.Say("Recruitment successful. Returning to market list.");
            AnnounceCurrent();
        }
    }
}
