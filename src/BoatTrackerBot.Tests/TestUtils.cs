namespace BoatTrackerBot.Tests
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                /*
                new BotTestCase()
                {
                    Action = "quit",
                    ExpectedReply = ""
                },
                */

                new BotTestCase()
                {
                    Action = "reset my account",
                    ExpectedReply = "Okay, by the time you read this",
                }
            };
        }

        internal static IList<BotTestCase> CreateTwoReservations()
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
    }
}