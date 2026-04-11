using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GladiatorManagerAccess
{
    /// <summary>
    /// Handles the summary screen at the end of each week.
    /// Lists training results, money income, and events.
    /// </summary>
    public class WeekEndHandler : IAccessibleHandler
    {
        private Calendar _calendar;
        private bool _initialStateCaptured;
        private bool _summaryReady;

        private int _initialMoney;
        private int _initialSalaries;
        private int _initialGate;
        private int _initialBounties;
        private int _initialArmour;
        private int _initialFees;

        private Dictionary<int, GladiatorStats> _initialStats = new Dictionary<int, GladiatorStats>();
        private List<string> _summaryLines = new List<string>();
        private int _currentLineIndex = 0;

        public static WeekEndHandler Instance { get; private set; }

        public WeekEndHandler()
        {
            Instance = this;
        }

        private struct GladiatorStats
        {
            public string Name;
            public int Strength;
            public int Agility;
            public int Initiative;
            public int Toughness;
            public int Discipline;
            public int Sword;
            public int Bravery;
            public int Recovery;
            public int Speed;
            public int Stamina;
            public int Leadership;
            public int Potential;
        }

        public void Update()
        {
            if (AccessStateManager.IsIn(AccessStateManager.State.WeekEndSummary))
            {
                ProcessInput();
                // If we exited the state during input processing, don't do anything else this frame
                if (!AccessStateManager.IsIn(AccessStateManager.State.WeekEndSummary)) return;
            }

            if (_calendar == null)
            {
                _calendar = UnityEngine.Object.FindObjectOfType<Calendar>();
                if (_calendar == null) return;
            }

            // Monitor week processing progress
            if (Calendar.startProcessingWeek)
            {
                // Fallback: if not captured at scene load, capture at step 1 or earlier
                if (!_initialStateCaptured && Calendar.progress <= 1)
                {
                    CaptureInitialState();
                }

                // Trigger at 19, which is the last step before completion in the game's Calendar.Update
                if (Calendar.progress >= 19 && !_summaryReady)
                {
                    CaptureFinalState();
                    _summaryReady = true;

                    if (AccessStateManager.TryEnter(AccessStateManager.State.WeekEndSummary))
                    {
                        // We no longer set battleResultsScreen = true here.
                        // Instead, the Harmony patch CalendarUpdatePausePatch will block the game's Update
                        // from finishing as long as we are in the WeekEndSummary state.
                        _currentLineIndex = 0;
                        AnnounceCurrentLine();
                        DebugLogger.LogState("Week summary triggered and game paused via patch at progress " + Calendar.progress);
                    }
                }
            }
            else
            {
                // Reset for next week when we are back on Home screen and not processing
                if (_summaryReady)
                {
                    _initialStateCaptured = false;
                    _summaryReady = false;
                }

                if (AccessStateManager.IsIn(AccessStateManager.State.WeekEndSummary))
                {
                    AccessStateManager.Exit(AccessStateManager.State.WeekEndSummary);
                }
            }
        }

        private void ProcessInput()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // Exiting the state will allow the Harmony patch to return true,
                // letting the game finish step 19 and go back to Home.
                AccessStateManager.Exit(AccessStateManager.State.WeekEndSummary);
                ScreenReader.Stop();
                DebugLogger.LogState("Week summary dismissed by user. Allowing game to finish processing.");
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (_currentLineIndex < _summaryLines.Count - 1)
                {
                    _currentLineIndex++;
                    AnnounceCurrentLine();
                }
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (_currentLineIndex > 0)
                {
                    _currentLineIndex--;
                    AnnounceCurrentLine();
                }
            }
        }

        public string GetHelpText()
        {
            return Loc.Get("week_summary_help", "Week summary. Use Up and Down arrows to read. Press Space or Enter to continue.");
        }

        public void CaptureInitialState()
        {
            Team playerTeam = DataManager.allTheTeams.Find(x => x.PlayerTeam);
            if (playerTeam == null) return;

            _initialMoney = playerTeam.Money;
            _initialSalaries = playerTeam.SalaryCostThisYear;
            _initialGate = playerTeam.GateReceiptsThisYear;
            _initialBounties = playerTeam.KillBonusThisYear;
            _initialArmour = playerTeam.ArmourCostsThisYear;
            _initialFees = playerTeam.SigningFeesThisYear;

            _initialStats.Clear();

            foreach (var glad in DataManager.allThePGladiators.Where(x => x.Recruited && x.Alive))
            {
                _initialStats[glad.ID] = new GladiatorStats
                {
                    Name = glad.FirstName + " " + glad.Surname,
                    Strength = glad.Strength,
                    Agility = glad.Agility,
                    Initiative = glad.Initiative,
                    Toughness = glad.Toughness,
                    Discipline = glad.Discipline,
                    Sword = glad.Sword,
                    Bravery = glad.Bravery,
                    Recovery = glad.Recovery,
                    Speed = glad.Speed,
                    Stamina = glad.Stamina,
                    Leadership = glad.Leadership,
                    Potential = glad.PotAbility
                };
            }
            DebugLogger.LogState("Captured initial state for week processing.");
        }

        private void CaptureFinalState()
        {
            _summaryLines.Clear();
            Team playerTeam = DataManager.allTheTeams.Find(x => x.PlayerTeam);

            _summaryLines.Add(Loc.Get("week_summary_title", "Week End Summary"));
            
            // Training
            _summaryLines.Add(Loc.Get("week_summary_training", "Training Results:"));
            bool anyGains = false;

            foreach (var glad in DataManager.allThePGladiators.Where(x => x.Recruited && x.Alive))
            {
                if (_initialStats.TryGetValue(glad.ID, out var initial))
                {
                    List<string> gains = new List<string>();
                    if (glad.Strength > initial.Strength) gains.Add(Loc.Get("stat_str", "Strength") + " +" + (glad.Strength - initial.Strength));
                    if (glad.Agility > initial.Agility) gains.Add(Loc.Get("stat_agi", "Agility") + " +" + (glad.Agility - initial.Agility));
                    if (glad.Initiative > initial.Initiative) gains.Add(Loc.Get("stat_ini", "Initiative") + " +" + (glad.Initiative - initial.Initiative));
                    if (glad.Toughness > initial.Toughness) gains.Add(Loc.Get("stat_tou", "Toughness") + " +" + (glad.Toughness - initial.Toughness));
                    if (glad.Discipline > initial.Discipline) gains.Add(Loc.Get("stat_dis", "Discipline") + " +" + (glad.Discipline - initial.Discipline));
                    if (glad.Sword > initial.Sword) gains.Add(Loc.Get("stat_swo", "Weapon Skill") + " +" + (glad.Sword - initial.Sword));
                    if (glad.Bravery > initial.Bravery) gains.Add(Loc.Get("stat_bra", "Bravery") + " +" + (glad.Bravery - initial.Bravery));
                    if (glad.Recovery > initial.Recovery) gains.Add(Loc.Get("stat_rec", "Recovery") + " +" + (glad.Recovery - initial.Recovery));
                    if (glad.Speed > initial.Speed) gains.Add(Loc.Get("stat_spe", "Speed") + " +" + (glad.Speed - initial.Speed));
                    if (glad.Stamina > initial.Stamina) gains.Add(Loc.Get("stat_sta", "Stamina") + " +" + (glad.Stamina - initial.Stamina));
                    if (glad.Leadership > initial.Leadership) gains.Add(Loc.Get("stat_lea", "Leadership") + " +" + (glad.Leadership - initial.Leadership));
                    
                    if (glad.PotAbility > initial.Potential)
                    {
                        gains.Add(Loc.Get("stat_potential_gain", "Potential +{0}", glad.PotAbility - initial.Potential));
                    }

                    if (gains.Count > 0)
                    {
                        anyGains = true;
                        _summaryLines.Add(initial.Name + ": " + string.Join(", ", gains));
                    }
                }
            }

            if (!anyGains)
            {
                _summaryLines.Add(Loc.Get("week_summary_no_gains", "No significant attribute gains this week."));
            }

            // Events and News
            bool hasNews = !string.IsNullOrEmpty(Calendar.newsTextTextUrgent) || 
                          !string.IsNullOrEmpty(Calendar.newsTextTextPlayer) ||
                          !string.IsNullOrEmpty(Calendar.newsTextTextPlayerTraining) ||
                          !string.IsNullOrEmpty(Calendar.newsTextTextOther);

            if (hasNews)
            {
                _summaryLines.Add(Loc.Get("week_summary_news", "News and Events:"));
                if (!string.IsNullOrEmpty(Calendar.newsTextTextUrgent))
                    _summaryLines.Add(CleanText(Calendar.newsTextTextUrgent));
                if (!string.IsNullOrEmpty(Calendar.newsTextTextPlayer))
                    _summaryLines.Add(CleanText(Calendar.newsTextTextPlayer));
                if (!string.IsNullOrEmpty(Calendar.newsTextTextPlayerTraining))
                    _summaryLines.Add(CleanText(Calendar.newsTextTextPlayerTraining));
                if (!string.IsNullOrEmpty(Calendar.newsTextTextOther))
                    _summaryLines.Add(CleanText(Calendar.newsTextTextOther));
            }

            // League Results
            _summaryLines.Add(Loc.Get("week_summary_league_results", "League Results:"));
            
            bool anyLeagueResults = false;
            // The game has 5 leagues, each with 5 matches per week.
            // Indices in FightSimulator arrays:
            // teamsTextAcrossLeagues: 1-indexed, 2 entries per match (winner/loser). Total 50 entries.
            // resultWordTextAcrossLeagues: 1-indexed, 1 entry per match. Total 25 entries.
            // gladiatorsTextAcrossLeagues: 1-indexed, 2 entries per match. Total 50 entries.
            
            // Check if results are populated (using a sentinel check similar to LeaguePopulator)
            if (FightSimulator.teamsTextAcrossLeagues != null && 
                FightSimulator.teamsTextAcrossLeagues.Length > 10 && 
                !string.IsNullOrEmpty(FightSimulator.teamsTextAcrossLeagues[10]))
            {
                for (int leagueIdx = 0; leagueIdx < 5; leagueIdx++)
                {
                    for (int matchIdx = 1; matchIdx <= 5; matchIdx++)
                    {
                        int resultIdx = leagueIdx * 5 + matchIdx;
                        int teamBaseIdx = leagueIdx * 10 + ((matchIdx - 1) * 2 + 1);
                        
                        string winner = CleanText(FightSimulator.teamsTextAcrossLeagues[teamBaseIdx]);
                        string loser = CleanText(FightSimulator.teamsTextAcrossLeagues[teamBaseIdx + 1]);
                        string resultWord = CleanText(FightSimulator.resultWordTextAcrossLeagues[resultIdx]);
                        string winGlad = CleanText(FightSimulator.gladiatorsTextAcrossLeagues[teamBaseIdx]);
                        string loseGlad = CleanText(FightSimulator.gladiatorsTextAcrossLeagues[teamBaseIdx + 1]);
                        
                        if (!string.IsNullOrEmpty(winner))
                        {
                            anyLeagueResults = true;
                            _summaryLines.Add($"{winner} {resultWord} {loser} ({winGlad} vs {loseGlad})");
                        }
                    }
                }
            }

            if (!anyLeagueResults)
            {
                _summaryLines.Add(Loc.Get("week_summary_no_league_results", "No league results this week."));
            }

            _summaryLines.Add(Loc.Get("week_summary_continue", "Press Space or Enter to continue."));
            DebugLogger.LogState("Calculated week summary.");
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            // Remove Unity rich text tags
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        }

        private void AnnounceCurrentLine()
        {
            if (_currentLineIndex >= 0 && _currentLineIndex < _summaryLines.Count)
            {
                string text = _summaryLines[_currentLineIndex];
                if (string.IsNullOrEmpty(text)) text = "..."; // For spacers
                ScreenReader.Say(text);
            }
        }
    }
}
