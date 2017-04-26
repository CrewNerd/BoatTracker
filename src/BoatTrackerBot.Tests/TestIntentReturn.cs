using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentReturn
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            TestRunner.EnsureAllReservationsCleared(context).Wait();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task ReturnSuccessScenarios()
        {
            var steps = new List<BotTestCase>();

            var reservationTime = await TestUtils.TimeOfCurrentReservation();

            // TODO: This test will fail if it's after 11:15pm. Force success or failure
            // here if that's the case.

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            //
            // Reserve the pinta and take it out
            //
            steps.Add(new BotTestCase
            {
                Action = $"reserve the pinta {reservationTime} for 30 minutes",
                ExpectedReply = "You want to reserve the Pinta on",
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set!",
            });

            steps.Add(new BotTestCase
            {
                Action = "start rowing the pinta",
                ExpectedReply = "Okay, you're all set to take out the Pinta. Your reservation ends at",
            });

            //
            // Reserve the nina and take it out
            //
            steps.Add(new BotTestCase
            {
                Action = $"reserve the nina {reservationTime} for 30 minutes",
                ExpectedReply = "You want to reserve the Nina on",
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set!",
            });

            steps.Add(new BotTestCase
            {
                Action = "start rowing the nina",
                ExpectedReply = "Okay, you're all set to take out the Nina. Your reservation ends at",
            });

            await Task.Delay(TimeSpan.FromMinutes(1));

            steps.Add(new BotTestCase
            {
                Action = "done rowing",
                ExpectedReply = "It looks like you have more than one current or recent reservation that hasn't been closed out. Please try again and include the name of the boat."
            });

            steps.Add(new BotTestCase
            {
                Action = $"done rowing the pinta",
                ExpectedReply = "Okay, you're good to go. Thanks!",
            });

            steps.Add(new BotTestCase
            {
                Action = "done rowing",
                ExpectedReply = "Okay, you're good to go. Thanks!",
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task ReturnWithNoReservation()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            steps.Add(new BotTestCase
            {
                Action = "Done rowing",
                ExpectedReply = "Sorry, but I don't see a current (or recent) reservation for you to check out of at this time."
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }
    }
}
