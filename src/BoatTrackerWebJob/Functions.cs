using System;
using System.IO;
using Microsoft.Azure.WebJobs;

namespace BoatTrackerWebJob
{
    public class Functions
    {
        [NoAutomaticTrigger]
        public static void RunPolicyChecks([Blob("container/policychecklog.txt")] TextWriter log)
        {
            log.WriteLine($"Policy checks starting at {DateTime.UtcNow.ToString()}");

            // TODO: implement policy checks
            // TODO: can we arrange to run the checks at 1am *local time* for each club?

            log.WriteLine($"Policy checks complete at {DateTime.UtcNow.ToString()}");
        }
    }
}
