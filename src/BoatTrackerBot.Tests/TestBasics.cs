using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestBasics
    {
        /// <summary>
        /// Verify help messages for the None intent.
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task TestHelpMessages()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User1));

            steps.Add(new BotTestCase
            {
                Action = "hi",
                ExpectedReply = "I can help you with:"
            });

            steps.Add(new BotTestCase
            {
                Action = "what is your favorite color?",
                ExpectedReply = "I'm sorry, I don't understand. Enter '?' to see what you can say at any time."
            });

            steps.Add(new BotTestCase
            {
                Action = "?",
                ExpectedReply = "I can help you with:"
            });

            steps.Add(new BotTestCase
            {
                Action = "help",
                ExpectedReply = "I can help you with:"
            });

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Verify that boat access permissions work correctly.
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task TestPermissions()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            //
            // testuser1
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User1));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "is the nina free tomorrow",
                ExpectedReply = "I'm sorry, but you don't have permission"
            });

            steps.Add(new BotTestCase
            {
                Action = "is the santa maria free tomorrow",
                ExpectedReply = "I'm sorry, but you don't have permission"
            });

            steps.Add(TestUtils.SignOut());

            //
            // testuser2
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User2));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "santa maria"
            });

            steps.Add(new BotTestCase
            {
                Action = "is the nina free tomorrow",
                ExpectedReply = "I'm sorry, but you don't have permission"
            });

            steps.Add(TestUtils.SignOut());

            //
            // testuser3
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User3));

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "nina"
            });

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "pinta"
            });

            steps.Add(new BotTestCase
            {
                Action = "what boats can i use",
                ExpectedReply = "santa maria"
            });

            steps.Add(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
