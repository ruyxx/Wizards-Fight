// Lokalna rang lista: najboljih pet vremena za svaki nivo, čuva se u PlayerPrefs.
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AWizardsFight
{
    public static class Leaderboard
    {
        private const int Keep = 5;

        private static string Key(string level) => "Records_" + level;

        public static List<float> Times(string level)
        {
            var stored = PlayerPrefs.GetString(Key(level), "");
            var result = new List<float>();
            foreach (var part in stored.Split(';'))
                if (float.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
                    result.Add(value);
            return result;
        }

        // Upisuje vreme i vraća mesto na listi (1 = novi rekord), ili 0 ako nije ušlo među najboljih pet.
        public static int Record(string level, float seconds)
        {
            var times = Times(level);
            times.Add(seconds);
            times.Sort();
            int place = times.IndexOf(seconds) + 1;
            times = times.Take(Keep).ToList();
            PlayerPrefs.SetString(Key(level), string.Join(";", times.Select(t => t.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))));
            PlayerPrefs.Save();
            return place <= Keep ? place : 0;
        }

        // Endless: pamti se najdalji talas (više je bolje).
        public const string EndlessKey = "Endless";

        public static int RecordWave(int wave, string key = EndlessKey)
        {
            var waves = Times(key).Select(value => -value).ToList();
            waves.Add(wave);
            waves = waves.OrderByDescending(value => value).ToList();
            int place = waves.IndexOf(wave) + 1;
            PlayerPrefs.SetString(Key(key), string.Join(";", waves.Take(Keep).Select(value => (-value).ToString(System.Globalization.CultureInfo.InvariantCulture))));
            PlayerPrefs.Save();
            return place <= Keep ? place : 0;
        }

        public static List<int> Waves(string key = EndlessKey) => Times(key).Select(value => Mathf.RoundToInt(-value)).ToList();

        public static string Format(float seconds)
        {
            int whole = Mathf.FloorToInt(seconds);
            return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
        }

        public static string Summary()
        {
            var text = new StringBuilder();
            foreach (var level in GameLevels.All)
            {
                var times = Times(level.Scene);
                text.Append("<color=#FFD966>").Append(level.Title).Append("</color>   ");
                text.Append(times.Count == 0 ? "<color=#888888>not cleared yet</color>" : string.Join("   ", times.Select((t, i) => (i + 1) + ". " + Format(t))));
                text.Append('\n');
            }
            var waves = Waves();
            text.Append("<color=#FF9A5C>ENDLESS</color>   ");
            text.Append(waves.Count == 0 ? "<color=#888888>not played yet</color>" : string.Join("   ", waves.Select((w, i) => (i + 1) + ". wave " + w)));
            text.Append('\n');
            var daily = Waves(DailyChallenge.Key);
            text.Append("<color=#7CFF8A>TODAY'S CHALLENGE</color>   ");
            text.Append(daily.Count == 0 ? "<color=#888888>not played yet</color>" : string.Join("   ", daily.Select((w, i) => (i + 1) + ". wave " + w)));
            text.Append('\n');
            return text.ToString();
        }
    }
}
