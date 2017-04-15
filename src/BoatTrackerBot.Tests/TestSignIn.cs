using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoatTrackerBot.Tests
{
    [TestClass]
    public class TestSignIn
    {
        /// <summary>
        /// Verify that we can sign in and out of all test accounts.
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task AllAccounts()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User1));
            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User2));
            steps.AddRange(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User3));
            steps.AddRange(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Test invalid club initials
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task BadClubInitials()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.Add(new BotTestCase
            {
                Action = "hi",
                ExpectedReply = "What are the initials"
            });

            steps.Add(new BotTestCase
            {
                Action = "abc",
                ExpectedReply = "Sorry, but I don't recognize the initials you entered."
            });

            steps.Add(new BotTestCase
            {
                Action = "xyz",
                ExpectedReply = "Sorry, but I don't recognize the initials you entered."
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your sign-in. You can retry again later."
            });

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Test invalid username
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task BadUsername()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.Add(new BotTestCase
            {
                Action = "hi",
                ExpectedReply = "What are the initials"
            });

            steps.Add(new BotTestCase
            {
                Action = "pnw",
                ExpectedReply = "What is your username?"
            });

            steps.Add(new BotTestCase
            {
                Action = "unknownuser",
                ExpectedReply = "I don't see that username in the roster for PNW Test Site. You may"
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your sign-in. You can retry again later."
            });

            await TestRunner.RunTestCases(steps, null, 0);
        }

        /// <summary>
        /// Test invalid password
        /// </summary>
        /// <returns>Task that completes when the test finishes.</returns>
        [TestMethod]
        public async Task BadPassword()
        {
            var steps = new List<BotTestCase>();

            steps.AddRange(TestUtils.SignOut());

            steps.Add(new BotTestCase
            {
                Action = "hi",
                ExpectedReply = "What are the initials"
            });

            steps.Add(new BotTestCase
            {
                Action = "pnw",
                ExpectedReply = "What is your username?"
            });

            steps.Add(new BotTestCase
            {
                Action = "testuser1",
                ExpectedReply = "What is your password?"
            });

            steps.Add(new BotTestCase
            {
                Action = "password",
                ExpectedReply = "What is your password?"
            });

            steps.Add(new BotTestCase
            {
                Action = "abcd",
                ExpectedReply = "I'm sorry but your password is incorrect. Please try again."
            });

            steps.Add(new BotTestCase
            {
                Action = "quit",
                ExpectedReply = "Okay, I'm aborting your sign-in. You can retry again later."
            });

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
