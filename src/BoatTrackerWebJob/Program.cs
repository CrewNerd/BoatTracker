using System;
using Microsoft.Azure.WebJobs;

namespace BoatTrackerWebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var host = new JobHost();
            host.Call(typeof(DailyReport).GetMethod("SendDailyReport"));

            // We refresh the bot caches every two hours for now. The bots will automatically
            // refresh on their own every 8 hours if the webjob fails to run for some reason.
            // It's better if we do it here so that user's don't see the latency.

            if (DateTime.UtcNow.Hour % 2 == 0)
            {
                host.Call(typeof(RefreshCaches).GetMethod("RefreshBotCaches"));
            }
        }
    }
}
