using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentCancelReservation
    {
        [ClassCleanup]
        public static void RemoveReservations()
        {
            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task CancelFromList()
        {
            await TestRunner.EnsureAllReservationsCleared(General.testContext);

            var steps = new List<BotTestCase>();
            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));
            steps.AddRange(TestUtils.CreateTwoReservations());

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation",
                ExpectedReply = "I found multiple reservations for you:",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("please enter the number of the reservation you want to cancel, or 3 for 'none'."));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "3",
                ExpectedReply = "Okay, I'm leaving all of your reservations unchanged."
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation next friday",
                ExpectedReply = "I found multiple reservations for you on",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("please enter the number of the reservation you want to cancel, or 3 for 'none'."));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "3",
                ExpectedReply = "Okay, I'm leaving all of your reservations unchanged."
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation",
                ExpectedReply = "I found multiple reservations for you:",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("please enter the number of the reservation you want to cancel, or 3 for 'none'."));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "4",
                ExpectedReply = "The index you entered is invalid, so no reservation was cancelled."
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation",
                ExpectedReply = "I found multiple reservations for you:",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("please enter the number of the reservation you want to cancel, or 3 for 'none'."));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "abc",
                ExpectedReply = "I'm sorry, but that isn't a valid response. Please select one of the options listed above."
            });

            steps.Add(new BotTestCase
            {
                Action = "def",
                ExpectedReply = "I'm sorry, but that isn't a valid response. Please select one of the options listed above."
            });

            steps.Add(new BotTestCase
            {
                Action = "ghi",
                ExpectedReply = "I'm sorry, but that isn't a valid response. Please select one of the options listed above."
            });

            steps.Add(new BotTestCase
            {
                Action = "jkl",
                ExpectedReply = "too many attempts",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("since you didn't select one of the listed options, i'm leaving all of your reservations unchanged."));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation",
                ExpectedReply = "I found multiple reservations for you:",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("please enter the number of the reservation you want to cancel, or 3 for 'none'."));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "1",
                ExpectedReply = "Okay, that reservation is cancelled now!"
            });

            steps.Add(new BotTestCase
            {
                Action = "show my reservations",
                ExpectedReply = "I found the following reservation for you:"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        [TestMethod]
        public async Task CancelByQuery()
        {
            await TestRunner.EnsureAllReservationsCleared(General.testContext);

            var steps = new List<BotTestCase>();
            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));
            steps.AddRange(TestUtils.CreateTwoReservations());

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation next thursday",
                ExpectedReply = "I don't see any reservations for you on"
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation for the shadowfax",
                ExpectedReply = "I don't see any reservations for the Shadowfax."
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation for the shadowfax next friday",
                ExpectedReply = "I don't see any reservations for the Shadowfax on"
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation for the pinto",
                ExpectedReply = "Is this the reservation you want to cancel?",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("9:00 am pinta (2 hours)"));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "q",
                ExpectedReply = "Sorry, I don't understand your response. Do you want to cancel the reservation shown above? (yes/no)"
            });

            steps.Add(new BotTestCase
            {
                Action = "n",
                ExpectedReply = "Okay, your reservation is unchanged."
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation for the maria",
                ExpectedReply = "Is this the reservation you want to cancel?",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("2:00 pm santa maria w/ test user2 (2 hours)"));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, your reservation is cancelled!"
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation next friday",
                ExpectedReply = "Is this the reservation you want to cancel? (yes/no)",
                Verified = (reply) =>
                {
                    reply = reply.ToLower();
                    Assert.IsTrue(reply.Contains("9:00 am pinta (2 hours)"));
                }
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, your reservation is cancelled!"
            });

            steps.Add(new BotTestCase
            {
                Action = "cancel my reservation",
                ExpectedReply = "I don't see any reservations for you."
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
