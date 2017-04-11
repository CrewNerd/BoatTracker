namespace BoatTrackerBot.Tests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public static class General
    {
        private static BotHelper botHelper;
        public static TestContext testContext { get; set; }

        internal static BotHelper BotHelper
        {
            get { return botHelper; }
        }

        // Will run once before all of the tests in the project.
        [AssemblyInitialize]
        public static void SetUp(TestContext context)
        {
            testContext = context;

            botHelper = new BotHelper(
                context.GetDirectLineToken(),
                context.GetMicrosoftAppId(),
                context.GetFromUser(),
                context.GetBotId());

            try
            {
                TestRunner.EnsureAllReservationsCleared().Wait();
            }
            catch
            {
                Console.WriteLine("CleanUp called from SetUp failed");
            }
        }

        // Will run after all the tests have finished
        [AssemblyCleanup]
        public static void CleanUp()
        {
            try
            {
                TestRunner.EnsureAllReservationsCleared().Wait();
            }
            finally
            {
                if (botHelper != null)
                {
                    botHelper.Dispose();
                }
            }
        }
    }
}