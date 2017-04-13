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

        /// <summary>
        /// Verify that boats can be referenced properly
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task TestBoatNames()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User3));

            // Normal name
            steps.Add(new BotTestCase
            {
                Action = "is the pinta free tomorrow",
                ExpectedReply = "I don't see any reservations for the Pinta on"
            });

            // First alias
            steps.Add(new BotTestCase
            {
                Action = "is the pinto free tomorrow",
                ExpectedReply = "I don't see any reservations for the Pinta on"
            });

            // Second alias
            steps.Add(new BotTestCase
            {
                Action = "is the pinte free tomorrow",
                ExpectedReply = "I don't see any reservations for the Pinta on"
            });

            // unknown boat name
            steps.Add(new BotTestCase
            {
                Action = "is the pinafore free tomorrow",
                ExpectedReply = "I'm sorry, but I didn't find any good matches for 'pinafore' in your club's boat list."
            });

            // one word of a multi-word name
            steps.Add(new BotTestCase
            {
                Action = "is the santa free tomorrow",
                ExpectedReply = "I don't see any reservations for the Santa Maria on"
            });

            // other word of a multi-word name
            steps.Add(new BotTestCase
            {
                Action = "is the maria free tomorrow",
                ExpectedReply = "I don't see any reservations for the Santa Maria on"
            });

            steps.Add(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Verify that boats can be referenced using the 'my' affordance for
        /// a user who doesn't own any boats.
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task TestMyBoatForUser1()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            //
            // testuser1 doesn't own anything
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User1));

            steps.Add(new BotTestCase
            {
                Action = "is my boat free tomorrow",
                ExpectedReply = "I'm sorry, but according to your club's boat list, you don't own a boat."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my single free tomorrow",
                ExpectedReply = "I'm sorry, but I don't see a single that you own in your club's boat list."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my 1x free tomorrow",
                ExpectedReply = "I'm sorry, but I don't see a 1x that you own in your club's boat list."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my double free tomorrow",
                ExpectedReply = "I'm sorry, but I don't see a double that you own in your club's boat list."
            });

            steps.Add(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Verify that boats can be referenced using the 'my' affordance
        /// for a user who owns one single.
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task TestMyBoatForUser3()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            //
            // testuser3 own one single
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User3));

            steps.Add(new BotTestCase
            {
                Action = "is my boat free tomorrow",
                ExpectedReply = "I don't see any reservations for the Nina on"
            });

            steps.Add(new BotTestCase
            {
                Action = "is my single free tomorrow",
                ExpectedReply = "I don't see any reservations for the Nina on"
            });

            steps.Add(new BotTestCase
            {
                Action = "is my 1x free tomorrow",
                ExpectedReply = "I don't see any reservations for the Nina on"
            });

            steps.Add(new BotTestCase
            {
                Action = "is my double free tomorrow",
                ExpectedReply = "I'm sorry, but I don't see a double that you own in your club's boat list."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my 2x free tomorrow",
                ExpectedReply = "I'm sorry, but I don't see a 2x that you own in your club's boat list."
            });

            steps.Add(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Verify that boats can be referenced using the 'my' affordance for
        /// a user who doesn't own any boats.
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task TestMyBoatForUser4()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            //
            // testuser4 owns two singles
            //
            steps.AddRange(TestUtils.SignIn(TestUtils.User4));

            steps.Add(new BotTestCase
            {
                Action = "is my boat free tomorrow",
                ExpectedReply = "It looks like multiple boats fit that description. You'll have to refer to the boat by name."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my single free tomorrow",
                ExpectedReply = "It looks like multiple boats fit that description. You'll have to refer to the boat by name."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my 1x free tomorrow",
                ExpectedReply = "It looks like multiple boats fit that description. You'll have to refer to the boat by name."
            });

            steps.Add(new BotTestCase
            {
                Action = "is my double free tomorrow",
                ExpectedReply = "I'm sorry, but I don't see a double that you own in your club's boat list."
            });

            steps.Add(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
