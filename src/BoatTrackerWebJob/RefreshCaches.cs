using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;

using BoatTracker.Bot.Configuration;

namespace BoatTrackerWebJob
{
    public static class RefreshCaches
    {
        /// <summary>
        /// Iterates over all of the clubs, refreshing the cache for each one.
        /// </summary>
        /// <param name="log">The writer for logging.</param>
        [NoAutomaticTrigger]
        public static void RefreshBotCaches(
            string logName,
            [Blob("container/{logName}.txt")] TextWriter log)
        {
            var env = EnvironmentDefinition.Instance;

            log.WriteLine($"{env.Name}: Cache Refresh WebJob starting at {DateTime.UtcNow.ToString()}");

            foreach (var clubId in env.MapClubIdToClubInfo.Keys)
            {
                var clubInfo = env.MapClubIdToClubInfo[clubId];

                log.WriteLine($"Starting cache refresh for club: {clubId} at {DateTime.UtcNow.ToString()}");

                try
                {
                    Task t = Task.Run(() => RefreshBotCache(clubId, log));
                    t.Wait();
                }
                catch (Exception ex)
                {
                    log.WriteLine($"RefreshBotCache failed: {ex.Message}");
                    log.WriteLine($"RefreshBotCache failed: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        log.WriteLine($"RefreshBotCache failed: inner exception = {ex.InnerException.Message}");
                        log.WriteLine($"RefreshBotCache failed: inner exception = {ex.InnerException.StackTrace}");
                    }
                }

                log.WriteLine($"Finished cache refresh for club: {clubId} at {DateTime.UtcNow.ToString()}");
            }

            log.WriteLine($"{env.Name}: Cache Refresh WebJob complete at {DateTime.UtcNow.ToString()}");
        }

        /// <summary>
        /// Sends a request to the bot service to refresh the cache for the given club.
        /// </summary>
        /// <param name="clubId">The ID of the club whose cache should be refreshed.</param>
        /// <param name="log">The writer for logging.</param>
        /// <returns>A task that completes when the refresh request finishes.</returns>
        private static async Task RefreshBotCache(string clubId, TextWriter log)
        {
            var env = EnvironmentDefinition.Instance;

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(2);
                var url = new Uri($"http://{env.ServiceHost}/api/control/refreshcache?clubId={clubId}&securityKey={env.SecurityKey}");

                await client.PostAsync(url, null);
            }
        }
    }
}
