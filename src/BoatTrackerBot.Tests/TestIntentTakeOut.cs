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

        [TestMethod]
        public async Task TakeOutNowWithNoReservation()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            steps.Add(new BotTestCase
            {
                Action = $"take out the pinta for 30 minutes",
                ExpectedReply = "You don't have a reservation, but I can create one for you now.",
                Verified = (reply) =>
                {
                    Assert.IsTrue(reply.Contains("You want to reserve the Pinta on"));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.Add(new BotTestCase
            {
                Action = $"take out the santa maria for 30 minutes with test user2",
                ExpectedReply = "You don't have a reservation, but I can create one for you now.",
                Verified = (reply) =>
                {
                    Assert.IsTrue(reply.Contains("You want to reserve the Santa Maria with Test User2 on"));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request.",
            });

            steps.Add(new BotTestCase
            {
                Action = "take out the santa maria for 30 minutes with donald trump",
                ExpectedReply = "You don't have a reservation, but I can create one for you now.",
                Verified = (reply) =>
                {
                    Assert.IsTrue(reply.Contains("Who are you rowing with?"));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request.",
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task TakeOutLaterWithNoReservation()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            steps.Add(new BotTestCase
            {
                Action = $"take out the pinta next thursday for 30 minutes",
                ExpectedReply = "What time do you want to start?",
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.Add(new BotTestCase
            {
                Action = $"take out the santa maria at 11:15pm for 30 minutes with test user2",
                ExpectedReply = "You want to reserve the Santa Maria with Test User2 on",
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request.",
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }
    }
}
