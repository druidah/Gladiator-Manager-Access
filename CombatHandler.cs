using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MelonLoader;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace GladiatorManagerAccess
{
    public class CombatHandler : IAccessibleHandler
    {
        private bool _wasOpen = false;
        private bool _wasResults = false;
        private int _selectedSlot = 0; // 0-5
        private int _placementSlot = 0; // 0-5 for placement phase
        private int _swapSourceSlot = -1; // -1 if no slot selected for swap
        private bool _isPlacing = false;
        
        private List<string> _battleLog = new List<string>();
        private List<string> _rollsLog = new List<string>();
        private List<string> _statsItems = new List<string>();
        
        private int _logIndex = 0;
        private int _statsIndex = 0;
        private bool _viewingLog = false;
        private bool _viewingRolls = false;
        private bool _viewingStats = false;
        
        private List<string> _availableActions = new List<string>();
        
        private bool _isTargetingBodyPart = false;
        private int _targetBodyPartIdx = 0;
        private readonly string[] _bodyParts = { "head", "torso", "left arm", "right arm", "left leg", "right leg", "left hand", "right hand", "left foot", "right foot" };

        private Queue<string> _recentLogMessages = new Queue<string>(10);
        private int _lastGlobalTurn = -1;
        private HashSet<string> _roundSeenMessages = new HashSet<string>();
        private Dictionary<string, string> _roundSeenSkeletons = new Dictionary<string, string>();

        // Cache for stats to survive scene transitions
        private struct GladStat { public string Name; public bool Alive; public int Damage; public int Hits; public int Injuries; public int XP; }
        private List<GladStat> _playerCache = new List<GladStat>();
        private List<GladStat> _enemyCache = new List<GladStat>();
        private bool _wonCache;
        private string _winTypeCache;
        private int _gateCache;
        private int _bountyCache;

        public CombatHandler()
        {
            CombatPatches.OnMessageReceived += OnMessageReceived;
            CombatPatches.OnFightEnd += CaptureStats;
        }

        public string GetHelpText()
        {
            if (_viewingLog) return "Battle Log: Use Up and Down arrows to scroll through events. Press Escape to exit.";
            if (_viewingRolls) return "Detailed Log: Use Up and Down arrows to scroll. Press Escape to exit.";
            if (_viewingStats) return "Combat Statistics: Use Up and Down arrows to read data. Escape to exit menu.";

            if (IsResultsActive())
            {
                return "Results Screen: Ctrl + 1 to 6 for fighter stats, Alt + 1 to 6 for enemy stats. F3 for Battle Log. Use Up/Down arrows to read through data. Press Enter to return to the Home screen.";
            }
            
            return "Combat: Arrows to navigate. Ctrl+Arrows to target. Ctrl + 1 to 6 for fighter stats, Alt + 1 to 6 for enemy stats. F3 Battle Log. F4 Quick Sim. Shift+Escape to yield. Space to advance time.";
        }

        private bool IsResultsActive()
        {
            bool musicRes = MusicPlayer.battleResultsScreen || MusicPlayer.teamResultsScreen;
            bool scriptRes = Object.FindObjectOfType<BattleReportScript>() != null;
            
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp == null) return musicRes || scriptRes;
            
            bool exitRes = fp.exitButton != null && fp.exitButton.interactable && fp.exitButtonText != null && fp.exitButtonText.text.Contains("Exit");
            bool textRes = fp.continueText != null && fp.continueText.text.Contains("Fight Over");

            bool final = musicRes || scriptRes || exitRes || textRes;
            if (final && !_wasResults && Main.DebugMode)
            {
                DebugLogger.LogState($"Results detected: Music={musicRes}, Script={scriptRes}, Exit={exitRes}, Text={textRes}");
            }
            return final;
        }

        public void Update()
        {
            bool resultsActive = IsResultsActive();
            var fp = Object.FindObjectOfType<FightProcessor>();
            bool meleeActive = fp != null && fp.meleeScreen && !resultsActive;

            // Detect Placement phase - strictly only if the game button says "Confirm Placement"
            bool placingNow = meleeActive && fp.continueText != null && fp.continueText.text == "Confirm Placement";
            
            if (placingNow && !_isPlacing)
            {
                _isPlacing = true;
                _placementSlot = 0;
                _swapSourceSlot = -1;
                ScreenReader.SayQueued("Placement Mode. Use Left and Right arrows to navigate slots. Space to pick up or swap gladiators. Enter to confirm placement.");
                AnnouncePlacementSlot();
            }
            else if (!placingNow && _isPlacing)
            {
                _isPlacing = false;
            }

            if ((meleeActive || resultsActive) && !_wasOpen)
            {
                OnOpen(resultsActive);
                _wasOpen = true;
                _wasResults = resultsActive;
                AccessStateManager.TryEnter(AccessStateManager.State.Combat);
            }
            else if (!(meleeActive || resultsActive) && _wasOpen)
            {
                _wasOpen = false;
                _wasResults = false;
            }

            if (!AccessStateManager.IsIn(AccessStateManager.State.Combat)) return;

            if (resultsActive && !_wasResults)
            {
                _wasResults = true;
                _viewingStats = true;
                _viewingLog = false;
                _viewingRolls = false;
                _statsIndex = 0;
                ScreenReader.SayQueued("Fight Over. Results available. Press Enter to exit.");
                BuildStatsItems();
            }

            if (resultsActive)
            {
                HandleResults();
            }
            else if (meleeActive)
            {
                if (_isPlacing) ProcessPlacementInput();
                else ProcessInput();
            }
        }

        private void ProcessPlacementInput()
        {
            if (ProcessUnitStatsInput()) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow)) { _placementSlot = Mathf.Max(0, _placementSlot - 1); AnnouncePlacementSlot(); }
            else if (Input.GetKeyDown(KeyCode.RightArrow)) { _placementSlot = Mathf.Min(5, _placementSlot + 1); AnnouncePlacementSlot(); }
            else if (Input.GetKeyDown(KeyCode.Space)) PerformPlacementSwap();
            else if (Input.GetKeyDown(KeyCode.F4)) HandleQuickSim();
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                var fp = Object.FindObjectOfType<FightProcessor>();
                if (fp != null)
                {
                    ScreenReader.Say("Placement confirmed.");
                    fp.ContinueButton();
                }
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    EnterLog(shift);
                }
                else AnnouncePlayerInSlot();
            }
        }

        private void AnnouncePlayerInSlot(int slot = -1)
        {
            if (slot == -1) slot = _placementSlot;
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp == null) return;

            // Determine if we are in placement or actual combat
            bool placingNow = fp.meleeScreen && fp.continueText != null && fp.continueText.text == "Confirm Placement";

            if (placingNow)
            {
                int gladIdx = SafeGetGladIntFromSlot(fp, slot, true);
                if (gladIdx == 1000) ScreenReader.Say($"Slot {slot + 1} is empty.");
                else
                {
                    var g = FightProcessor.currentPlayerGladiator[gladIdx];
                    ScreenReader.Say($"Player in Slot {slot + 1}: {FormatGladiatorStats(g, true)}");
                }
            }
            else
            {
                // Actual combat status by slot
                int gladIdx = -1;
                for (int i = 0; i < 6; i++)
                {
                    if (FightProcessor.pAlive[i] && FightProcessor.pGladPosx[i] == slot)
                    {
                        gladIdx = i;
                        break;
                    }
                }

                if (gladIdx == -1) ScreenReader.Say($"Slot {slot + 1} is empty or fighter has fallen.");
                else
                {
                    var g = FightProcessor.currentPlayerGladiator[gladIdx];
                    string name = g.FirstName;
                    int blood = FightProcessor.bloodPlayer[gladIdx];
                    int energy = FightProcessor.pEnergy[gladIdx];
                    int morale = FightProcessor.moraleDisplayPlayer[gladIdx];
                    string state = FightProcessor.currentStatePlayer[gladIdx];
                    string order = FightProcessor.playerAct[gladIdx];
                    
                    string surroundings = "";
                    for (int e = 0; e < 6; e++)
                    {
                        if (FightProcessor.eAlive[e] && FightProcessor.eGladPosx[e] == slot)
                        {
                            string eName = FightProcessor.currentEnemyGladiator[e].FirstName;
                            surroundings += $"Engaged with {eName}. ";
                        }
                    }

                    ScreenReader.Say($"{name} in Slot {slot + 1}. {surroundings}{blood}% health, {energy}% energy, {morale}% control. {state}. Order: {order}.");
                }
            }
        }

        private void AnnounceEnemyInSlot(int slot = -1)
        {
            if (slot == -1) slot = _placementSlot;
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp == null) return;

            // Determine if we are in placement or actual combat
            bool placingNow = fp.meleeScreen && fp.continueText != null && fp.continueText.text == "Confirm Placement";

            if (placingNow)
            {
                int enemyIdx = SafeGetGladIntFromSlot(fp, slot, false);
                if (enemyIdx == 1000) ScreenReader.Say($"No enemy in slot {slot + 1}.");
                else
                {
                    var eg = FightProcessor.currentEnemyGladiator[enemyIdx];
                    ScreenReader.Say($"Enemy in Slot {slot + 1}: {FormatGladiatorStats(eg, false)}");
                }
            }
            else
            {
                // Actual combat status for enemy
                int enemyIdx = -1;
                for (int i = 0; i < 6; i++)
                {
                    if (FightProcessor.eAlive[i] && FightProcessor.eGladPosx[i] == slot)
                    {
                        enemyIdx = i;
                        break;
                    }
                }

                if (enemyIdx == -1) ScreenReader.Say($"No enemy in slot {slot + 1}.");
                else
                {
                    var eg = FightProcessor.currentEnemyGladiator[enemyIdx];
                    string name = eg.FirstName;
                    int blood = FightProcessor.bloodEnemy[enemyIdx];
                    int energy = FightProcessor.eEnergy[enemyIdx];
                    int morale = FightProcessor.moraleDisplayEnemy[enemyIdx];
                    string state = FightProcessor.currentStateEnemy[enemyIdx];

                    string surroundings = "";
                    for (int p = 0; p < 6; p++)
                    {
                        if (FightProcessor.pAlive[p] && FightProcessor.pGladPosx[p] == slot)
                        {
                            string pName = FightProcessor.currentPlayerGladiator[p].FirstName;
                            surroundings += $"Engaged with {pName}. ";
                        }
                    }

                    ScreenReader.Say($"Enemy {name} in Slot {slot + 1}. {surroundings}{blood}% health, {energy}% energy, {morale}% control. {state}.");
                }
            }
        }

        private int SafeGetGladIntFromSlot(FightProcessor fp, int slot, bool player)
        {
            if (fp == null || slot < 0 || slot >= 6) return 1000;
            bool active = player ? fp.pGladSlotActive[slot] : fp.eGladSlotActive[slot];
            if (!active) return 1000;
            return fp.GetGladIntFromSlot(slot, player, false);
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
                info += $"Str {g.Strength}, Agi {g.Agility}, Skill {g.Sword}, Tou {g.Toughness}, Bra {g.Bravery}, Lea {g.Leadership}, Ini {g.Initiative}, Spe {g.Speed}, Sta {g.Stamina}, Rec {g.Recovery}, Dis {g.Discipline}.";
            }
            else
            {
                var v = DataManager.allTheEVisStats.Find(x => x.ID == g.ID);
                if (v == null)
                {
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

        private void AnnouncePlacementSlot()
        {
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp == null) return;

            int gladIdx = SafeGetGladIntFromSlot(fp, _placementSlot, true);
            string content = "Empty";
            if (gladIdx != 1000)
            {
                var g = FightProcessor.currentPlayerGladiator[gladIdx];
                content = $"{g.FirstName} ({g.Class})";
            }

            // Check for enemies in this slot
            int enemyIdx = SafeGetGladIntFromSlot(fp, _placementSlot, false);
            string enemyInfo = "";
            if (enemyIdx != 1000)
            {
                var eg = FightProcessor.currentEnemyGladiator[enemyIdx];
                enemyInfo = $". Facing Enemy {eg.FirstName} ({eg.Class})";
            }

            string status = (_swapSourceSlot == _placementSlot) ? " (Selected for swap)" : "";
            ScreenReader.Say($"Slot {_placementSlot + 1}, {content}{enemyInfo}{status}.");
        }

        private void PerformPlacementSwap()
        {
            if (_swapSourceSlot == -1)
            {
                _swapSourceSlot = _placementSlot;
                ScreenReader.Say("Selected slot for swap. Navigate to another slot and press Space again.");
            }
            else
            {
                if (_swapSourceSlot == _placementSlot)
                {
                    _swapSourceSlot = -1;
                    ScreenReader.Say("Swap canceled.");
                }
                else
                {
                    var fp = Object.FindObjectOfType<FightProcessor>();
                    if (fp == null) return;

                    int glad1 = SafeGetGladIntFromSlot(fp, _swapSourceSlot, true);
                    int glad2 = SafeGetGladIntFromSlot(fp, _placementSlot, true);

                    if (glad1 != 1000) FightProcessor.pGladPosx[glad1] = _placementSlot;
                    if (glad2 != 1000) FightProcessor.pGladPosx[glad2] = _swapSourceSlot;

                    // Swap active flags manually to ensure UI sync is correct
                    bool tempActive = fp.pGladSlotActive[_swapSourceSlot];
                    fp.pGladSlotActive[_swapSourceSlot] = fp.pGladSlotActive[_placementSlot];
                    fp.pGladSlotActive[_placementSlot] = tempActive;

                    // Sync UI
                    var sa = Object.FindObjectOfType<SlotAssignor>();
                    if (sa != null)
                    {
                        fp.DetermineTargets();
                        sa.RefreshCurrentView();
                        
                        // Sync visual highlights and cards
                        SlotAssignor.highlightSlotPlayer = _placementSlot;
                        sa.HighlightThisCard(_placementSlot);
                        if (glad1 != 1000) sa.SetUpSlots(glad1, _placementSlot);
                        if (glad2 != 1000) sa.SetUpSlots(glad2, _swapSourceSlot);
                        sa.SetUpChoiceSlots();
                        sa.ShowArrows();
                    }

                    _swapSourceSlot = -1;
                    ScreenReader.Say("Gladiators swapped.");
                    AnnouncePlacementSlot();
                }
            }
        }

        private void OnOpen(bool isResults)
        {
            if (isResults)
            {
                ScreenReader.SayQueued("Battle Results Screen. Use Up and Down arrows to read detailed statistics.");
                _viewingStats = true;
                _statsIndex = 0;
                BuildStatsItems();
            }
            else
            {
                _battleLog.Clear();
                _rollsLog.Clear();
                _recentLogMessages.Clear();
                _viewingLog = false;
                _viewingRolls = false;
                _viewingStats = false;
                
                _selectedSlot = 0;
                Populator.tabID = _selectedSlot;
                MelonCoroutines.Start(DelayedActionRefresh(false));
            }
        }

        private void CaptureStats()
        {
            _playerCache.Clear();
            _enemyCache.Clear();
            _wonCache = FightSimulator.actualPlayerWon;
            _winTypeCache = FightSimulator.actualPlayerTypeOfWin;

            for (int i = 0; i < 6; i++)
            {
                var g = FightProcessor.currentPlayerGladiator[i];
                if (g != null)
                {
                    int xp = 0;
                    if (i < FightSimulator.APpExp.Length) xp = FightSimulator.APpExp[i];
                    _playerCache.Add(new GladStat { 
                        Name = $"{g.FirstName} {g.Surname}", 
                        Alive = FightProcessor.pAlive[i], 
                        Damage = FightProcessor.pTotalDamage[i],
                        Hits = FightProcessor.pTotalHits[i],
                        Injuries = FightProcessor.pTotalInjuries[i],
                        XP = xp
                    });
                }

                var eg = FightProcessor.currentEnemyGladiator[i];
                if (eg != null)
                {
                    _enemyCache.Add(new GladStat { 
                        Name = $"{eg.FirstName} {eg.Surname}", 
                        Alive = FightProcessor.eAlive[i], 
                        Damage = FightProcessor.eTotalDamage[i],
                        Hits = FightProcessor.eTotalHits[i],
                        Injuries = FightProcessor.eTotalInjuries[i],
                        XP = 0
                    });
                }
            }

            var dataManager = Object.FindObjectOfType<DataManager>();
            if (dataManager != null) dataManager.RefreshSaveData();
            var saveData = DataManager.allTheSaveData?.Find(x => x.SaveID == 1);
            _gateCache = saveData != null ? saveData.PGateReceipts : 0;
            _bountyCache = Calendar.playerBountiesThisWeek;
            
            DebugLogger.LogState($"Stats captured. Player won: {_wonCache}");
        }

        private void BuildStatsItems()
        {
            // If cache is empty but we are in results, try one last time
            if (_playerCache.Count == 0) CaptureStats();

            _statsItems.Clear();
            _statsItems.Add("--- Battle Outcome ---");
            _statsItems.Add(_wonCache ? $"Victory! You won by {_winTypeCache}." : $"Defeat. The enemy won by {_winTypeCache}.");
            _statsItems.Add($"Total Money Earned: {_gateCache + _bountyCache} gold (Gate: {_gateCache} gold, Bounties: {_bountyCache} gold).");

            _statsItems.Add("--- Your Team Performance ---");
            foreach (var stat in _playerCache)
            {
                string status = stat.Alive ? "Survived" : "Fallen";
                _statsItems.Add($"{stat.Name} ({status}): {stat.Hits} hits, {stat.Damage} damage dealt, {stat.Injuries} injuries caused. XP Gained: {stat.XP}.");
            }

            _statsItems.Add("--- Enemy Team Performance ---");
            foreach (var stat in _enemyCache)
            {
                string status = stat.Alive ? "Survived" : "Fallen";
                _statsItems.Add($"{stat.Name} ({status}): {stat.Hits} hits, {stat.Damage} damage dealt, {stat.Injuries} injuries caused.");
            }
        }

        private void ProcessInput()
        {
            if (_viewingLog || _viewingRolls || _viewingStats)
            {
                ProcessLogInput();
                return;
            }

            if (ProcessUnitStatsInput()) return;

            // Correct detection for both controls
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (Input.GetKeyDown(KeyCode.LeftArrow)) NavigateUnits(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) NavigateUnits(1);
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (ctrl) CycleTarget(-1);
                else CycleAction(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (ctrl) CycleTarget(1);
                else CycleAction(1);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                EnterLog(shift);
            }
            else if (Input.GetKeyDown(KeyCode.F4)) HandleQuickSim();
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    var fp = Object.FindObjectOfType<FightProcessor>();
                    if (fp != null && fp.exitButton != null && fp.exitButton.interactable && fp.exitButtonText != null && fp.exitButtonText.text == "Yield")
                    {
                        ScreenReader.Say("Yielding entire team.");
                        fp.exitButton.onClick.Invoke();
                    }
                    else
                    {
                        ScreenReader.Say("Yield not available yet. No injuries sustained.");
                    }
                }
                else if (_isTargetingBodyPart)
                {
                    _isTargetingBodyPart = false;
                    ScreenReader.Say("Canceled targeting.");
                    AnnounceCurrentUnit();
                }
            }
        }

        private bool ProcessUnitStatsInput()
        {
            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                    bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                    if (shift)
                    {
                        if (alt) AnnounceEnemyDetails(i);
                        else AnnouncePlayerDetails(i);
                        return true;
                    }
                    else if (ctrl)
                    {
                        AnnouncePlayerInSlot(i);
                        return true;
                    }
                    else if (alt)
                    {
                        AnnounceEnemyInSlot(i);
                        return true;
                    }
                }
            }
            return false;
        }

        private void AnnouncePlayerDetails(int slot)
        {
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp == null) return;

            int gladIdx = SafeGetGladIntFromSlot(fp, slot, true);
            if (gladIdx == 1000) { ScreenReader.Say($"Slot {slot + 1} is empty."); return; }

            var g = FightProcessor.currentPlayerGladiator[gladIdx];
            string armour = UIUtilities.GetArmourStatus(g.ID, true, gladIdx);
            string injuries = UIUtilities.GetInjuryDetails(g.ID, true);
            
            ScreenReader.Say($"{g.FirstName} Status: {armour} {injuries}");
        }

        private void AnnounceEnemyDetails(int slot)
        {
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp == null) return;

            int enemyIdx = SafeGetGladIntFromSlot(fp, slot, false);
            if (enemyIdx == 1000) { ScreenReader.Say($"No enemy in slot {slot + 1}."); return; }

            var eg = FightProcessor.currentEnemyGladiator[enemyIdx];
            string armour = UIUtilities.GetArmourStatus(eg.ID, false, enemyIdx);
            string injuries = UIUtilities.GetInjuryDetails(eg.ID, false);
            
            ScreenReader.Say($"Enemy {eg.FirstName} Status: {armour} {injuries}");
        }

        private void HandleQuickSim()
        {
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp != null && fp.quickSimButton != null && fp.quickSimButton.interactable)
            {
                ScreenReader.Say("Quick Simulating the rest of the battle.");
                DebugLogger.LogState("F4 pressed: Triggering Quick Sim.");
                fp.quickSimButton.onClick.Invoke();
            }
            else
            {
                ScreenReader.Say("Quick Sim not available.");
            }
        }

        private void ProcessLogInput()
        {
            List<string> currentList = _battleLog;
            if (_viewingRolls) currentList = _rollsLog;
            else if (_viewingStats) currentList = _statsItems;

            int currentIdx = _viewingStats ? _statsIndex : _logIndex;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                currentIdx = Mathf.Max(0, currentIdx - 1);
                if (_viewingStats) _statsIndex = currentIdx; else _logIndex = currentIdx;
                AnnounceLogEntry();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                currentIdx = Mathf.Min(currentList.Count - 1, currentIdx + 1);
                if (_viewingStats) _statsIndex = currentIdx; else _logIndex = currentIdx;
                AnnounceLogEntry();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                _viewingLog = false;
                _viewingRolls = false;
                _viewingStats = false;
                ScreenReader.Say("Exiting menu.");
                if (!IsResultsActive()) AnnounceCurrentUnit();
            }
        }

        private void EnterLog(bool full)
        {
            var log = full ? _rollsLog : _battleLog;
            if (log.Count == 0) { ScreenReader.Say("Log is empty."); return; }
            _viewingLog = !full;
            _viewingRolls = full;
            _viewingStats = false;
            _logIndex = log.Count - 1;
            ScreenReader.Say(full ? "Detailed Log." : "Battle Log.");
            AnnounceLogEntry();
        }

        private void AnnounceLogEntry()
        {
            List<string> currentList = _battleLog;
            int currentIdx = _logIndex;
            if (_viewingRolls) currentList = _rollsLog;
            else if (_viewingStats) { currentList = _statsItems; currentIdx = _statsIndex; }

            if (currentIdx >= 0 && currentIdx < currentList.Count) 
            {
                ScreenReader.Say(currentList[currentIdx]);
            }
        }

        private void NavigateUnits(int direction)
        {
            int next = _selectedSlot;
            int count = 0;
            do {
                next = (next + direction + 6) % 6;
                count++;
                if (FightProcessor.pAlive[next])
                {
                    _selectedSlot = next;
                    _isTargetingBodyPart = false;
                    
                    var pop = Object.FindObjectOfType<Populator>();
                    if (pop != null)
                    {
                        pop.ShowGladiatorOnMouseOver(_selectedSlot + 4);
                    }
                    
                    MelonCoroutines.Start(DelayedActionRefresh(true));
                    return;
                }
            } while (count < 6);
        }

        private int GetGridIndexFromSlot(int slot)
        {
            for (int i = 0; i < FightProcessor.pGladPosx.Length; i++)
                if (FightProcessor.pGladPosx[i] == slot) return i + 1;
            return slot + 1;
        }

        private System.Collections.IEnumerator DelayedActionRefresh(bool announceUnit)
        {
            yield return new WaitForSeconds(0.1f);
            RefreshAvailableActions();
            if (announceUnit) AnnounceCurrentUnit();
        }

        private void RefreshAvailableActions()
        {
            _availableActions.Clear();
            var sa = Object.FindObjectOfType<SlotAssignor>();
            if (sa == null) return;

            // Force refresh of the static actionIconsString for the currently selected gladiator
            if (_selectedSlot >= 0 && _selectedSlot < 6)
            {
                int gridPos = FightProcessor.pGladPosx[_selectedSlot];
                sa.SetUpSlots(_selectedSlot, gridPos);
            }

            var field = typeof(SlotAssignor).GetField("actionIconsString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field != null)
            {
                string[] actions = (string[])field.GetValue(sa);
                if (actions != null)
                {
                    foreach (var a in actions)
                        if (!string.IsNullOrEmpty(a) && a != "Blank" && a != "Symbol")
                            if (!_availableActions.Contains(a)) _availableActions.Add(a);
                }
            }
            if (!_availableActions.Contains("Rest")) _availableActions.Add("Rest");
        }

        private void CycleAction(int direction)
        {
            var sa = Object.FindObjectOfType<SlotAssignor>();
            if (sa == null) return;

            RefreshAvailableActions();
            int count = _availableActions.Count;
            if (count == 0) return;

            string currentOrder = FightProcessor.playerAct[_selectedSlot];
            int currentIdx = _availableActions.IndexOf(currentOrder);
            if (currentIdx == -1) currentIdx = 0;

            int nextIdx = (currentIdx + direction + count) % count;
            string nextOrder = _availableActions[nextIdx];
            
            FightProcessor.playerAct[_selectedSlot] = nextOrder;
            int gridPos = FightProcessor.pGladPosx[_selectedSlot];
            sa.SetUpThisChoiceSlotByFightProcesserPlayerAct(gridPos);
            
            var selectors = Object.FindObjectsOfType<ActionSelector>();
            foreach (var sel in selectors)
            {
                if (sel.playerSelector != null)
                {
                    for (int i = 0; i < sel.playerSelector.options.Count; i++)
                    {
                        if (sel.playerSelector.options[i].text == nextOrder)
                        {
                            sel.playerSelector.value = i;
                            sel.actionSelecting(true);
                            break;
                        }
                    }
                }
            }
            MelonCoroutines.Start(DelayedDescriptionAnnouncement(nextOrder, nextIdx, count));
        }

        private System.Collections.IEnumerator DelayedDescriptionAnnouncement(string order, int idx, int total)
        {
            yield return new WaitForSeconds(0.05f);
            string description = ReportToggle.helpText;
            
            if (string.IsNullOrEmpty(description) || !description.StartsWith(order))
            {
                description = GetGameTooltipDescription(order);

                if (string.IsNullOrEmpty(description))
                {
                    description = GetManualDescription(order);
                }
            }
            else
            {
                description = description.Substring(order.Length).Trim();
                description = NormalizeDescriptionText(description);
            }

            ScreenReader.Say($"{order}, {description}.");
        }

        private string GetGameTooltipDescription(string order)
        {
            string originalActionType = DuloGames.UI.UISpellSlot.actionType;
            bool originalInAColumn = DuloGames.UI.UISpellSlot.inAColumn;
            string originalHelpDescription = DuloGames.UI.UISpellSlot.helpDescription;

            try
            {
                DuloGames.UI.UISpellSlot.actionType = order;
                DuloGames.UI.UISpellSlot.inAColumn = false;
                DuloGames.UI.UISpellSlot.helpDescription = string.Empty;
                DuloGames.UI.UISpellSlot.SetHelpInfo();

                return NormalizeDescriptionText(DuloGames.UI.UISpellSlot.helpDescription);
            }
            finally
            {
                DuloGames.UI.UISpellSlot.actionType = originalActionType;
                DuloGames.UI.UISpellSlot.inAColumn = originalInAColumn;
                DuloGames.UI.UISpellSlot.helpDescription = originalHelpDescription;
            }
        }

        private string NormalizeDescriptionText(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return string.Empty;
            }

            description = Regex.Replace(description, "<.*?>", string.Empty);
            description = description.Replace("\r\n", "\n");
            description = description.Replace("\n\n", ". ");
            description = description.Replace("\n", " ");
            description = Regex.Replace(description, "\\s+", " ").Trim();
            description = description.Replace(" .", ".").Replace("..", ".");

            return description;
        }

        private string GetManualDescription(string order)
        {
            switch (order)
            {
                case "Rest": return "Recovers base 12 energy. (+ 5 energy if not attacked). (normal recovery is +1/turn). (Modified by Stamina).";
                case "Yield": return "This fighter will yield. This causes the selected fighter alone to yield, leaving their team mates to fight on.";
                case "Charge": return "Charging happens after normal engaged attacks but before other movement. An effective charge relies on weapon skill and speed. A successful charge will give a +(0-10) modifier to damage, depending on strength. If both fighters connect on a charge there will be a +(0-16) modifier to damage. A missed charge will result in significant loss of control.";
                case "Engage": return "Engaging happens after attacks in the movement phase. The fighter will engage his opposite number. Once engaged gladiators can no longer move left and right without disengaging.";
                case "Left":
                case "Right": return "Movement happens after attacks. A fighter can only move if disengaged. A fighter can only swap places with another if both are disengaged.";
                case "Disengage": return "Disengage happens after attacks in the movement phase. An attempt to disengage will leave the fighter unable to positively defend attacks for the turn. A successful disengage will free the fighter to move on the following turn. A fighter must beat his opponent in a test of agility or strength to disengage.";
                case "Sword Cut":
                case "Cut": return "A cutting attack has a better hit chance than a thrust (+10 penalty to hit). A cut is 4x more likely to hit against a dodge than a parry. It does a base damage of 6 (Base 15 damage against armour).";
                case "Sword Thrust":
                case "Thrust": return "A thrusting attack has a worse hit chance than a cut (+20 penalty to hit). A thrust is 4x more likely to hit against a parry than a dodge. It does a base damage of 10 (Base 15 damage against armour).";
                case "Precise Strike": return "Special attack move. Becomes available when the fighter passes difficult control and weapon skill tests. Difficult to pull off, relying on high weapon skill. Must also find a gap in the armour to succeed. Delivers a dangerous injury that may kill instantly or cause serious bleeding.";
                case "Swing": return "A swing attack has a much better hit chance than a smash. (+5 penalty to hit). A swing is 4x more likely to hit against a dodge than a parry. It does a weak base damage of 5 (Strong base 12 damage against armour).";
                case "Smash": return "A smashing attack has a much worse hit chance than a swing (+25 penalty to hit). A smash is 4x more likely to hit against a parry than a dodge. More likely to hit the upper parts of the body, such as the head. It does a base damage of 10 (Strong base 12 damage against armour).";
                case "Crush": return "Special attack move. Becomes available when the fighter passes difficult control and weapon skill tests. Difficult to pull off, relying on high weapon skill and strength. More likely to hit the head. Successful crush delivers a very high base damage of 16.";
                case "Slash": return "A slash attack has a much better hit chance than a stab. (+2 penalty to hit). A slash is 4x more likely to hit against a dodge than a parry. It does a weak base damage of 5 (Very weak base 4 damage against armour).";
                case "Stab": return "A stabbing attack has a much worse hit chance than a slash (+10 penalty to hit). A stab is 4x more likely to hit against a parry than a dodge. It does a base damage of 10 (Very weak base 4 damage against armour).";
                case "Jab": return "A jab attack has a much better hit chance than a stab. (+3 penalty to hit). A jab is 4x more likely to hit against a parry than a dodge. It does a very weak base damage of 4 (Base 8 damage against armour).";
                case "Skewer": return "A skewer attack has a much worse hit chance than a jab (+25 penalty to hit). A skewer is 4x more likely to hit against a parry than a dodge. It does a high base damage of 14 (Base 8 damage against armour).";
                case "Prod": return "A prod attack has a much better hit chance than a pin. (+3 penalty to hit). A prod is 4x more likely to hit against a parry than a dodge. Very weak base damage of 3 (Strong base 12 against armour). Gives a +5 control bonus on hit.";
                case "Pin": return "A pin attack has a much worse hit chance than a prod (+25 penalty to hit). A pin is 4x more likely to hit against a parry than a dodge. Greater chance of hitting the lower body. Base damage of 10 (Strong base 12 against armour). Gives a +5 control bonus on hit.";
                case "Pierce": return "Special attack move. Becomes available when the fighter passes difficult control and weapon skill tests. Difficult to pull off, relying on high weapon skill. A successful pierce attack ignores armour entirely. Very high base damage of 14.";
                case "Net": return "Special attack move. The fighter must pass a weapon skill check to snare the enemy. A successful net attack gains a huge 75 control.";
                case "Target": return "Allows a fighter to choose an area of the body to aim at. Base +20 penalty to hit, multiplied by 1.25 (chest) to 2 (head) depending on the area chosen.";
                case "Dodge": return "Relies on agility. 4x more likely to succeed against a thrusting type attack than a slashing, cutting or swinging one. Successful dodge increases control, depending on initiative.";
                case "Parry": return "Relies on weapon skill. 4x more likely to succeed against a slash, cut or swing attack than thrust. Successful parry increases control, depending on initiative.";
                case "Shield": return "Relies on weapon skill. Effective against all types of attack. Successful shield defence increases control, depending on initiative.";
                case "Stand": return "Governed by agility. Until the fighter stands he will take penalties to defence and will suffer increased damage. While fallen, a fighter can only defend or attempt to stand.";
                case "Press": return "Press the Enemy. Available with 51%+ control. Very high accuracy attack (2x easier than cut, 4x easier than thrust). Double control loss if the attack misses or is defended.";
                case "Inspire": return "Inspire the Team. Gives the team a 30% boost to attack, defence and damage. Lasts three turns. The leader is vulnerable when inspiring and will not defend against any attack.";
                case "Rage": return "Frenzied Rage. Large penalty to hit (8x harder than swing, 2x harder than smash). Does double damage to armour. Inflicts powerful base 18 damage if through armour.";
                case "Guard": return "Guard Adjacent Allies. Increases defence rolls for adjacent allies by 10%. Decreases enemy chance to hit adjacent allies by 10%. The Guard will defend himself as well.";
                case "Distract": return "Distract the Leader. Standard attack with a bonus against a Leader Class opponent. Leaders will be distracted, causing a 15% drop in performance to the whole team for three turns.";
                case "AngledAttack": return "Shift Team Formation. The whole enemy team suffers a 50% penalty to parry and shield defences. Dodge defences are unaffected. Performs a standard thrusting attack.";
                case "PinWeapon": return "Parry and Pin Weapon. Requires two weapon skill checks, the lowest used. If successful, the defence multiplier is 400 (twice the usual parry multiplier).";
                case "ShoulderBarge": return "Attempt to knock over opposing fighter. Attacker's strength tested against defender's. Successful fall likely causes damage. Costs 14 base energy.";
                case "Trip": return "Attempt to trip opposing fighter. Attacker's initiative and agility tested against defender's. Successful fall likely causes damage. Costs 14 base energy.";
                case "Pommel": return "Strike enemy with weapon pommel. Guaranteed to hit the head. Defender must beat toughness check to avoid being dazed (penalties to attack and defence).";
                case "Sun": return "Put the sun in the enemies' eyes. 40% chance of blinding each enemy. Blinded fighters suffer severe penalties until they pass a Recovery check.";
                case "Tangle": return "Tangle opponent's legs with weapon. Weapon skill tested against defender's agility. Successful attack causes the defender to fall.";
                case "Taunt": return "Defensive position and taunt enemy. Opposing fighter suffers 20% penalty to discipline checks. Taunting fighter performs standard parry, shield or dodge if attacked.";
                case "ShieldBreak": return "Attempt to shatter enemy's shield. Tests strength. On success, enemy's shield is destroyed, and they can only dodge for the rest of the battle.";
                case "ForearmSlash": return "Slash deep cut into enemy's forearm. Targets right arm. Success often causes defender to drop weapon and become disarmed.";
                case "FormUp": return "The fighter yells to his team, forming them up. Team gets a 40% boost to discipline and defence rolls, decreasing by 10% per turn. Fighter is vulnerable.";
                case "Entrench": return "Strong defensive stance. 30% defence bonus. Lasts two turns. Defence bonus reduces to 15% in the second turn.";
                case "RecoverNet": return "The Retarius recovers their net and will be able to use it again. Involves a defensive Dodge against any attacks.";
                case "Aegis": return "Guard Adjacent Allies and take hits for them. Increases ally defence rolls and decreases enemy hit chance. Guard's armour tested if ally struck.";
                case "Massacre": return "Triple Damage Attack. If the hit lands it will do triple damage to armour and health. Four turn cooldown.";
                case "Disappear": return "Dodge with triple effectiveness. Dodge skill is tripled. Three turn cooldown.";
                case "ZeroIn": return "Target a specific body part. Weapon skill check is tripled. Very accurate targeted attack. Two turn cooldown.";
                case "ParryMaster": return "Parry with triple effectiveness. Parry skill is tripled. Three turn cooldown.";
                case "ShieldMaster": return "Shield defence with triple effectiveness. Shield skill is tripled. Three turn cooldown.";
                default: return "";
            }
        }

        private void CycleTarget(int direction)
        {
            int count = _bodyParts.Length;
            _targetBodyPartIdx = (_targetBodyPartIdx + direction + count) % count;
            string part = _bodyParts[_targetBodyPartIdx];
            ActionSelector.targetChoice[_selectedSlot] = part;
            
            // Sync with game's internal selector
            var selector = Object.FindObjectOfType<ActionSelector>();
            if (selector != null && selector.targetChoiceSelector != null)
            {
                for (int i = 0; i < selector.targetChoiceSelector.options.Count; i++)
                {
                    if (selector.targetChoiceSelector.options[i].text.ToLower() == part)
                    {
                        selector.targetChoiceSelector.value = i;
                        selector.TargetSelector(); // Trigger game's target selection logic
                        break;
                    }
                }
            }
            
            ScreenReader.Say($"Targeting {part}.");
        }

        private void AnnounceCurrentUnit()
        {
            int i = _selectedSlot;
            var g = FightProcessor.currentPlayerGladiator[i];
            if (g == null) return;

            string name = g.FirstName;
            int blood = FightProcessor.bloodPlayer[i];
            int morale = FightProcessor.moraleDisplayPlayer[i];
            int energy = FightProcessor.pEnergy[i];
            string state = FightProcessor.currentStatePlayer[i];
            string order = FightProcessor.playerAct[i];
            
            int myPos = FightProcessor.pGladPosx[i];
            string surroundings = "";
            for (int e = 0; e < 6; e++)
            {
                if (FightProcessor.eAlive[e] && FightProcessor.eGladPosx[e] == myPos)
                {
                    string eName = FightProcessor.currentEnemyGladiator[e].FirstName;
                    string eLastAct = FightProcessor.eLastDecisionsBySlot[myPos];
                    string actDesc = !string.IsNullOrEmpty(eLastAct) ? $" (last turn: {eLastAct})" : "";
                    surroundings += $"Engaged with {eName}{actDesc}. ";
                }
            }

            string targetInfo = (order == "Target") ? $" Striking {ActionSelector.targetChoice[i]}." : "";
            ScreenReader.Say($"{name}. Pos {myPos + 1}. {surroundings}{blood}% health. {energy}% energy. {morale}% control. {state}. Order: {order}{targetInfo}.");
        }

        private void AnnounceFullStatus()
        {
            string status = "Battle Status. ";
            for (int i = 0; i < 6; i++)
            {
                var g = FightProcessor.currentPlayerGladiator[i];
                if (g != null)
                {
                    string name = g.FirstName;
                    int blood = FightProcessor.bloodPlayer[i];
                    int energy = FightProcessor.pEnergy[i];
                    int morale = FightProcessor.moraleDisplayPlayer[i];
                    string state = FightProcessor.currentStatePlayer[i];
                    status += $"{name}: {blood}% health, {energy}% energy, {morale}% control ({state}). ";
                }
            }
            status += "Enemies: ";
            for (int i = 0; i < 6; i++)
            {
                var g = FightProcessor.currentEnemyGladiator[i];
                if (g != null)
                {
                    string name = g.FirstName;
                    string className = g.Class;
                    string advantages = GetClassAdvantageInfo(className);
                    string advDesc = !string.IsNullOrEmpty(advantages) ? $" ({advantages})" : "";

                    int blood = FightProcessor.bloodEnemy[i];
                    int energy = FightProcessor.eEnergy[i];
                    int morale = FightProcessor.moraleDisplayEnemy[i];
                    string state = FightProcessor.currentStateEnemy[i];
                    
                    int pos = FightProcessor.eGladPosx[i];
                    string lastAct = (pos >= 0 && pos < 6) ? FightProcessor.eLastDecisionsBySlot[pos] : "";
                    string actDesc = !string.IsNullOrEmpty(lastAct) ? $" ({lastAct})" : "";
                    
                    status += $"{name} ({className}){advDesc}{actDesc}: {blood}% health, {energy}% energy, {morale}% control ({state}). ";
                }
            }
            ScreenReader.Say(status);
        }

        private void AnnounceTacticalMap()
        {
            string map = "Tactical Map. ";
            for (int p = 0; p < 6; p++)
            {
                string slotInfo = $"Pos {p + 1}: ";
                bool found = false;
                for (int i = 0; i < 6; i++)
                {
                    var g = FightProcessor.currentPlayerGladiator[i];
                    if (g != null && FightProcessor.pGladPosx[i] == p)
                    {
                        slotInfo += $"{g.FirstName} ({FightProcessor.bloodPlayer[i]}% health, {FightProcessor.currentStatePlayer[i]}). ";
                        found = true;
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    var g = FightProcessor.currentEnemyGladiator[i];
                    if (g != null && FightProcessor.eGladPosx[i] == p)
                    {
                        string lastAct = FightProcessor.eLastDecisionsBySlot[p];
                        string actDesc = !string.IsNullOrEmpty(lastAct) ? $" ({lastAct})" : "";
                        slotInfo += $"Enemy {g.FirstName}{actDesc} ({FightProcessor.bloodEnemy[i]}% health, {FightProcessor.currentStateEnemy[i]}). ";
                        found = true;
                    }
                }
                if (found) map += slotInfo;
            }
            ScreenReader.Say(map);
        }

        private void HandleResults()
        {
            if (_viewingLog || _viewingRolls || _viewingStats)
            {
                ProcessLogInput();
                return;
            }

            if (ProcessUnitStatsInput()) return;

            if (Input.GetKeyDown(KeyCode.F3))
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                EnterLog(shift);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                var fp = Object.FindObjectOfType<FightProcessor>();
                AccessStateManager.IsBattleDoneThisWeek = true;

                if (fp != null && fp.exitButton != null && fp.exitButton.interactable)
                {
                    fp.exitButton.onClick.Invoke();
                    return;
                }

                // If we are on BattleResult level, we should transition to TeamResults to simulate other matches
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (sceneName == "BattleResult")
                {
                    var lm = Object.FindObjectOfType<LevelManager>();
                    if (lm != null)
                    {
                        ScreenReader.Say("Continuing to league results.");
                        lm.LoadLevel("TeamResults");
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("TeamResults");
                    }
                }
                else if (sceneName == "TeamResults")
                {
                    // If we are on TeamResults, simulation should have happened.
                    // We need to trigger the game's way of advancing, which is calling cal.ToggleProcessing()
                    // or clicking the Continue button if it exists and is interactable.
                    var simulator = Object.FindObjectOfType<FightSimulator>();
                    if (simulator != null && simulator.continueButton != null && simulator.continueButton.interactable)
                    {
                        ScreenReader.Say("Processing week summary.");
                        simulator.continueButton.onClick.Invoke();
                    }
                    else
                    {
                        var cal = Object.FindObjectOfType<Calendar>();
                        if (cal != null)
                        {
                            ScreenReader.Say("Advancing to week summary.");
                            cal.ToggleProcessing();
                        }
                        else
                        {
                            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
                        }
                    }
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
                }
            }
        }

        public void OnMessageReceived(int tabId, string message)
        {
            if (string.IsNullOrEmpty(message) || message.Trim() == "***") return;
            string clean = Regex.Replace(message, "<.*?>", string.Empty).Trim();
            
            // Handle round-based filtering
            var fp = Object.FindObjectOfType<FightProcessor>();
            if (fp != null)
            {
                var field = typeof(FightProcessor).GetField("globalTurn", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    int currentRound = (int)field.GetValue(fp);
                    if (currentRound != _lastGlobalTurn)
                    {
                        _lastGlobalTurn = currentRound;
                        _roundSeenMessages.Clear();
                        _roundSeenSkeletons.Clear();
                    }
                }
            }

            // Filter identical messages within the same round
            if (_roundSeenMessages.Contains(clean)) return;
            _roundSeenMessages.Add(clean);

            // Standard duplicate filtering (across turns/rounds within a short window)
            if (_recentLogMessages.Contains(clean)) return;
            _recentLogMessages.Enqueue(clean);
            if (_recentLogMessages.Count > 10) _recentLogMessages.Dequeue();

            if (!string.IsNullOrEmpty(clean))
            {
                // Shift+F3 (Full Log) always gets everything
                _rollsLog.Add(clean);

                string lower = clean.ToLower();
                
                // Technical/Excluded detection: Filter out technical rolls, energy, boosts, and base value changes from F3 log.
                bool isExcluded = lower.Contains("roll") ||
                                 lower.Contains("rolls") ||
                                 lower.Contains("energy") ||
                                 lower.Contains("boosted by") ||
                                 lower.Contains("increased by") ||
                                 lower.Contains("decreased by") ||
                                 lower.Contains("modified") ||
                                 lower.Contains("base") ||
                                 Regex.IsMatch(clean, @"\(\d+\)"); // Exclude technical parentheticals like (100)

                // Everything that isn't excluded is now considered narrative for the F3 log
                bool isNarrative = !isExcluded;

                // Force inclusion of specific narrative patterns even if they somehow match excluded words
                if (lower.Contains("targeted strike") || lower.Contains("target doing") || lower.Contains("into the head") || lower.Contains("into the torso"))
                {
                    isNarrative = true;
                }

                if (isNarrative)
                {
                    // Skeleton-based filtering to remove redundant versions, preferring the version WITHOUT numbers
                    string skeleton = GetSkeleton(clean);
                    if (_roundSeenSkeletons.TryGetValue(skeleton, out string existing))
                    {
                        bool hasNumbers = Regex.IsMatch(clean, @"\d");
                        bool existingHasNumbers = Regex.IsMatch(existing, @"\d");
                        
                        // If current has numbers and existing doesn't, skip the numbered one
                        if (hasNumbers && !existingHasNumbers) return;
                        
                        // If current has NO numbers and existing DOES, replace the numbered one in the log
                        if (!hasNumbers && existingHasNumbers)
                        {
                            if (_battleLog.Count > 0 && _battleLog.Last() == existing)
                            {
                                _battleLog.RemoveAt(_battleLog.Count - 1);
                            }
                        }
                        else if (existing.Length >= clean.Length) 
                        {
                            return;
                        }
                    }
                    _roundSeenSkeletons[skeleton] = clean;

                    _battleLog.Add(clean);

                    // Specifically announce leadership roll results
                    if (clean.Contains("arrives in disarray, while"))
                    {
                        if (clean.Contains("Player gets to arrange"))
                        {
                            ScreenReader.SayQueued("Leadership Victory! You can now arrange your formation.");
                        }
                        else
                        {
                            ScreenReader.SayQueued("Leadership Defeat. Formation fixed.");
                        }
                    }

                    // Cinematic text should NOT interrupt, use SayQueued
                    if (AccessStateManager.IsIn(AccessStateManager.State.Combat) && !_viewingLog && !_viewingRolls && !_viewingStats) 
                        ScreenReader.SayQueued(clean);
                }
            }
        }
        private string GetSkeleton(string msg)
        {
            // Remove parenthetical numbers first: (100) -> ""
            string s = Regex.Replace(msg, @"\(\d+\)", "");
            // Remove standalone numbers: 100 -> ""
            s = Regex.Replace(s, @"\d+", "");
            // Normalize spaces and lowercase for broad matching
            return Regex.Replace(s, @"\s+", " ").Trim().ToLower();
        }
    }
}
