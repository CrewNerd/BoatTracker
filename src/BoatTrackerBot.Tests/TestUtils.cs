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

        internal static BotTestCase SignOut()
        {
            return new BotTestCase()
            {
                Action = "reset my account",
                ExpectedReply = "Okay, by the time you read this",
            };
        }
    }
}