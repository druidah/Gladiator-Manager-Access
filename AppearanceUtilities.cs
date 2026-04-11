using UnityEngine;
using System.Collections.Generic;

namespace GladiatorManagerAccess
{
    public static class AppearanceUtilities
    {
        public static string GetAppearance(int gladId, bool preferPlayer = true)
        {
            GladiatorPortrait p = null;
            if (preferPlayer)
            {
                p = DataManager.allThePPortraits?.Find(x => x.GladID == gladId);
                if (p == null) p = DataManager.allTheEPortraits?.Find(x => x.GladID == gladId);
            }
            else
            {
                p = DataManager.allTheEPortraits?.Find(x => x.GladID == gladId);
                if (p == null) p = DataManager.allThePPortraits?.Find(x => x.GladID == gladId);
            }

            if (p == null) return "Appearance unknown.";

            List<string> features = new List<string>();

            // Skin & Age
            string skin = GetSkinDescription(p.Skin);
            if (!string.IsNullOrEmpty(skin)) features.Add(skin);

            // Hair
            string hair = GetHairDescription(p.Hair, p.Gender == "Female");
            if (!string.IsNullOrEmpty(hair)) features.Add(hair);

            // Facial Hair (Male only)
            if (p.Gender == "Male")
            {
                string facial = GetFacialHairDescription(p.Beard, p.Moustache);
                if (!string.IsNullOrEmpty(facial)) features.Add(facial);
            }

            // Eyes & Features
            string eyes = GetEyesDescription(p.Eyes);
            if (!string.IsNullOrEmpty(eyes)) features.Add(eyes);

            string extra = GetFeatureDescription(p.Feature);
            if (!string.IsNullOrEmpty(extra)) features.Add(extra);

            if (features.Count == 0) return "Average appearance.";
            return string.Join(", ", features) + ".";
        }

        public static string GetFeatureDescription(string category, int id, bool isFemale = false)
        {
            category = category.ToLower();
            if (category.Contains("skin")) return GetSkinDescription(id);
            if (category.Contains("hair")) return GetHairDescription(id, isFemale);
            if (category.Contains("eyes")) return GetEyesDescription(id);
            if (category.Contains("eyebrows")) return GetEyebrowsDescription(id);
            if (category.Contains("beard")) return GetBeardDescription(id);
            if (category.Contains("moustache")) return GetMoustacheDescription(id);
            if (category.Contains("marks") || category.Contains("feature")) return GetFeatureDescription(id);
            if (category.Contains("mouth")) return "Mouth style " + id;
            if (category.Contains("nose")) return "Nose style " + id;
            if (category.Contains("clothing")) return "Clothing style " + id;
            return "Style " + id;
        }

        public static string GetSkinDescription(int id)
        {
            // 0-2: Mid, 3-5: Old, 6-8: Young
            string age = id < 3 ? "weathered" : (id < 6 ? "wrinkled" : "smooth");
            string tone = (id % 3 == 0) ? "fair" : (id % 3 == 1 ? "tanned" : "dark");
            return $"{tone}, {age} skin";
        }

        public static string GetHairDescription(int id, bool isFemale)
        {
            if (id == 200 || id == 100) return "None";

            if (isFemale)
            {
                if (id < 17) return "short bob";
                if (id < 35) return "shoulder-length hair";
                if (id < 49) return "long braids";
                if (id < 66) return "wild curls";
                if (id < 82) return "neat bun";
                if (id < 97) return "ponytail";
                return "long hair";
            }
            else
            {
                if (id < 5) return "cropped hair";
                if (id < 10) return "military buzzcut";
                if (id < 18) return "balding head";
                if (id < 30) return "wild mane";
                if (id < 42) return "short spikes";
                if (id < 60) return "thick curls";
                if (id < 80) return "neatly combed";
                return "shaggy hair";
            }
        }

        public static string GetFacialHairDescription(int beard, int moustache)
        {
            if (beard == 100 && moustache == 100) return "clean-shaven";
            
            string b = GetBeardDescription(beard);
            string m = GetMoustacheDescription(moustache);

            if (b != "None" && m != "None") return $"{b} and {m}";
            if (b != "None") return b;
            return m;
        }

        public static string GetBeardDescription(int id)
        {
            if (id == 100) return "None";
            if (id < 5) return "light stubble";
            if (id < 9) return "short beard";
            if (id < 13) return "thick beard";
            if (id < 15) return "goatee";
            if (id < 21) return "braided beard";
            if (id < 27) return "pointed beard";
            if (id < 29) return "full beard";
            if (id < 32) return "groomed beard";
            if (id < 37) return "wild beard";
            if (id < 41) return "rugged beard";
            if (id < 45) return "square beard";
            return "long beard";
        }

        public static string GetMoustacheDescription(int id)
        {
            if (id == 100) return "None";
            if (id < 2) return "thin moustache";
            if (id < 4) return "pencil moustache";
            if (id < 6) return "thick moustache";
            if (id < 8) return "horseshoe moustache";
            if (id < 10) return "handlebar moustache";
            return "walrus moustache";
        }

        public static string GetEyesDescription(int id)
        {
            if (id < 10) return "piercing eyes";
            if (id < 25) return "dark eyes";
            if (id < 45) return "narrow eyes";
            return "alert eyes";
        }

        public static string GetFeatureDescription(int id)
        {
            if (id == 100) return "None";
            if (id < 3) return "facial scar";
            if (id < 7) return "war paint";
            if (id < 10) return "broken nose";
            if (id < 13) return "bruised face";
            if (id < 16) return "rough tattoos";
            if (id < 19) return "tribal markings";
            if (id < 23) return "facial ink";
            return "distinctive features";
        }

        public static string GetEyebrowsDescription(int id)
        {
            if (id == 100) return "None";
            if (id < 5) return "thin eyebrows";
            if (id < 11) return "arched eyebrows";
            if (id < 16) return "bushy eyebrows";
            if (id < 21) return "straight eyebrows";
            if (id < 26) return "furrowed eyebrows";
            if (id < 35) return "slanted eyebrows";
            if (id < 41) return "thick eyebrows";
            return "defined eyebrows";
        }
    }
}
