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
        [NoAutomaticTrigger]
        public static void RefreshBotCaches([Blob("container/cacherefresh.txt")] TextWriter log)
        {
            var env = EnvironmentDefinition.Instance;

            log.WriteLine($"{env.Name}: Cache Refresh WebJob starting at {DateTime.UtcNow.ToString()}");

            foreach (var clubId in env.MapClubIdToClubInfo.Keys)
            {
                var clubInfo = env.MapClubIdToClubInfo[clubId];

                log.WriteLine($"Starting cache refresh for club: {clubId}");

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

                log.WriteLine($"Finished cache refresh for club: {clubId}");
            }

            log.WriteLine($"Cache Refresh WebJob complete at {DateTime.UtcNow.ToString()}");
        }

        private static async Task RefreshBotCache(string clubId, TextWriter log)
        {
            var env = EnvironmentDefinition.Instance;

            using (HttpClient client = new HttpClient())
            {
                var url = new Uri($"http://{env.ServiceHost}/api/control/refreshcache?clubId={clubId}&securityKey={env.SecurityKey}");

                await client.PostAsync(url, null);
            }
        }
    }
}
