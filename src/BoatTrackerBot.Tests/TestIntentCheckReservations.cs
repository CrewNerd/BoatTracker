using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentCheckReservations
    {
        [ClassInitialize]
        public static void CreateReservations(TestContext context)
        {
            TestRunner.EnsureAllReservationsCleared(context).Wait();

            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            //
            // Make a couple of reservations
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));
            steps.AddRange(TestUtils.CreateTwoReservations());
            steps.AddRange(TestUtils.SignOut());

            TestRunner.RunTestCases(steps, null, 0).Wait();
        }

        [ClassCleanup]
        public static void RemoveReservations()
        {
            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task CheckReservations()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            steps.Add(new BotTestCase
            {
                Action = "show my reservations",
                ExpectedReply = "I found the following reservations for you:",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("9:00 am pinta (2 hours)"), "Pinta reservation missing");
                    Assert.IsTrue(reply.Contains("2:00 pm santa maria w/ test user2 (2 hours)"), "Santa Maria reservation missing");
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "show my reservations for next friday",
                ExpectedReply = "I found the following reservations for you on ",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("9:00 am pinta (2 hours)"), "Pinta reservation missing");
                    Assert.IsTrue(reply.Contains("2:00 pm santa maria w/ test user2 (2 hours)"), "Santa Maria reservation missing");
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "show my reservations for next thursday",
                ExpectedReply = "I don't see any reservations for you on"
            });

            steps.Add(new BotTestCase
            {
                Action = "show reservations for the pinta",
                ExpectedReply = "I found the following reservation for the Pinta:",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("9:00 am pinta (2 hours)"), "Pinta reservation missing");
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "show reservations for the pinta next friday",
                ExpectedReply = "I found the following reservation for the Pinta on",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("9:00 am pinta (2 hours)"), "Pinta reservation missing");
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "show my reservations for the pinta next thursday",
                ExpectedReply = "I don't see any reservations for the Pinta on"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
