using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;
using SLS.Widgets.Table;

namespace GladiatorManagerAccess
{
    public class RecordsHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _selectedIndex = 0;
        private List<string> _items = new List<string>();

        public string GetHelpText()
        {
            return "Records Screen: Use Up and Down arrows to browse statistics. Press Tab to switch between Team Records, League Records, Hall of Fame, and League Hall of Fame. Press Left and Right arrows to change Division where applicable. Press Escape to return home.";
        }

        public void Update()
        {
            var lp = Object.FindObjectOfType<LeaguePopulator>();
            var rd = Object.FindObjectOfType<RecordsDropDown>();
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            bool isOpen = (rd != null || (lp != null && lp.records) || sceneName == "PlayerRecords");

            if (isOpen && !_wasOpen)
            {
                if (AccessStateManager.TryEnter(AccessStateManager.State.Records))
                {
                    OnOpen();
                    _wasOpen = true;
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.Records);
                _wasOpen = false;
            }

            if (!isOpen || !AccessStateManager.IsIn(AccessStateManager.State.Records)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            _selectedIndex = 0;
            // Delay slightly to ensure data is populated
            MelonCoroutines.Start(DelayedOpen());
        }

        private System.Collections.IEnumerator DelayedOpen()
        {
            yield return new WaitForSeconds(0.2f);
            BuildItems();
            var rd = Object.FindObjectOfType<RecordsDropDown>();
            string title = rd != null && rd.titleText != null ? rd.titleText.text : "Records";
            ScreenReader.Say($"{title} Screen.");
            AnnounceCurrent();
        }

        private void BuildItems()
        {
            _items.Clear();
            var rd = Object.FindObjectOfType<RecordsDropDown>();
            if (rd == null) return;

            string title = rd.titleText != null ? rd.titleText.text : "Records";
            _items.Add($"--- {title} ---");

            // DataTable.popstatlist contains the generated stats
            if (DataTable.popstatlist != null)
            {
                foreach (var stat in DataTable.popstatlist)
                {
                    if (stat == null || stat.ID <= 0) continue;
                    
                    // Fetch accurate data using the unique ID
                    var g = DataManager.allThePGladiators.Find(x => x.ID == stat.ID) ?? 
                            DataManager.allTheEGladiators.Find(x => x.ID == stat.ID);
                    
                    // In Team Records, only show gladiators that actually belong to the player
                    if (DataTable.statScreenDisplayOption == "Team")
                    {
                        if (g == null || !g.Recruited) continue;
                    }

                    string name = stat.Name;
                    string teamName = stat.Team;

                    if (g != null)
                    {
                        name = $"{g.FirstName} {g.Surname}";
                        var t = DataManager.allTheTeams.Find(x => x.TeamID == g.Team);
                        if (t != null) teamName = t.Name;
                    }

                    // Individual/Gladiator stats
                    string entry = $"{name} ({teamName}): {stat.Wins} wins, {stat.Kills} kills. Rating: {stat.BattleRating:F1}.";
                    
                    // Avoid adding the exact same string multiple times (secondary safety)
                    if (!_items.Contains(entry))
                    {
                        _items.Add(entry);
                    }
                }
            }

            if (_items.Count <= 1)
            {
                _items.Add("No records found for this category.");
            }
        }

        private void ProcessInput()
        {
            var rd = Object.FindObjectOfType<RecordsDropDown>();
            if (rd == null) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // Cycle main dropdown (Team, League, HOF, HOFL)
                if (rd.thisDropdown != null)
                {
                    rd.thisDropdown.value = (rd.thisDropdown.value + 1) % rd.thisDropdown.options.Count;
                    rd.ActionsOnChange();
                    _selectedIndex = 0;
                    BuildItems();
                    ScreenReader.Say(rd.titleText.text);
                    AnnounceCurrent();
                }
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (_items.Count == 0) return;
                _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (_items.Count == 0) return;
                _selectedIndex = (_selectedIndex + 1) % _items.Count;
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                // Adjust division if the dropdown is active
                if (rd.divisionDropdown != null && rd.divisionDropdown.transform.localScale.x > 0.5f)
                {
                    int dir = Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 1;
                    int next = rd.divisionDropdown.value + dir;
                    if (next >= 0 && next < rd.divisionDropdown.options.Count)
                    {
                        rd.divisionDropdown.value = next;
                        rd.ActionsOnChange();
                        _selectedIndex = 0;
                        BuildItems();
                        ScreenReader.Say($"Division: {rd.divisionDropdown.options[next].text}");
                        AnnounceCurrent();
                    }
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                var lm = Object.FindObjectOfType<LevelManager>();
                if (lm != null) lm.LoadLevel("Home");
            }
        }

        private void AnnounceCurrent()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                ScreenReader.Say($"{_items[_selectedIndex]}, {_selectedIndex + 1} of {_items.Count}.");
            }
        }
    }
}
