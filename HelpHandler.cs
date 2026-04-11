using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;
using System.Text.RegularExpressions;

namespace GladiatorManagerAccess
{
    public class HelpHandler : IAccessibleHandler
    {
        private enum MenuLevel { Main, Manual, Tutorials }
        private MenuLevel _currentLevel = MenuLevel.Main;
        
        private bool _wasOpen = false;
        private int _selectedIndex = 0;
        private bool _viewingContent = false;

        private List<HelpTopic> _manualTopics = new List<HelpTopic>();
        private List<HelpTopic> _tutorialTopics = new List<HelpTopic>();
        private string[] _mainMenuItems = { "Game Manual", "Tutorial Archive" };

        private class HelpTopic
        {
            public string Title;
            public string Content;
            public int PanelIndex;
        }

        public string GetHelpText()
        {
            if (_viewingContent) return "Viewing Content. Press Escape to return to the list.";
            if (_currentLevel == MenuLevel.Main) return "Main Help Menu: Choose between the Game Manual or the Tutorial Archive. Use arrows to select and Enter to open.";
            return "Help Category: Use Up and Down arrows to browse topics. Press Enter to read. Press Escape to go back.";
        }

        public static Main MainInstance;

        public void Update()
        {
            var help = Object.FindObjectOfType<HelpScreen>();
            bool isOpen = help != null && help.gameObject.activeInHierarchy && AccessStateManager.IsIn(AccessStateManager.State.Help);

            if (isOpen && !_wasOpen)
            {
                OnOpen();
                _wasOpen = true;
                return;
            }
            else if (!isOpen && _wasOpen)
            {
                _wasOpen = false;
            }

            if (!isOpen || !AccessStateManager.IsIn(AccessStateManager.State.Help)) return;

            ProcessInput();
        }

        private void OnOpen()
        {
            _currentLevel = MenuLevel.Main;
            _selectedIndex = 0;
            _viewingContent = false;
            BuildManualTopics();
            BuildTutorialTopics();
            ScreenReader.Say("Help Menu. Select a category.");
            AnnounceCurrent();
        }

        private void BuildManualTopics()
        {
            _manualTopics.Clear();
            var help = Object.FindObjectOfType<HelpScreen>();
            if (help == null || help.helpPanels == null) return;

            for (int i = 0; i < help.helpPanels.Length; i++)
            {
                var panel = help.helpPanels[i];
                if (panel == null) continue;
                var textComp = panel.GetComponentInChildren<Text>();
                if (textComp != null)
                {
                    string content = StripFormatting(textComp.text);
                    if (!string.IsNullOrEmpty(content))
                    {
                        _manualTopics.Add(new HelpTopic { Title = GetTopicTitle(i, content), Content = content, PanelIndex = i });
                    }
                }
            }
        }

        private void BuildTutorialTopics()
        {
            _tutorialTopics.Clear();
            
            // Home / General
            AddTutorial("Welcome to Gladiator Manager", "Welcome to Gladiator Manager! These pop-ups will provide occasional assistance. This is a non-linear tutorial - you make all the decisions.");
            AddTutorial("Congratulations", "Congratulations on getting through your first fight! These tutorial hints will continue to pop up from time to time without repetition.");
            AddTutorial("Losing Streak", "Don't worry about losing! This is meant to be a hard game. You start with terrible fighters, but next week you can hire better ones to bolster your team.");
            AddTutorial("New Recruits", "New fighters for hire appear every 4th week throughout the season, replacing any sacked, dead or unhired fighters out of your 24 space stable.");
            AddTutorial("Season Schedule", "The season lasts 52 weeks, running in batches of 4 battles (1v1 to 4v4) with two weeks off between each batch for recovery and the cup (from week 23).");
            AddTutorial("Training Explained", "Every week all fighters engage in normal training. Fighters in battle also engage in battle training. Success depends on Work Ethic and Intelligence checks.");
            AddTutorial("Mood and Condition", "Mood fluctuates based on wins, losses, and injuries. Condition increases when resting and decreases when fighting. Both affect combat performance.");
            AddTutorial("The Cup", "In Week 23 the cup starts! Teams from any division compete. Finance and reputation rewards are bigger if you defeat higher division opponents.");
            AddTutorial("Contract Renewals", "At the end of the season, remember to renew contracts for your fighters and your starting character in the Barracks.");
            AddTutorial("Leveling Up", "Fighters gain experience every battle. Leveling up lets you select new special abilities in the Perk menu.");
            
            // Screen Specific
            AddTutorial("Barracks Overview", "The Barracks lists your starting gladiators and hirable ones. Click on a tab to see details, stats, and contract terms.");
            AddTutorial("Hiring Gladiators", "Hirable gladiators have hidden stats described in terms like 'bad' or 'fair'. Look for high Weapon Skill, Strength, and Agility.");
            AddTutorial("League Screen", "The League shows team standings. Only the top team gains automatic promotion. Positions 2-5 enter the play-offs.");
            AddTutorial("Team Selection", "Select your fighter based on the opponent. Use the Class Advantage wheel to counter them for 25% bonuses.");
            
            // Combat Mechanics
            AddTutorial("Combat: Positioning", "If you win the leadership roll, you can choose starting positions. In the arena, select actions from the bottom bar and hit Space to continue.");
            AddTutorial("Combat: Turn Order", "Turn order depends on action type, control, and speed. Order: yielding; attacking; charging/moving; engaging.");
            AddTutorial("Combat: Controls", "Use Tab to switch fighters. Use 1-0 for actions. Press Space to execute orders. A, S, D, F, G switch card displays.");
            AddTutorial("Combat: Control Mechanic", "Control (the crystal) is crucial. Above 50 gives bonuses and special moves. Below 50 makes you vulnerable to critical strikes.");
            AddTutorial("Combat: Card Fronts", "Change card fronts (A-G) to see attributes, armour, health, and stats. This helps you understand exactly why something happened.");
            AddTutorial("Combat: Detailed Report", "The Detailed Report shows every single roll the simulation made. Studying this can help you learn the game mechanics.");
            AddTutorial("Combat: Bleeding", "Rogue dagger wounds cause extra bleeding. Low blood can cause enemies to become dizzy or unconscious.");
            AddTutorial("Combat: Attack Types", "Horizontal attacks are easier to parry. Vertical attacks are easier to dodge. Counter high agility with horizontal, high skill with vertical.");
            AddTutorial("Combat: Tension", "Every time both sides defend, tension increases. High tension makes fighters break discipline and attack spontaneously.");
            AddTutorial("Combat: Yielding", "If you've lost a fighter, you can yield to prevent further damage. This saves survivors but may harm team popularity.");
            AddTutorial("Combat: Discipline", "Fighters failing discipline checks may disobey orders. Cowards may defend when told to attack, suffering a skill penalty.");
            AddTutorial("Combat: Damaging Armour", "Worn armour increases the chance of hits getting through. Targeted strikes can exploit vulnerable, damaged areas.");
            AddTutorial("Combat: Injuries", "Injuries range from minor nicks to serious severed limbs. Serious injuries cause temporary or permanent loss of ability.");
            AddTutorial("Combat: Leadership", "The team leader performs leadership checks. Success inspires the team with bravery; failure causes confidence loss.");
            AddTutorial("Combat: Guarding", "Defending fighters can 'guard' adjacent allies, giving them a bonus to defence based on weapon skill and initiative.");
            AddTutorial("Combat: Morale & Energy", "Low morale causes panic or yielding. Low energy increases the chance of failing checks and missing turns.");
            AddTutorial("Combat: Flanking", "Attacking an enemy engaged with someone else (flanking) gives bonuses to attack, damage, and armour penetration.");
            AddTutorial("Combat: Special Moves", "Gladiators, Leaders, and Rogues can perform Precise Strikes at high control. Barbarians have Crushing Blows. Defenders have Piercing Strikes.");
            AddTutorial("Combat: Knockdowns", "Powerful blows can knock fighters down. Fallen fighters cannot attack until they stand up again.");
            AddTutorial("Combat: Combinations", "Fighters with high control and tactics can perform special sequences. Three successful moves guarantee a double-damage finishing attack.");
            AddTutorial("Combat: Simulating Fights", "Quick Sim lets the AI take over. All simulations are played out properly round by round with genuine outcomes.");
        }

        private void AddTutorial(string title, string content)
        {
            _tutorialTopics.Add(new HelpTopic { Title = title, Content = StripFormatting(content), PanelIndex = -2 });
        }

        private string GetTopicTitle(int index, string content)
        {
            var help = Object.FindObjectOfType<HelpScreen>();
            bool isMelee = help != null && help.melee;

            switch (index)
            {
                case 0: return "The Basics";
                case 1: return "Classes Overview";
                case 2: return "The Gladiator Class";
                case 3: return "The Leader Class";
                case 4: return "The Defender Class";
                case 5: return "The Barbarian Class";
                case 6: return "The Rogue Class";
                case 7: return "The Retarius Class";
                case 8: return "Attributes Overview";
                case 9: return "Attributes: Skill";
                case 10: return "Attributes: Physical";
                case 11: return "Attributes: Mental";
            }

            if (isMelee)
            {
                switch (index)
                {
                    case 12: return "Armour, Injuries and Defeat";
                    case 13: return "Attack Types";
                }
            }
            
            if (index >= 12) return "Origins and Lore";

            if (content.Length > 30) return content.Substring(0, 27) + "...";
            return content;
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (_viewingContent) { _viewingContent = false; ScreenReader.Say("Back to list."); }
                else Navigate(-1);
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (_viewingContent) { _viewingContent = false; ScreenReader.Say("Back to list."); }
                else Navigate(1);
                AnnounceCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                ActivateCurrent();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscape();
            }
        }

        private void Navigate(int direction)
        {
            int count = 0;
            if (_currentLevel == MenuLevel.Main) count = _mainMenuItems.Length;
            else if (_currentLevel == MenuLevel.Manual) count = _manualTopics.Count;
            else if (_currentLevel == MenuLevel.Tutorials) count = _tutorialTopics.Count;

            if (count > 0) _selectedIndex = (_selectedIndex + direction + count) % count;
        }

        private void ActivateCurrent()
        {
            if (_viewingContent) return;

            if (_currentLevel == MenuLevel.Main)
            {
                if (_selectedIndex == 0) _currentLevel = MenuLevel.Manual;
                else _currentLevel = MenuLevel.Tutorials;
                
                _selectedIndex = 0;
                ScreenReader.Say(_currentLevel == MenuLevel.Manual ? "Game Manual." : "Tutorial Archive.");
                AnnounceCurrent();
            }
            else
            {
                _viewingContent = true;
                AnnounceCurrent();
            }
        }

        private void HandleEscape()
        {
            if (_viewingContent)
            {
                _viewingContent = false;
                ScreenReader.Say("Back to list.");
                AnnounceCurrent();
            }
            else if (_currentLevel != MenuLevel.Main)
            {
                _currentLevel = MenuLevel.Main;
                _selectedIndex = 0;
                ScreenReader.Say("Back to Main Help.");
                AnnounceCurrent();
            }
            else
            {
                var help = Object.FindObjectOfType<HelpScreen>();
                if (help != null) help.Close();
                
                // Return to appropriate state based on scene
                string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (scene == "Home") AccessStateManager.TryEnter(AccessStateManager.State.Home);
                else if (scene == "Barracks") AccessStateManager.TryEnter(AccessStateManager.State.Barracks);
                else AccessStateManager.Exit(AccessStateManager.State.Help);

                ScreenReader.Say("Exiting Help.");
            }
        }

        private void AnnounceCurrent()
        {
            if (_currentLevel == MenuLevel.Main)
            {
                ScreenReader.Say($"{_mainMenuItems[_selectedIndex]}, {_selectedIndex + 1} of 2.");
                return;
            }

            var list = (_currentLevel == MenuLevel.Manual) ? _manualTopics : _tutorialTopics;
            if (list.Count == 0) { ScreenReader.Say("Category is empty."); return; }

            var topic = list[_selectedIndex];
            if (_viewingContent) ScreenReader.Say($"{topic.Title}: {topic.Content}");
            else ScreenReader.Say($"{topic.Title}, {_selectedIndex + 1} of {list.Count}.");
        }

        private string StripFormatting(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "<.*?>", string.Empty).Replace("\n", " ").Trim();
        }
    }
}
