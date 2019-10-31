using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoatTracker.Bot.Utils
{
    public static class TimeSpanExtensions
    {
        public static string ToDisplayString(this TimeSpan timeSpan)
        {
            StringBuilder sb = new StringBuilder();

            if (timeSpan.Hours > 0)
            {
                sb.Append($"{timeSpan.Hours} hour");

                if (timeSpan.Hours > 1)
                {
                    sb.Append("s");
                }

                sb.Append(" ");
            }

            if (timeSpan.Minutes > 0)
            {
                sb.Append($"{timeSpan.Minutes} minutes");
            }

            return sb.ToString();
        }

        private static Dictionary<string, string> NumberTokens = new Dictionary<string, string>()
        {
            ["one"] = "1",
            ["two"] = "2",
            ["three"] = "3",
            ["four"] = "4",
            ["five"] = "5",
            ["six"] = "6",
            ["seven"] = "7",
            ["eight"] = "8",
            ["nine"] = "9",
            ["ten"] = "10",
            ["eleven"] = "11",
            ["twelve"] = "12",
            ["thirteen"] = "13",
            ["fourteen"] = "14",
            ["fifteen"] = "15",
            ["thirty"] = "30",
            ["forty-five"] = "45",
            ["ninety"] = "90",
        };

        public static TimeSpan? FromDisplayString(string s)
        {
            TimeSpan ts = TimeSpan.Zero;

            s = s.Trim().ToLower();

            if (TimeSpan.TryParse(s, out ts))
            {
                return ts;
            }

            // Look for words like "one", "two", etc. and convert them to numeric strings
            var tokens = s.Split(' ', ',').Select(t => NumberTokens.ContainsKey(t) ? NumberTokens[t] : t).ToArray();

            for (int i = 0; i < tokens.Length; i++)
            {
                double num;

                if (double.TryParse(tokens[i], out num))
                {
                    if (tokens.Length > i + 1)
                    {
                        if (tokens[i + 1].Contains("hour"))
                        {
                            ts += TimeSpan.FromHours(num);
                        }
                        else if (tokens[i + 1].Contains("minute"))
                        {
                            ts += TimeSpan.FromMinutes(num);
                        }
                    }
                }
            }

            return ts != TimeSpan.Zero ? ts : (TimeSpan?)null;
        }
    }
}