using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;
using SimpleSQL;
using System.Text.RegularExpressions;

namespace GladiatorManagerAccess
{
    public class BarracksHandler : IAccessibleHandler
    {
        private enum Mode { Owned, Hirable, Profile, Training }
        private Mode _mode = Mode.Owned;

        public string GetHelpText()
        {
            switch (_mode)
            {
                case Mode.Owned:
                    return "Barracks: Owned Gladiators. Use Up and Down to select a fighter. Press Tab to switch to Hirable fighters. Press F2 for team status. Press Enter to view the selected gladiator's profile.";
                case Mode.Hirable:
                    return "Barracks: Gladiators for Hire. Use Up and Down to select a fighter. Press Tab to switch to Owned fighters. Press F2 for team status. Press Enter to view the selected gladiator's profile.";
                case Mode.Profile:
                    return "Gladiator Profile: Use Up and Down to read stats, class description, and actions. Press Enter on an action to activate it. Press Escape to return to the list.";
                case Mode.Training:
                    return "Training Adjustment: Use Up and Down to select Focus or Intensity. Press Enter to cycle through options. Press Escape to return to the profile.";
                default:
                    return "Barracks: Manage your team. Use arrows to navigate and Enter to select.";
            }
        }

        private int _ownedIndex = 0;
        private int _hirableIndex = 0;
        private int _trainingStep = 0; // 0: Focus, 1: Intensity, 2: Back
        private int _profileStep = 0;

        private List<string> _profileItems = new List<string>();
        private List<int> _ownedIndices = new List<int>(); 
        private List<int> _hirableIndices = new List<int>(); 

        private bool _wasOpen = false;

        public static bool IsBarracksOpen()
        {
            var pop = Object.FindObjectOfType<Populator>();
            return pop != null && pop.barracks && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Barracks";
        }

        public void Update()
        {
            bool isOpen = IsBarracksOpen();

            if (isOpen)
            {
                // If the screen is open, but we've lost the state (e.g. returning from Perk screen)
                if (!AccessStateManager.IsIn(AccessStateManager.State.Barracks) && 
                    AccessStateManager.Current == AccessStateManager.State.None)
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Barracks);
                }

                if (!_wasOpen)
                {
                    if (AccessStateManager.TryEnter(AccessStateManager.State.Barracks))
                    {
                        OnOpen();
                        _wasOpen = true;
                    }
                }
            }
            else if (!isOpen && _wasOpen)
            {
                AccessStateManager.Exit(AccessStateManager.State.Barracks);
                _wasOpen = false;
            }

            if (!AccessStateManager.IsIn(AccessStateManager.State.Barracks)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            _mode = Mode.Owned;
            RefreshLists();
            
            ScreenReader.Say("Barracks. Owned gladiators list.");
            if (_ownedIndices.Count > 0)
            {
                _ownedIndex = 0;
                SelectGladiatorInGrid(_ownedIndices[_ownedIndex]);
            }
            AnnounceCurrentItem();
        }

        private void RefreshLists()
        {
            _ownedIndices.Clear();
            _hirableIndices.Clear();

            // Use the game's internal rearrangeGrid to ensure we're targeting the correct slots.
            // rearrangeGrid is 1-indexed (indices 1-24 are used for gladiators).
            for (int i = 1; i <= 24; i++)
            {
                int id = Populator.rearrangeGrid[i];
                if (id == 0) continue;

                var g = DataManager.allThePGladiators.Find(x => x.ID == id);
                if (g != null)
                {
                    if (g.Recruited)
                    {
                        _ownedIndices.Add(i);
                    }
                    else
                    {
                        _hirableIndices.Add(i);
                    }
                }
            }

            // Hirable gladiators are added to rearrangeGrid by the game from the end (index 24) downwards.
            // Sorting _hirableIndices descending ensures we navigate them in the order they were generated (ID 6, 7, etc.)
            // as Populator.rearrangeGrid[24] typically holds the first hirable gladiator found.
            _hirableIndices.Sort((a, b) => b.CompareTo(a));
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_mode != Mode.Profile && _mode != Mode.Training) ToggleMode();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow)) Navigate(-1);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) Navigate(1);
            else if (Input.GetKeyDown(KeyCode.Return)) ActivateCurrent();
            else if (Input.GetKeyDown(KeyCode.F2)) AnnounceStatus();
            else if (Input.GetKeyDown(KeyCode.F3)) AnnounceDetailedStats();
            else if (Input.GetKeyDown(KeyCode.LeftArrow) && _mode == Mode.Training) AdjustTraining(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow) && _mode == Mode.Training) AdjustTraining(1);
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_mode == Mode.Training)
                {
                    _mode = Mode.Profile;
                    _profileStep = 0;
                    BuildProfileItems();
                    ScreenReader.Say("Back to Profile.");
                    AnnounceCurrentItem();
                }
                else if (_mode == Mode.Profile)
                {
                    var g = DataManager.allThePGladiators.Find(x => x.ID == GetSelectedGladId());
                    _mode = (g != null && g.Recruited) ? Mode.Owned : Mode.Hirable;
                    ScreenReader.Say("Back to list.");
                    AnnounceCurrentItem();
                }
                else
                {
                    var lm = Object.FindObjectOfType<LevelManager>();
                    if (lm != null) lm.ExitBarracksToRightPlace();
                }
            }
        }

        private void ToggleMode()
        {
            if (_mode == Mode.Owned)
            {
                _mode = Mode.Hirable;
                RefreshLists();
                _hirableIndex = 0;
                ScreenReader.Say("Hirable fighters list.");
                if (_hirableIndices.Count > 0) SelectGladiatorInGrid(_hirableIndices[_hirableIndex]);
            }
            else
            {
                _mode = Mode.Owned;
                RefreshLists();
                _ownedIndex = 0;
                ScreenReader.Say("Owned gladiators list.");
                if (_ownedIndices.Count > 0) SelectGladiatorInGrid(_ownedIndices[_ownedIndex]);
            }
            AnnounceCurrentItem();
        }

        private void Navigate(int direction)
        {
            if (_mode == Mode.Owned)
            {
                if (_ownedIndices.Count == 0) return;
                _ownedIndex = (_ownedIndex + direction + _ownedIndices.Count) % _ownedIndices.Count;
                SelectGladiatorInGrid(_ownedIndices[_ownedIndex]);
            }
            else if (_mode == Mode.Hirable)
            {
                if (_hirableIndices.Count == 0) return;
                _hirableIndex = (_hirableIndex + direction + _hirableIndices.Count) % _hirableIndices.Count;
                SelectGladiatorInGrid(_hirableIndices[_hirableIndex]);
            }
            else if (_mode == Mode.Profile)
            {
                _profileStep = (_profileStep + direction + _profileItems.Count) % _profileItems.Count;
            }
            else if (_mode == Mode.Training)
            {
                _trainingStep = (_trainingStep + direction + 3) % 3;
            }

            AnnounceCurrentItem();
        }

        private int GetSelectedGladId()
        {
            int displayAdjustment = Populator.bottomHalfDisplay ? 12 : 0;
            int idx = Populator.gladiatorDisplayID + displayAdjustment;
            if (idx < 0 || idx >= Populator.rearrangeGrid.Length) return -1;
            return Populator.rearrangeGrid[idx];
        }

        private void SelectGladiatorInGrid(int gridIndex)
        {
            var pop = Object.FindObjectOfType<Populator>();
            if (pop == null) return;

            if (gridIndex <= 12)
            {
                Populator.bottomHalfDisplay = false;
                Populator.displayAdjustment = 0;
                pop.changeDisplayID(gridIndex);
            }
            else
            {
                Populator.bottomHalfDisplay = true;
                Populator.displayAdjustment = 12;
                pop.changeDisplayID(gridIndex - 12);
            }
        }

        private void ActivateCurrent()
        {
            if (_mode == Mode.Owned || _mode == Mode.Hirable)
            {
                _mode = Mode.Profile;
                _profileStep = 0;
                BuildProfileItems();
                ScreenReader.Say("Opening Gladiator Profile.");
            }
            else if (_mode == Mode.Profile)
            {
                if (_profileStep < 0 || _profileStep >= _profileItems.Count) return;
                string item = _profileItems[_profileStep];
                
                if (item.Contains("Training Adjustment"))
                {
                    _mode = Mode.Training;
                    _trainingStep = 0;
                    ScreenReader.Say("Training Adjustment Mode.");
                }
                else if (item.Contains(Loc.Get("level_up")) || item.Contains(Loc.Get("perks")))
                {
                    var pop = Object.FindObjectOfType<Populator>();
                    if (pop != null && pop.levelUpButton != null)
                    {
                        pop.levelUpButton.onClick.Invoke();
                        return; // Exit immediately, sub-handler takes over
                    }
                }
                else if (item.Contains("Fire Gladiator"))
                {
                    var gen = Object.FindObjectOfType<GladiatorGenerator>();
                    if (gen != null)
                    {
                        gen.Sack(GetSelectedGladId(), false);
                        if (GladiatorGenerator.areYouSure) ScreenReader.Say("Press Enter again to confirm firing.");
                        else { _mode = Mode.Owned; MelonCoroutines.Start(DelayedRefreshAndAnnounce("Fired.")); }
                    }
                }
                else if (item.Contains("Hire Gladiator"))
                {
                    var gen = Object.FindObjectOfType<GladiatorGenerator>();
                    if (gen != null) gen.Recruit();
                    _mode = Mode.Owned;
                    MelonCoroutines.Start(DelayedRefreshAndAnnounce("Hired!"));
                }
                else if (item.Contains("Back to list"))
                {
                    var g = DataManager.allThePGladiators.Find(x => x.ID == GetSelectedGladId());
                    _mode = (g != null && g.Recruited) ? Mode.Owned : Mode.Hirable;
                    ScreenReader.Say("Back to list.");
                }
            }
            else if (_mode == Mode.Training)
            {
                if (_trainingStep == 2) // Back
                {
                    _mode = Mode.Profile;
                    _profileStep = 0;
                    BuildProfileItems();
                    ScreenReader.Say("Back to Profile.");
                }
                else ScreenReader.Say("Value set.");
            }
            AnnounceCurrentItem();
        }

        private void BuildProfileItems()
        {
            _profileItems.Clear();
            var g = DataManager.allThePGladiators.Find(x => x.ID == GetSelectedGladId());
            if (g == null) return;

            bool isOwned = g.Recruited;
            var v = GetVisibility(g.ID);

            _profileItems.Add($"--- {g.FirstName} {g.Surname} Profile ---");
            _profileItems.Add($"Origin: {g.Origin}");
            _profileItems.Add($"Class: {g.Class}");

            // Class details directly below Class
            string classHelp = GetInternalClassHelp(g.Class);
            if (!string.IsNullOrEmpty(classHelp)) _profileItems.Add(StripFormatting(classHelp));

            string advantageInfo = GetClassAdvantageInfo(g.Class);
            if (!string.IsNullOrEmpty(advantageInfo)) _profileItems.Add(StripFormatting(advantageInfo));

            _profileItems.Add($"Age: {g.AgeYears} years");
            _profileItems.Add($"Gender: {g.Gender}");
            _profileItems.Add($"Appearance: {AppearanceUtilities.GetAppearance(g.ID, true)}");
            
            if (isOwned)
            {
                _profileItems.Add($"Mood: {GeneralUtilities.MoodIntToString(g.SpareInt1)} ({g.SpareInt1})");
                _profileItems.Add($"Condition: {GeneralUtilities.EnergyIntToString(g.SpareInt2)} ({g.SpareInt2})");
                _profileItems.Add($"Energy: {g.Energy}%");
                _profileItems.Add($"Experience: {g.Experience}");
                _profileItems.Add($"Level: {g.Level}");
            }

            _profileItems.Add(GetContractText(g));
            _profileItems.Add($"Salary: {g.Salary} gold");
            if (!isOwned) _profileItems.Add($"Signing Fee: {g.SigningFee} gold");

            _profileItems.Add("--- Attributes ---");
            _profileItems.Add(FormatStat("Initiative", g.Initiative, isOwned, v?.Initiative ?? true));
            _profileItems.Add(FormatStat("Strength", g.Strength, isOwned, v?.Strength ?? true));
            _profileItems.Add(FormatStat("Agility", g.Agility, isOwned, v?.Agility ?? true));
            _profileItems.Add(FormatStat("Toughness", g.Toughness, isOwned, v?.Toughness ?? true));
            _profileItems.Add(FormatStat("Discipline", g.Discipline, isOwned, v?.Discipline ?? true));
            _profileItems.Add(FormatStat("Weapon Skill", g.Sword, isOwned, v?.Sword ?? true));
            _profileItems.Add(FormatStat("Bravery", g.Bravery, isOwned, v?.Bravery ?? true));
            _profileItems.Add(FormatStat("Recovery", g.Recovery, isOwned, v?.Recovery ?? true));
            _profileItems.Add(FormatStat("Speed", g.Speed, isOwned, v?.Speed ?? true));
            _profileItems.Add(FormatStat("Stamina", g.Stamina, isOwned, v?.Stamina ?? true));
            _profileItems.Add(FormatStat("Leadership", g.Leadership, isOwned, v?.Leadership ?? true));

            _profileItems.Add("--- Status Details ---");
            _profileItems.Add(UIUtilities.GetInjuryDetails(g.ID, true));

            int ca = g.CurrentAbility;
            if (!isOwned && ca == 0 && g.Strength > 0) ca = CalculateEstimatedAbility(g);
            _profileItems.Add(FormatStat("Current Ability", ca, isOwned, v?.CurrentAbility ?? true));
            _profileItems.Add(FormatStat("Potential", g.PotAbility, isOwned, v?.PotentialAbility ?? true));

            var s = DataManager.allThePStats.Find(x => x.ID == g.ID);
            if (s != null)
            {
                _profileItems.Add("--- Career Statistics ---");
                _profileItems.Add($"Record: {s.Wins} wins, {s.Losses} losses");
                _profileItems.Add($"Kills: {s.Kills}");
                _profileItems.Add($"Damage Dealt: {s.DamageDealt}");
                _profileItems.Add($"Damage Taken: {s.DamageTaken}");
                _profileItems.Add($"Injuries Dealt: {s.InjuriesDealt}");
                _profileItems.Add($"Injuries Taken: {s.InjuriesTaken}");
                _profileItems.Add($"Win Streak: {s.WinStreak} (Max: {s.WinStreakAllTime})");
            }

            if (isOwned)
            {
                _profileItems.Add("Training Adjustment");
                var pop = Object.FindObjectOfType<Populator>();
                if (pop != null)
                {
                    if (pop.LevelUpButtonShow(g.Experience, g.Level)) 
                        _profileItems.Add(Loc.Get("level_up"));
                    else 
                        _profileItems.Add(Loc.Get("perks"));
                }
                _profileItems.Add("Fire Gladiator (Press Enter twice)");
            }
            else _profileItems.Add("Hire Gladiator");
            
            _profileItems.Add("Back to list");
        }

        private System.Collections.IEnumerator DelayedRefreshAndAnnounce(string prefix)
        {
            yield return new WaitForSeconds(0.2f);
            RefreshLists();
            ScreenReader.Say(prefix);
            AnnounceCurrentItem();
        }

        private void AdjustTraining(int direction)
        {
            var pop = Object.FindObjectOfType<Populator>();
            var gen = Object.FindObjectOfType<GladiatorGenerator>();
            if (pop == null || gen == null) return;

            if (_trainingStep == 0) // Focus
            {
                pop.trainingFocusDropdown.value = (pop.trainingFocusDropdown.value + direction + pop.trainingFocusDropdown.options.Count) % pop.trainingFocusDropdown.options.Count;
                gen.SetNewTrainingFocus();
                ScreenReader.Say("Focus: " + pop.trainingFocusDropdown.options[pop.trainingFocusDropdown.value].text);
            }
            else if (_trainingStep == 1) // Intensity
            {
                pop.trainingIntensityDropdown.value = (pop.trainingIntensityDropdown.value + direction + pop.trainingIntensityDropdown.options.Count) % pop.trainingIntensityDropdown.options.Count;
                gen.SetNewTrainingIntensity();
                ScreenReader.Say("Intensity: " + pop.trainingIntensityDropdown.options[pop.trainingIntensityDropdown.value].text);
            }
        }

        private void AnnounceCurrentItem()
        {
            var pop = Object.FindObjectOfType<Populator>();
            if (pop == null) return;

            if (_mode == Mode.Owned) AnnounceGladiator(true);
            else if (_mode == Mode.Hirable) AnnounceGladiator(false);
            else if (_mode == Mode.Profile)
            {
                if (_profileItems != null && _profileItems.Count > 0)
                {
                    if (_profileStep < 0) _profileStep = 0;
                    if (_profileStep >= _profileItems.Count) _profileStep = _profileItems.Count - 1;
                    ScreenReader.Say($"{_profileItems[_profileStep]}, {_profileStep + 1} of {_profileItems.Count}.");
                }
                else
                {
                    ScreenReader.Say("Profile information not available.");
                }
            }
            else if (_mode == Mode.Training)
            {
                string text = "";
                if (_trainingStep == 0) text = "Training Focus: " + pop.trainingFocusDropdown.options[pop.trainingFocusDropdown.value].text;
                else if (_trainingStep == 1) text = "Training Intensity: " + pop.trainingIntensityDropdown.options[pop.trainingIntensityDropdown.value].text;
                else text = "Back to Profile";

                ScreenReader.Say($"{text}, {_trainingStep + 1} of 3.");
            }
        }

        private void AnnounceGladiator(bool owned)
        {
            var g = DataManager.allThePGladiators.Find(x => x.ID == GetSelectedGladId());
            if (g == null) { ScreenReader.Say("No fighters in this list."); return; }

            int index = owned ? _ownedIndex : _hirableIndex;
            int count = owned ? _ownedIndices.Count : _hirableIndices.Count;

            ScreenReader.Say($"{g.FirstName} {g.Surname}, {g.Origin} {g.Class}, {index + 1} of {count}.");
        }

        private void AnnounceDetailedStats()
        {
            var g = DataManager.allThePGladiators.Find(x => x.ID == GetSelectedGladId());
            if (g == null) return;
            
            bool isOwned = g.Recruited;
            string info = $"{g.FirstName} {g.Surname} ({g.Class}, Level {g.Level}). ";
            
            if (isOwned)
            {
                info += $"Str {g.Strength}, Agi {g.Agility}, Skill {g.Sword}, Tou {g.Toughness}, Bra {g.Bravery}, Lea {g.Leadership}, Ini {g.Initiative}, Spe {g.Speed}, Sta {g.Stamina}, Rec {g.Recovery}, Dis {g.Discipline}.";
            }
            else
            {
                var v = GetVisibility(g.ID);
                List<string> stats = new List<string>();
                if (v == null || v.Strength) stats.Add($"Str {g.Strength}");
                if (v == null || v.Agility) stats.Add($"Agi {g.Agility}");
                if (v == null || v.Sword) stats.Add($"Skill {g.Sword}");
                if (v == null || v.Toughness) stats.Add($"Tou {g.Toughness}");
                if (v == null || v.Bravery) stats.Add($"Bra {g.Bravery}");
                if (v == null || v.Leadership) stats.Add($"Lea {g.Leadership}");
                if (v == null || v.Initiative) stats.Add($"Ini {g.Initiative}");
                if (v == null || v.Speed) stats.Add($"Spe {g.Speed}");
                if (v == null || v.Stamina) stats.Add($"Sta {g.Stamina}");
                if (v == null || v.Recovery) stats.Add($"Rec {g.Recovery}");
                if (v == null || v.Discipline) stats.Add($"Dis {g.Discipline}");
                
                if (stats.Count == 0) info += "Stats unknown.";
                else info += string.Join(", ", stats) + ".";
            }
            
            ScreenReader.Say(info);
        }

        private string GetContractText(Gladiator g)
        {
            string label = "contract"; string unit = "weeks";
            if (g.ContractType == "sentence") { label = "sentence"; unit = "fights"; }
            else if (g.ContractType == "slave") { label = "contract"; unit = "wins"; }
            return $"{label}: {g.ContractLength} {unit}";
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
                // Current Ability and Potential are shown as stars (1-20 in game)
                // For screen reader, let's keep it consistent with words but maybe add "Stars" concept
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

        private int CalculateEstimatedAbility(Gladiator g) => (g.Initiative + g.Strength + g.Agility + g.Toughness + g.Discipline + g.Sword + g.Bravery + g.Recovery + g.Speed + g.Stamina + g.Leadership) / 11;

        private void AnnounceStatus()
        {
            var team = DataManager.allTheTeams.Find(x => x.PlayerTeam);
            if (team == null) return;
            ScreenReader.Say($"Barracks: {_ownedIndices.Count} owned, {_hirableIndices.Count} for hire. Gold: {team.Money}. Weekly salaries: {team.CurrentSalaryTotal}. Salary budget: {team.SalaryBudget}.");
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

        private string GetInternalClassHelp(string className)
        {
            // First try our own localized descriptions (more complete and reliable)
            string localDesc = Loc.GetClassDescription(className);
            if (!string.IsNullOrEmpty(localDesc)) return localDesc;

            var helpScreens = Resources.FindObjectsOfTypeAll<HelpScreen>();
            if (helpScreens.Length == 0) return "";

            int panelIdx = 2; // Default to Gladiator
            switch (className)
            {
                case "Gladiator": panelIdx = 2; break;
                case "Leader": panelIdx = 3; break;
                case "Defender": panelIdx = 4; break;
                case "Barbarian": panelIdx = 5; break;
                case "Rogue": panelIdx = 6; break;
                case "Retarius": panelIdx = 7; break;
                case "Freestyler": panelIdx = 2; break; // Use Gladiator as base for freestyler if needed
            }

            foreach (var h in helpScreens)
            {
                if (h.gameObject.scene.name != null && panelIdx < h.helpPanels.Length)
                {
                    var panel = h.helpPanels[panelIdx];
                    if (panel != null) { var t = panel.GetComponentInChildren<Text>(); if (t != null) return t.text; }
                }
            }
            return "";
        }

        private string StripFormatting(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
