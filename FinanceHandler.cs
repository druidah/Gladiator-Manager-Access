using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;

namespace GladiatorManagerAccess
{
    public class FinanceHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private int _selectedIndex = 0;
        private List<string> _financeItems = new List<string>();
        private string _lastAnnouncedText = "";

        public string GetHelpText()
        {
            return "Finances: Use Up and Down arrows to browse your team's financial status, including balance, salaries, and repair costs. Press Escape to return to the Home screen.";
        }

        public void Update()
        {
            var mm = Object.FindObjectOfType<MoneyManager>();
            bool isOpen = mm != null && mm.finances && AccessStateManager.IsIn(AccessStateManager.State.Finance);

            if (isOpen && !_wasOpen)
            {
                _wasOpen = true;
                _selectedIndex = 0;
                BuildFinanceItems();
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

        private void BuildFinanceItems()
        {
            _financeItems.Clear();
            var mm = Object.FindObjectOfType<MoneyManager>();
            if (mm == null) return;

            var team = DataManager.allTheTeams.Find(x => x.PlayerTeam);
            if (team == null) return;

            _financeItems.Add($"Balance: {team.Money} gold");
            _financeItems.Add($"Weekly Salaries: {team.CurrentSalaryTotal} gold");
            _financeItems.Add($"Salary Budget: {team.SalaryBudget} gold");
            _financeItems.Add($"Fee Budget: {team.FeeBudget} gold");
            
            _financeItems.Add($"Armour Repair Cost (This Year): {team.ArmourCostsThisYear} gold");
            
            // In the game, armour repair is usually automatic or handled in a specific way.
            // If there's a button to trigger manual repair, we'd add it here.
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _selectedIndex--;
                if (_selectedIndex < 0) _selectedIndex = _financeItems.Count - 1;
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _selectedIndex++;
                if (_selectedIndex >= _financeItems.Count) _selectedIndex = 0;
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                ActivateCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitToHome();
            }
        }

        private void AnnounceCurrent()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _financeItems.Count)
            {
                string itemText = _financeItems[_selectedIndex];
                if (itemText == _lastAnnouncedText)
                {
                    ScreenReader.Say(itemText);
                }
                else
                {
                    _lastAnnouncedText = itemText;
                    ScreenReader.Say($"{itemText}, {_selectedIndex + 1} of {_financeItems.Count}.");
                }
            }
        }

        private void ActivateCurrent()
        {
            // Currently no selectable actions in Finance except info
            ScreenReader.Say("This item is for information only.");
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
    }
}
