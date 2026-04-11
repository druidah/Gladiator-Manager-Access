using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GladiatorManagerAccess
{
    public static class UIUtilities
    {
        public static string GetArmourStatus(int gladID, bool isPlayer, int slot = -1)
        {
            if (slot >= 0 && slot < 6)
            {
                var armourArray = isPlayer ? Populator.armourPlayer : Populator.armourEnemy;
                if (armourArray == null) return "Armour data not initialized.";

                List<string> damagedParts = new List<string>();
                for (int part = 0; part < 10; part++)
                {
                    var a = armourArray[slot, part];
                    if (a != null && a.Condition < 100)
                    {
                        damagedParts.Add($"{a.Part}: {a.Condition}%");
                    }
                }

                if (damagedParts.Count == 0) return "All armour is in perfect condition.";
                return "Damaged Armour: " + string.Join(", ", damagedParts) + ".";
            }

            var db = DataManager.dbManager;
            string table = isPlayer ? "PArmour" : "EArmour";
            
            try
            {
                var armourItems = db.Query<Armour>($"SELECT * FROM {table} WHERE GladID = ?", gladID);
                if (armourItems == null || armourItems.Count == 0) return "No armour records found.";

                List<string> damagedParts = new List<string>();
                foreach (var a in armourItems)
                {
                    if (a.Condition < 100)
                    {
                        damagedParts.Add($"{a.Part}: {a.Condition}%");
                    }
                }

                if (damagedParts.Count == 0) return "All armour is in perfect condition.";
                return "Damaged Armour: " + string.Join(", ", damagedParts) + ".";
            }
            catch
            {
                return "Armour data unavailable.";
            }
        }

        public static string GetInjuryDetails(int gladID, bool isPlayer)
        {
            try
            {
                var list = isPlayer ? DataManager.allThePInjuries : DataManager.allTheEInjuries;
                if (list == null) return "No active injuries.";

                var activeInjuries = list.FindAll(x => x.ID == gladID);
                if (activeInjuries.Count == 0) return "No active injuries.";

                List<string> descriptions = new List<string>();
                foreach (var inj in activeInjuries)
                {
                    string name = inj.Description;
                    string impact = GetInjuryImpactString(inj);
                    string recovery = inj.Residual ? "Permanent" : $"{inj.TotalRecLeft} days left";
                    
                    descriptions.Add($"{name} ({recovery}). {impact}");
                }

                return "Injuries: " + string.Join("; ", descriptions);
            }
            catch
            {
                return "Injury data unavailable.";
            }
        }

        private static string GetInjuryImpactString(ActiveInjuries inj)
        {
            List<string> impacts = new List<string>();
            if (inj.PerImpactOnIni != 0) impacts.Add($"Ini {inj.PerImpactOnIni}");
            if (inj.PerImpactOnStr != 0) impacts.Add($"Str {inj.PerImpactOnStr}");
            if (inj.PerImpactOnAgi != 0) impacts.Add($"Agi {inj.PerImpactOnAgi}");
            if (inj.PerImpactOnTou != 0) impacts.Add($"Tou {inj.PerImpactOnTou}");
            if (inj.PerImpactOnDis != 0) impacts.Add($"Dis {inj.PerImpactOnDis}");
            if (inj.PerImpactOnWeaSki != 0) impacts.Add($"Skill {inj.PerImpactOnWeaSki}");
            if (inj.PerImpactOnBra != 0) impacts.Add($"Bra {inj.PerImpactOnBra}");
            if (inj.PerImpactOnRec != 0) impacts.Add($"Rec {inj.PerImpactOnRec}");
            if (inj.PerImpactOnSpe != 0) impacts.Add($"Spe {inj.PerImpactOnSpe}");
            if (inj.PerImpactOnSta != 0) impacts.Add($"Sta {inj.PerImpactOnSta}");
            if (inj.PerImpactOnLea != 0) impacts.Add($"Lea {inj.PerImpactOnLea}");

            if (impacts.Count == 0) return "No major stat impact.";
            return "Impact: " + string.Join(", ", impacts);
        }

        public static string StripFormatting(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}
