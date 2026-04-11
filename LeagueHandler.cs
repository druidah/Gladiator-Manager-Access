using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace GladiatorManagerAccess
{
    public class LeagueHandler : IAccessibleHandler
    {
        private enum View { Table, Fixtures, Results }
        private View _view = View.Table;
        
        private bool _wasOpen = false;
        private int _selectedIndex = 0;
        private List<string> _items = new List<string>();

        public string GetHelpText()
        {
            string viewName = _view.ToString();
            return $"League {viewName}: Use Up and Down arrows to browse items. Press Tab to switch between Table, Fixtures, and Results. Use Left and Right arrows to change Division (in Table/Results) or Week (in Fixtures). Press Escape to return Home.";
        }

        public void Update()
        {
            var lp = Object.FindObjectOfType<LeaguePopulator>();
            bool isOpen = lp != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "League";

            if (isOpen && !_wasOpen)
            {
                if (AccessStateManager.TryEnter(AccessStateManager.State.League))
                {
                    OnOpen();
                    _wasOpen = true;
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.League);
                _wasOpen = false;
            }

            if (!isOpen || !AccessStateManager.IsIn(AccessStateManager.State.League)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            var lp = Object.FindObjectOfType<LeaguePopulator>();
            _view = View.Table;
            if (lp != null && lp.cup) ScreenReader.Say("The Cup.");
            else ScreenReader.Say("League Standings.");
            
            _selectedIndex = 0;
            BuildItems();
            AnnounceCurrent();
        }

        private void BuildItems()
        {
            _items.Clear();
            var lp = Object.FindObjectOfType<LeaguePopulator>();
            if (lp == null) return;

            if (lp.cup)
            {
                _items.Add("--- The Cup Rounds ---");
                if (lp.cupRoundFixtures != null)
                {
                    for (int i = 0; i < lp.cupRoundFixtures.Length; i++)
                    {
                        var f = lp.cupRoundFixtures[i];
                        if (f != null && !string.IsNullOrEmpty(f.text))
                        {
                            _items.Add($"Round {i + 1}: {f.text}");
                        }
                    }
                }
                if (_items.Count <= 1) _items.Add("Cup draws not yet available.");
                return;
            }

            if (_view == View.Table)
            {
                _items.Add($"--- {lp.titleText.text} Standings ---");
                // The table uses SLS.Widgets.Table which is hard to read directly from code without reflection
                // However, we can use the DataManager teams list for the current division
                var teams = DataManager.allTheTeams
                    .Where(x => x.League == lp.leagueSelector.value + 1)
                    .OrderByDescending(x => x.Points)
                    .ThenByDescending(x => x.Diff)
                    .ToList();

                int rank = 1;
                foreach (var t in teams)
                {
                    _items.Add($"{rank}. {t.Name}: {t.Points} pts. Record: {t.Wins}W, {t.Losses}L. Kills: {t.KillsFor}.");
                    rank++;
                }
            }
            else if (_view == View.Fixtures)
            {
                _items.Add($"--- {lp.fixtureDisplayWeek.text} Fixtures ---");
                foreach (var f in lp.Fixtures)
                {
                    if (f != null && !string.IsNullOrEmpty(f.text))
                    {
                        _items.Add(f.text);
                    }
                }
            }
            else if (_view == View.Results)
            {
                _items.Add($"--- {lp.titleText.text} Recent Results ---");
                // lp.resultText array (indices 1-5 usually)
                if (lp.resultText != null)
                {
                    foreach (var r in lp.resultText)
                    {
                        if (r != null && !string.IsNullOrEmpty(r.text))
                        {
                            _items.Add(r.text);
                        }
                    }
                }
                if (_items.Count <= 1) _items.Add("No results recorded yet.");
            }
        }

        private void ProcessInput()
        {
            var lp = Object.FindObjectOfType<LeaguePopulator>();
            if (lp == null) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _view = (View)(((int)_view + 1) % 3);
                _selectedIndex = 0;
                BuildItems();
                ScreenReader.Say($"{_view} View.");
                AnnounceCurrent();
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
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (_view == View.Fixtures) lp.ChangeFixtureWeek(false);
                else AdjustDivision(lp, -1);
                
                BuildItems();
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (_view == View.Fixtures) lp.ChangeFixtureWeek(true);
                else AdjustDivision(lp, 1);

                BuildItems();
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                var lm = Object.FindObjectOfType<LevelManager>();
                if (lm != null) lm.LoadLevel("Home");
            }
        }

        private void AdjustDivision(LeaguePopulator lp, int direction)
        {
            if (lp.leagueSelector == null) return;
            int next = lp.leagueSelector.value + direction;
            if (next >= 0 && next < lp.leagueSelector.options.Count)
            {
                lp.leagueSelector.value = next;
                lp.ChangeLeagueOnDisplay();
                ScreenReader.Say($"Division: {lp.leagueSelector.options[next].text}");
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
