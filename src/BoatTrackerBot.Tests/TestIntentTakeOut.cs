using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentTakeOut
    {
        [ClassCleanup]
        public static void Cleanup()
        {
            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task CheckinWithMultipleReservations()
        {
            var steps = new List<BotTestCase>();

            var reservationTime = await TestUtils.TimeOfCurrentReservation();

            // TODO: This test will fail if it's after 11:15pm. Force success or failure
            // here if that's the case.

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

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
                Action = "start rowing",
                ExpectedReply = "It looks like you have more than one reservation starting about now. Can you be more specific?",
            });

            steps.Add(new BotTestCase
            {
                Action = "start rowing the pinta",
                ExpectedReply = "Okay, you're all set to take out the Pinta. Your reservation ends at",
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }
    }
}
