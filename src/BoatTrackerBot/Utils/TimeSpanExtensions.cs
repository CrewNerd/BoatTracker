using System;
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

        public static TimeSpan? FromDisplayString(string s)
        {
            TimeSpan ts = TimeSpan.Zero;

            if (TimeSpan.TryParse(s, out ts))
            {
                return ts;
            }

            var tokens = s.Split(' ', ',');

            // TODO: Look for words like "one", "two", etc. and convert them to numeric strings

            for (int i = 0; i < tokens.Length; i++)
            {
                double num;

                if (double.TryParse(tokens[i], out num))
                {
                    if (tokens.Length > i + 1)
                    {
                        if (tokens[i + 1].ToLower().Contains("hour"))
                        {
                            ts += TimeSpan.FromHours(num);
                        }
                        else if (tokens[i + 1].ToLower().Contains("minute"))
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