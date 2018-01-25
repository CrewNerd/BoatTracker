namespace BoatTrackerBot.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal static class TestUtils
    {
        internal class BotUser
        {
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        public static BotUser User1 = new BotUser { UserName = "testuser1", Password = "testuser1" };
        public static BotUser User2 = new BotUser { UserName = "testuser2", Password = "testuser2" };
        public static BotUser User3 = new BotUser { UserName = "testuser3", Password = "testuser3" };
        public static BotUser User4 = new BotUser { UserName = "testuser4", Password = "testuser4" };

        internal static IList<BotTestCase> SignIn(BotUser user)
        {
            return new List<BotTestCase>() {
                new BotTestCase()
                {
                    Action = "hi",
                    ExpectedReply = "What are the initials"
                },

                new BotTestCase()
                {
                    Action = "pnw",
                    ExpectedReply = "What is your username?",
                },

                new BotTestCase()
                {
                    Action = user.UserName,
                    ExpectedReply = "What is your password?",
                },

                new BotTestCase()
                {
                    Action = user.Password,
                    ExpectedReply = "Okay, your account is now initialized",
                }
            };
        }

        internal static IList<BotTestCase> SignOut()
        {
            return new List<BotTestCase>() {
                // Bail out of any form that we're in. Ignored otherwise.
                new BotTestCase()
                {
                    Action = "quit",
                    ExpectedReply = ""
                },

                new BotTestCase()
                {
                    Action = "quit",
                    ExpectedReply = ""
                },

                new BotTestCase()
                {
                    Action = "quit",
                    ExpectedReply = ""
                },

                new BotTestCase()
                {
                    Action = "quit",
                    ExpectedReply = ""
                },

                // This works whether we're signed in or not.
                new BotTestCase()
                {
                    Action = "#!logout",
                    ExpectedReply = "Okay, by the time you read this",
                }
            };
        }

        internal static IList<BotTestCase> CreateTestReservations()
        {
            return new List<BotTestCase>() {
                new BotTestCase
                {
                    Action = "reserve the pinta next friday at 9am for 2 hours",
                    ExpectedReply = "You want to reserve the Pinta on Friday"
                },
                new BotTestCase
                {
                    Action = "y",
                    ExpectedReply = "Okay, you're all set! When it's time"
                },
                new BotTestCase
                {
                    Action = "reserve the santa maria next friday at 2pm for 2 hours with test user2",
                    ExpectedReply = "You want to reserve the Santa Maria with Test User2 on Friday"
                },
                new BotTestCase
                {
                    Action = "y",
                    ExpectedReply = "Okay, you're all set! When it's time"
                }
            };
        }

        /// <summary>
        /// Return a time string appropriate for a reservation starting now. We
        /// have 15 minutes before the reservation is removed, so as long as we
        /// have more than a minute left, start the reservation at the prior
        /// quarter-hour. Otherwise, just wait one minute so we're in the next
        /// quarter-hour with plenty of time.
        /// </summary>
        /// <returns></returns>
        internal static async Task<string> TimeOfCurrentReservation()
        {
            var now = DateTime.Now;

            var minutesSinceQuarterHour = now.Minute % 15;

            if (minutesSinceQuarterHour <  14)
            {
                var t = now - TimeSpan.FromMinutes(minutesSinceQuarterHour);
                return $"on {t.Month}/{t.Day} at {t.ToShortTimeString()}";
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
            return await TimeOfCurrentReservation();
        }
    }
}