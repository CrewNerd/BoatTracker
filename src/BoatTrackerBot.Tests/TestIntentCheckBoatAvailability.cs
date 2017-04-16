using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentCheckBoatAvailability
    {
        [ClassInitialize]
        public static void CreateReservations(TestContext context)
        {
            TestRunner.EnsureAllReservationsCleared(context).Wait();

            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
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
        public async Task CheckAvailabilityUser1()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User1));

            steps.Add(new BotTestCase
            {
                Action = "what boats are available next thursday",
                ExpectedReply = "I don't see any reservations on"
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles are available next thursday",
                ExpectedReply = "I don't see any reservations for singles on"
            });

            steps.Add(new BotTestCase
            {
                Action = "is the pinto available next thursday",
                ExpectedReply = "I don't see any reservations for the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "what boats are available next friday",
                ExpectedReply = "9:00 AM Pinta (2 hours)  Test User4",
                Verified = (s) => Assert.IsFalse(s.ToLower().Contains("santa maria"), "Variation 1")
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles are available next friday",
                ExpectedReply = "9:00 AM Pinta (2 hours)  Test User4",
                Verified = (s) => Assert.IsFalse(s.ToLower().Contains("santa maria"), "Variation 2")
            });

            steps.Add(new BotTestCase
            {
                Action = "is the pinte available next friday",
                ExpectedReply = "9:00 AM Pinta (2 hours)  Test User4",
                Verified = (s) => Assert.IsFalse(s.ToLower().Contains("santa maria"), "Variation 3")
            });

            steps.Add(new BotTestCase
            {
                Action = "is the santa maria available next friday",
                ExpectedReply = "I'm sorry, but you don't have permission to use the Santa Maria."
            });

            steps.Add(new BotTestCase
            {
                Action = "is the foobar available next friday",
                ExpectedReply = "I'm sorry, but I didn't find any good matches for 'foobar' in your club's boat list."
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        [TestMethod]
        public async Task CheckAvailabilityUser2()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            steps.Add(new BotTestCase
            {
                Action = "what boats are available next thursday",
                ExpectedReply = "I don't see any reservations on"
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles are available next thursday",
                ExpectedReply = "I don't see any reservations for singles on"
            });

            steps.Add(new BotTestCase
            {
                Action = "is the pinto available next thursday",
                ExpectedReply = "I don't see any reservations for the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "what boats are available next friday",
                ExpectedReply = "9:00 AM Pinta (2 hours)  Test User4",
                Verified = (s) => Assert.IsTrue(s.ToLower().Contains("santa maria") && s.ToLower().Contains("pinta"), "Variation 1")
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles are available next friday",
                ExpectedReply = "9:00 AM Pinta (2 hours)  Test User4",
                Verified = (s) => Assert.IsFalse(s.ToLower().Contains("santa maria"), "Variation 2")
            });

            steps.Add(new BotTestCase
            {
                Action = "what doubles are available next friday",
                ExpectedReply = "2:00 PM Santa Maria w/ Test User2 (2 hours)  Test User4",
                Verified = (s) => Assert.IsFalse(s.ToLower().Contains("pinta"), "Variation 3")
            });

            steps.Add(new BotTestCase
            {
                Action = "is the pinte available next friday",
                ExpectedReply = "9:00 AM Pinta (2 hours)  Test User4"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
