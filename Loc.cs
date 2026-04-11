using System.Collections.Generic;
using UnityEngine;

namespace GladiatorManagerAccess
{
    /// <summary>
    /// Central localization for the accessibility mod.
    /// Automatically detects game language.
    /// </summary>
    public static class Loc
    {
        #region Fields

        private static bool _initialized = false;
        private static string _currentLang = "en";

        private static readonly Dictionary<string, string> _english = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes localization. Call once at mod startup.
        /// </summary>
        public static void Initialize()
        {
            InitializeStrings();
            RefreshLanguage();
            _initialized = true;
        }

        /// <summary>
        /// Refreshes the language based on game settings.
        /// </summary>
        public static void RefreshLanguage()
        {
            string gameLang = GetGameLanguage();

            switch (gameLang)
            {
                // Add cases for other languages here if needed
                default:
                    _currentLang = "en";
                    break;
            }
        }

        /// <summary>
        /// Gets a localized string.
        /// </summary>
        public static string Get(string key)
        {
            if (!_initialized) Initialize();

            var dict = GetCurrentDictionary();

            // Try current language
            if (dict.TryGetValue(key, out string value))
                return value;

            // Fallback: English
            if (_english.TryGetValue(key, out string engValue))
                return engValue;

            // Final fallback: key itself
            return key;
        }

        /// <summary>
        /// Gets a localized string with placeholders.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Gets a localized class description.
        /// </summary>
        public static string GetClassDescription(string className)
        {
            string key = "class_desc_" + className.ToLower().Replace(" ", "_");
            string desc = Get(key);
            if (desc == key) return ""; // Not found
            return desc;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Detects the current game language.
        /// </summary>
        private static string GetGameLanguage()
        {
            try 
            {
                // From Translator.cs analysis:
                // language = PlayerPrefs.GetString("Language", "English");
                return PlayerPrefs.GetString("Language", "English");
            }
            catch 
            {
                return "English";
            }
        }

        private static Dictionary<string, string> GetCurrentDictionary()
        {
            switch (_currentLang)
            {
                default: return _english;
            }
        }

        /// <summary>
        /// Helper: Adds a string to the dictionary.
        /// </summary>
        private static void Add(string key, string english)
        {
            _english[key] = english;
        }

        /// <summary>
        /// Define all translations here.
        /// </summary>
        private static void InitializeStrings()
        {
            // ===== GENERAL =====
            Add("mod_loaded", "Gladiator Manager Access loaded. Press F1 for help.");
            Add("help_title", "Help:");
            Add("unknown", "Unknown");

            // ===== MAIN MENU =====
            Add("main_menu_opened", "Main Menu opened.");
            Add("main_menu_closed", "Leaving Main Menu.");
            Add("menu_item", "{2}, {0} of {1}.");

            // ===== SAVE SCREEN =====
            Add("save_screen_opened", "Save Selection Screen opened.");
            Add("no_slots_available", "No save slots available.");

            // ===== SCENES =====
            Add("scene_splash", "Splash Screen.");
            Add("scene_new_game", "Main Menu.");
            Add("scene_savescreen", "Save Selection.");
            Add("scene_ngoptions", "New Game Options.");
            Add("scene_home", "Home Base.");
            Add("scene_barracks", "Barracks.");
            Add("scene_melee", "Battlefield.");
            Add("scene_teamresults", "Team Results.");
            Add("scene_battleresult", "Battle Results.");
            Add("scene_options", "Options Menu.");
            Add("scene_loaded", "{0} screen loaded.");

            // ===== CLASS DESCRIPTIONS =====
            Add("class_desc_gladiator", "The Gladiator fights with a sword and leather armour. They typically have higher weapon skill but lower leadership. They usually make good solo fighters.");
            Add("class_desc_barbarian", "Barbarians are typically stonger, tougher and braver. They often have poor initiative and agility and bad discipline. They fight with a club and cloth armour. They are good at bashing enemies' heads in.");
            Add("class_desc_rogue", "Rogues typically have high initiative and agility, with lesser bonuses to skill and speed. They often have poor strength, toughness and discipline. They fight with daggers and thick cloth armour. They are good at bleeding an enemy to death.");
            Add("class_desc_retarius", "Retarii are typically very agile, with lesser bonuses to skill and initiative. They often have poor strength and toughness. They fight with a trident and light leather armour. They are good at holding an enemy at bay and keeping control.");
            Add("class_desc_murmillo", "The Murmillo fights with a sword and large shield and heavy leather armour. They typically have high toughnes and discipline but low agility and speed. They are difficult to take down.");
            Add("class_desc_bestiarius", "Bestiarii are typically faster and have better stamina, with lesser bonuses to skill and initiative. They often have poor strength and toughness. They fight with a spear and light cloth armour. They are good at keeping their distance.");
            Add("class_desc_hoplomachus", "Hoplomachi fight with a spear and large shield and heavy leather armour. They typically have high toughnes and discipline but low agility and speed. They are difficult to take down and good at keeping distance.");
            Add("class_desc_dimachaer", "Dimachaeri fight with two swords and leather armour. They typically have high skill, initiative, speed and agility but poor strength and toughness. They are efficient at dealing damage quickly.");
            Add("class_desc_thraex", "The Thraex fights with a curved sword and medium shield and leather armour. They typically have high weapon skill and agility but lower toughness and strength. They are efficient and technical fighters.");
            Add("class_desc_secutor", "Secutors are typically very strong and tough, with a high discipline. They often have poor agility and speed. They fight with a sword and large shield and heavy leather armour. They are good at taking a hit and staying in the fight.");
            Add("class_desc_leader", "Leaders typically have excellent leadership, with lesser bonuses to discipline and bravery. They often have poor speed, stamina and agility. They fight with a sword and shield, and leather armour. They are good at inspiring the rest of the team.");
            Add("class_desc_defender", "Defenders are typically more disciplined, tougher and braver. They often have poor speed, stamina and agility. They fight with a spear and shield, and studded leather armour. They are good at holding firm against a skilled opponent.");
            Add("class_desc_freestyler", "Freestylers are typically more skilled and had greater initiative and bravery. They often have poor discipline and leadership. They fight with a sword and a club, and leather armour. They have no class advantage or vulnerability so can be a useful safety pick in team selection.");

            // ===== HANDLER-SPECIFIC =====

            // Week Summary
            Add("week_summary_title", "Week End Summary");
            Add("week_summary_income", "Net Income: +{0}");
            Add("week_summary_expense", "Net Expense: {0}");
            Add("week_summary_salaries", "Weekly Salaries: {0}");
            Add("week_summary_gate", "Gate Receipts: +{0}");
            Add("week_summary_bounties", "Bounties: +{0}");
            Add("week_summary_armour", "Armour Costs: {0}");
            Add("week_summary_fees", "Signing Fees: {0}");
            Add("week_summary_balance", "Current Balance: {0}");
            Add("week_summary_training", "Training Results:");
            Add("week_summary_no_gains", "No significant attribute gains this week.");
            Add("week_summary_league_results", "League Results:");
            Add("week_summary_no_league_results", "No league results this week.");
            Add("week_summary_news", "News and Events:");
            Add("week_summary_continue", "Press Space or Enter to continue.");
            Add("week_summary_help", "Week summary. Press Space or Enter to continue.");
            
            // Barracks / Profile
            Add("level_up", "Level Up!");
            Add("perks", "Perks");
            
            // Stats
            Add("stat_str", "Strength");
            Add("stat_agi", "Agility");
            Add("stat_ini", "Initiative");
            Add("stat_tou", "Toughness");
            Add("stat_dis", "Discipline");
            Add("stat_swo", "Weapon Skill");
            Add("stat_bra", "Bravery");
            Add("stat_rec", "Recovery");
            Add("stat_spe", "Speed");
            Add("stat_sta", "Stamina");
            Add("stat_lea", "Leadership");
            Add("stat_potential_gain", "Potential +{0}");

            // Add more as needed: [handler]_[action]
        }

        #endregion
    }
}
