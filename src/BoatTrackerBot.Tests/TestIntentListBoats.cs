using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestIntentListBoats
    {
        [TestMethod]
        public async Task ListBoatsUser1()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User1));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "Pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles can i use",
                ExpectedReply = "Pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "what doubles can i use",
                ExpectedReply = "You don't have permission to use any boats, currently."
            });

            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        [TestMethod]
        public async Task ListBoatsUser2()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "Pinta, Santa Maria"
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles can i use",
                ExpectedReply = "Pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "what doubles can i use",
                ExpectedReply = "Santa Maria"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        [TestMethod]
        public async Task ListBoatsUser3()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User3));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "Nina, Pinta, Santa Maria"
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles can i use",
                ExpectedReply = "Nina, Pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "what doubles can i use",
                ExpectedReply = "Santa Maria"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        [TestMethod]
        public async Task ListBoatsUser4()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "Nina, Pinta, Santa Maria, Shadowfax"
            });

            steps.Add(new BotTestCase
            {
                Action = "what singles can i use",
                ExpectedReply = "Nina, Pinta, Shadowfax"
            });

            steps.Add(new BotTestCase
            {
                Action = "what doubles can i use",
                ExpectedReply = "Santa Maria"
            });

            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
