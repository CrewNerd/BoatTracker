using System;
using BoatTracker.Bot.Configuration;
using Microsoft.Azure.WebJobs;

namespace BoatTrackerWebJob
{
    /// <summary>
    /// Main program for our web job. We call the daily report generator every hour since there could be
    /// clubs in each time zone. We refresh all of the caches every two hours.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Set up for logging and call our web job functions as appropriate.
        /// </summary>
        public static void Main()
        {
            var env = EnvironmentDefinition.Instance;
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH:MM");

            var logName = $"{env.Name}_report_{timestamp}";

            var host = new JobHost();
            host.CallAsync(
                typeof(DailyReport).GetMethod("SendDailyReport"),
                new
                {
                    logName = logName,
                    log = $"container/{logName}"
                }).Wait();

            // We refresh the bot caches every two hours for now. The bots will automatically
            // refresh on their own every 8 hours if the webjob fails to run for some reason.
            // It's better if we do it here so that user's don't see the latency.

            if (DateTime.UtcNow.Hour % 2 == 0)
            {
                logName = $"{env.Name}_refresh_{timestamp}";
                host.CallAsync(
                    typeof(RefreshCaches).GetMethod("RefreshBotCaches"),
                    new
                    {
                        logName = logName,
                        log = $"container/{logName}"
                    }).Wait();
            }
        }
    }
}
