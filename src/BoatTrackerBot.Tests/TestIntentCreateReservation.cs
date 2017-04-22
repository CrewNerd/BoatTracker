using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentCreateReservation
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
        public async Task AllWeekdays()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            var weekdays = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };

            foreach (var day in weekdays)
            {
                steps.Add(new BotTestCase
                {
                    Action = $"reserve the pinta next {day} at 5am for 1 hour",
                    ExpectedReply = "You want to reserve the Pinta on"
                });

                steps.Add(new BotTestCase
                {
                    Action = "y",
                    ExpectedReply = "Okay, you're all set! When it's time for your reservation"
                });
            }

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task MissingBoatSlot()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User1));

            //
            // Basic success case
            //
            steps.Add(new BotTestCase
            {
                Action = "make a reservation next friday at 5am for 1 hour",
                ExpectedReply = "What boat do you want to reserve?"
            });

            steps.Add(new BotTestCase
            {
                Action = "pinto",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            //
            // Test boat name validation cases
            //
            steps.Add(new BotTestCase
            {
                Action = "make a reservation next friday at 7am for 1 hour",
                ExpectedReply = "What boat do you want to reserve?"
            });

            steps.Add(new BotTestCase
            {
                Action = "pinafore",
                ExpectedReply = "I'm sorry, but I didn't find any good matches for 'pinafore' in your club's boat list",
                Verified = (s) => Assert.IsTrue(s.Contains("What boat do you want to reserve?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "santa maria",
                ExpectedReply = "Sorry, but you don't have permission to use that boat.",
                Verified = (s) => Assert.IsTrue(s.Contains("What boat do you want to reserve?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "lemon",
                ExpectedReply = "I'm sorry, but that boat is currently unavailable for use.",
                Verified = (s) => Assert.IsTrue(s.Contains("What boat do you want to reserve?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "pinte",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task MissingPartnerSlot()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            //
            // Basic success case
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the santa maria next friday at 5am for 1 hour",
                ExpectedReply = "Who are you rowing with?"
            });

            steps.Add(new BotTestCase
            {
                Action = "testuser3",
                ExpectedReply = "You want to reserve the Santa Maria with Test User3 on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            //
            // Test partner name validation cases
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the santa maria next friday at 7am for 1 hour",
                ExpectedReply = "Who are you rowing with?"
            });

            steps.Add(new BotTestCase
            {
                Action = "donald trump",
                ExpectedReply = "I'm sorry, but I didn't find any good matches for 'donald trump' in your club's member list",
                Verified = (s) => Assert.IsTrue(s.Contains("Who are you rowing with?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "testuser1",
                ExpectedReply = "Sorry, but your partner doesn't have permission to use that boat.",
                Verified = (s) => Assert.IsTrue(s.Contains("Who are you rowing with?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "testuser3",
                ExpectedReply = "You want to reserve the Santa Maria with Test User3 on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task MissingStartDateSlot()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            //
            // Basic success case
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the pinta at 1am for 1 hour",
                ExpectedReply = "What day do you want to reserve it?"
            });

            steps.Add(new BotTestCase
            {
                Action = "next thursday",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            //
            // Test start date validation
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the pinta at 1am for 1 hour",
                ExpectedReply = "What day do you want to reserve it?"
            });

            steps.Add(new BotTestCase
            {
                // BUG: we should be able to say "yesterday" here, but there's a bug in the
                // parsing of this field in the ReservationRequest dialog in that the local
                // time zone isn't being honored, so late in the day, "yesterday" returns 
                // the current date.
                Action = "two days ago",
                ExpectedReply = "You can't make reservations in the past",
                Verified = (s) => Assert.IsTrue(s.Contains("What day do you want to reserve it?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "three weeks from tomorrow",
                ExpectedReply = "You can only make reservations for the next two weeks",
                Verified = (s) => Assert.IsTrue(s.Contains("What day do you want to reserve it?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "next friday",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task MissingStartTimeSlot()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            //
            // Basic success case
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the pinta next thursday for 1 hour",
                ExpectedReply = "What time do you want to start?"
            });

            steps.Add(new BotTestCase
            {
                Action = "1am",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            //
            // Test start time validation
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the pinta next friday for 1 hour",
                ExpectedReply = "What time do you want to start?"
            });

            steps.Add(new BotTestCase
            {
                Action = "1:05am",
                ExpectedReply = "Reservations must start on an even 15-minute slot.",
                Verified = (s) => Assert.IsTrue(s.Contains("What time do you want to start?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "12am",
                ExpectedReply = "Reservations can't be made earlier than",
                Verified = (s) => Assert.IsTrue(s.Contains("What time do you want to start?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "11:45pm",
                ExpectedReply = "Reservations can't be made later than",
                Verified = (s) => Assert.IsTrue(s.Contains("What time do you want to start?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "1am",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task MissingDurationSlot()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            //
            // Basic success case
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the pinta next thursday at 5am",
                ExpectedReply = "How long do you want to use the boat?"
            });

            steps.Add(new BotTestCase
            {
                Action = "1 hour",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            //
            // Test duration validation
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the pinta next friday at 5am",
                ExpectedReply = "How long do you want to use the boat?"
            });

            steps.Add(new BotTestCase
            {
                Action = "for as long as I feel like it",
                ExpectedReply = "That doesn't look like a valid duration. Please try again.",
                Verified = (s) => Assert.IsTrue(s.Contains("How long do you want to use the boat?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "20 minutes",
                ExpectedReply = "Reservations must last for quarter-hour increments.",
                Verified = (s) => Assert.IsTrue(s.Contains("How long do you want to use the boat?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "15 minutes",
                ExpectedReply = "Reservations must be for at least",
                Verified = (s) => Assert.IsTrue(s.Contains("How long do you want to use the boat?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "5 hours",
                ExpectedReply = "The maximum duration allowed is",
                Verified = (s) => Assert.IsTrue(s.Contains("How long do you want to use the boat?"))
            });

            steps.Add(new BotTestCase
            {
                Action = "1 hour",
                ExpectedReply = "You want to reserve the Pinta on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            //
            // Verify that users can exceed the club's maximum duration when reserving a private boat.
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the shadowfax next thursday at 7am for 5 hours",
                ExpectedReply = "You want to reserve the Shadowfax on"
            });

            steps.Add(new BotTestCase
            {
                Action = "y",
                ExpectedReply = "Okay, you're all set! When it's time for your reservation"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

        [TestMethod]
        public async Task MiscFailureScenarios()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            //
            // Misc failure cases
            //
            steps.Add(new BotTestCase
            {
                Action = "reserve the shadowfax next thursday at 5am for 1 hour",
                ExpectedReply = "I'm sorry, but you don't have permission to use the Shadowfax"
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.Add(new BotTestCase
            {
                Action = "reserve the lemon next thursday at 5am for 1 hour",
                ExpectedReply = "I'm sorry, but the Lemon is currently unavailable for use."
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.Add(new BotTestCase
            {
                Action = "reserve the santa maria next thursday at 5am for 1 hour with test user1",
                ExpectedReply = "I'm sorry, but Test User1 doesn't have permission to use the Santa Maria."
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.Add(new BotTestCase
            {
                Action = "reserve the santa maria next thursday at 5am for 1 hour with donald trump",
                ExpectedReply = "I'm sorry, but I didn't find any good matches for 'donald trump' in your club's member list."
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.Add(new BotTestCase
            {
                Action = "reserve the pinafore next thursday at 5am for 1 hour",
                ExpectedReply = "I'm sorry, but I didn't find any good matches for 'pinafore' in your club's boat list."
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your reservation request."
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);

            TestRunner.EnsureAllReservationsCleared(General.testContext).Wait();
        }

    }
}
