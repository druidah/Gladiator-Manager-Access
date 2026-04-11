using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[assembly: MelonInfo(typeof(GladiatorManagerAccess.Main), "GladiatorManagerAccess", "1.0.0", "Szabi")]
[assembly: MelonGame("Eternity Games", "GladiatorManager")]

namespace GladiatorManagerAccess
{
    /// <summary>
    /// Main mod entry point. Coordinates all handlers and processes global hotkeys.
    /// </summary>
    public class Main : MelonMod
    {
        #region Fields

        private bool _gameReady = false;

        /// <summary>
        /// Debug mode - when true, logs all screenreader output and detailed game state.
        /// Toggle with F12.
        /// </summary>
        public static bool DebugMode = false;

        private MainMenuHandler _mainMenuHandler;
        private SaveScreenHandler _saveScreenHandler;
        private NGOptionsHandler _ngOptionsHandler;
        private HomeScreenHandler _homeScreenHandler;
        private CharacterCreationHandler _characterCreationHandler;
        private BarracksHandler _barracksHandler;
        private PerkHandler _perkHandler;
        private MarketHandler _marketHandler;
        private FinanceHandler _financeHandler;
        private OptionsHandler _optionsHandler;
        private HelpHandler _helpHandler;
        private TeamSelectionHandler _teamSelectionHandler;
        private CombatHandler _combatHandler;
        private LeagueHandler _leagueHandler;
        private RecordsHandler _recordsHandler;
        private WeekEndHandler _weekEndHandler;
        private PopupHandler _popupHandler;

        #endregion

        #region Lifecycle

        public override void OnInitializeMelon()
        {
            ScreenReader.Initialize();
            Loc.Initialize();
            InitializeHandlers();
            
            // Initialize Harmony patches
            HarmonyInstance.PatchAll();
            
            HelpHandler.MainInstance = this;
            MelonCoroutines.Start(AnnounceStartupDelayed());
        }

        public IAccessibleHandler GetHandler(AccessStateManager.State state)
        {
            switch (state)
            {
                case AccessStateManager.State.MainMenu: return _mainMenuHandler;
                case AccessStateManager.State.Home: return _homeScreenHandler;
                case AccessStateManager.State.Barracks: return _barracksHandler;
                case AccessStateManager.State.Shop: return _marketHandler;
                case AccessStateManager.State.Finance: return _financeHandler;
                case AccessStateManager.State.Options: return _optionsHandler;
                case AccessStateManager.State.CharacterCreation: return _characterCreationHandler;
                case AccessStateManager.State.TeamSelection: return _teamSelectionHandler;
                case AccessStateManager.State.Combat: return _combatHandler;
                case AccessStateManager.State.League: return _leagueHandler;
                case AccessStateManager.State.Records: return _recordsHandler;
                case AccessStateManager.State.WeekEndSummary: return _weekEndHandler;
                case AccessStateManager.State.Popup: return _popupHandler;
                case AccessStateManager.State.SaveScreen: return _saveScreenHandler;
                case AccessStateManager.State.PerkScreen: return _perkHandler;
                default: return null;
            }
        }

        private void InitializeHandlers()
        {
            _mainMenuHandler = new MainMenuHandler();
            _saveScreenHandler = new SaveScreenHandler();
            _ngOptionsHandler = new NGOptionsHandler();
            _homeScreenHandler = new HomeScreenHandler();
            _characterCreationHandler = new CharacterCreationHandler();
            _barracksHandler = new BarracksHandler();
            _perkHandler = new PerkHandler();
            _marketHandler = new MarketHandler();
            _financeHandler = new FinanceHandler();
            _optionsHandler = new OptionsHandler();
            _helpHandler = new HelpHandler();
            _teamSelectionHandler = new TeamSelectionHandler();
            _combatHandler = new CombatHandler();
            _leagueHandler = new LeagueHandler();
            _recordsHandler = new RecordsHandler();
            _weekEndHandler = new WeekEndHandler();
            _popupHandler = new PopupHandler();
        }

        private IEnumerator AnnounceStartupDelayed()
        {
            // Short delay so screenreader is ready
            yield return new WaitForSeconds(1f);
            ScreenReader.Say(Loc.Get("mod_loaded"));
        }

        public override void OnUpdate()
        {
            // Wait for game to be ready
            if (!CheckGameReady()) return;

            // Process global hotkeys first
            if (ProcessHotkeys()) return;

            // Update all handlers
            UpdateHandlers();
        }

        private bool CheckGameReady()
        {
            if (_gameReady) return true;

            // Check for game singletons or scene-specific managers
            if (ZUIManager.Instance != null || 
                Object.FindObjectOfType<LevelManager>() != null ||
                Object.FindObjectOfType<GladiatorGenerator>() != null)
            {
                _gameReady = true;
                MelonLogger.Msg("Game ready");
            }

            return _gameReady;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            MelonLogger.Msg($"Scene loaded: {sceneName}");
            DebugLogger.LogState($"Scene changed to: {sceneName}");
            _gameReady = false; // Reset on scene change

            if (sceneName == "Home")
            {
                AccessStateManager.TryEnter(AccessStateManager.State.Home);
                _weekEndHandler.CaptureInitialState();
            }
            else if (sceneName == "Finance")
            {
                AccessStateManager.TryEnter(AccessStateManager.State.Finance);
            }
            else if (sceneName == "Options")
            {
                AccessStateManager.TryEnter(AccessStateManager.State.Options);
            }
            else if (sceneName == "TeamSelection")
            {
                AccessStateManager.TryEnter(AccessStateManager.State.TeamSelection);
            }
            else if (sceneName == "Melee")
            {
                AccessStateManager.TryEnter(AccessStateManager.State.Combat);
            }
            else if (sceneName == "League")
            {
                var lp = Object.FindObjectOfType<LeaguePopulator>();
                if (lp != null && lp.records)
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Records);
                }
                else
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.League);
                }
            }
            else if (sceneName == "PlayerRecords")
            {
                AccessStateManager.TryEnter(AccessStateManager.State.Records);
            }
            else if (sceneName == "Barracks")
            {
                // Note: Market/Shop also uses the Barracks scene
                if (!AccessStateManager.IsIn(AccessStateManager.State.Shop))
                {
                    AccessStateManager.TryEnter(AccessStateManager.State.Barracks);
                }
            }

            // Announce scene name
            string locKey = "scene_" + sceneName.Replace(" ", "_").ToLower();
            string friendlyName = Loc.Get(locKey);
            if (friendlyName != locKey)
            {
                ScreenReader.SayQueued(Loc.Get("scene_loaded", friendlyName));
            }
        }

        public override void OnApplicationQuit()
        {
            ScreenReader.Shutdown();
        }

        #endregion

        #region Hotkeys

        /// <summary>
        /// Processes global hotkeys. Returns true if a key was handled.
        /// </summary>
        private bool ProcessHotkeys()
        {
            // F12 = Toggle debug mode
            if (Input.GetKeyDown(KeyCode.F12))
            {
                DebugMode = !DebugMode;
                var status = DebugMode ? "enabled" : "disabled";
                MelonLogger.Msg($"Debug mode {status}");
                ScreenReader.Say($"Debug mode {status}");
                return true;
            }

            // F1 = Context Help (Mod help)
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugLogger.LogInput("F1", "Context Help");
                AnnounceHelp();
                return true;
            }

            return false;
        }

        #endregion

        #region Handler Updates

        private void UpdateHandlers()
        {
            _mainMenuHandler.Update();
            _saveScreenHandler.Update();
            _ngOptionsHandler.Update();
            _homeScreenHandler.Update();
            _characterCreationHandler.Update();
            _barracksHandler.Update();
            _perkHandler.Update();
            _marketHandler.Update();
            _financeHandler.Update();
            _optionsHandler.Update();
            _helpHandler.Update();
            _teamSelectionHandler.Update();
            _combatHandler.Update();
            _leagueHandler.Update();
            _recordsHandler.Update();
            _weekEndHandler.Update();
            _popupHandler.Update();
        }

        #endregion

        #region Help

        private void AnnounceHelp()
        {
            IAccessibleHandler activeHandler = null;

            // Determine active handler based on state
            switch (AccessStateManager.Current)
            {
                case AccessStateManager.State.MainMenu: activeHandler = _mainMenuHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Home: activeHandler = _homeScreenHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Barracks: activeHandler = _barracksHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Shop: activeHandler = _marketHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Finance: activeHandler = _financeHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Options: activeHandler = _optionsHandler as IAccessibleHandler; break;
                case AccessStateManager.State.CharacterCreation: activeHandler = _characterCreationHandler as IAccessibleHandler; break;
                case AccessStateManager.State.TeamSelection: activeHandler = _teamSelectionHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Combat: activeHandler = _combatHandler as IAccessibleHandler; break;
                case AccessStateManager.State.League: activeHandler = _leagueHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Records: activeHandler = _recordsHandler as IAccessibleHandler; break;
                case AccessStateManager.State.WeekEndSummary: activeHandler = _weekEndHandler as IAccessibleHandler; break;
                case AccessStateManager.State.Popup: activeHandler = _popupHandler as IAccessibleHandler; break;
                case AccessStateManager.State.SaveScreen: activeHandler = _saveScreenHandler as IAccessibleHandler; break;
                case AccessStateManager.State.PerkScreen: activeHandler = _perkHandler as IAccessibleHandler; break;
            }

            if (activeHandler != null)
            {
                string help = activeHandler.GetHelpText();
                if (!string.IsNullOrEmpty(help))
                {
                    ScreenReader.Say(help);
                    return;
                }
            }

            // Global fallback
            string hotkeys = "Mod hotkeys: " +
                "F1 Context Help. " + 
                "F12 Toggle debug mode. ";

            ScreenReader.Say(hotkeys);
        }

        #endregion
    }
}
