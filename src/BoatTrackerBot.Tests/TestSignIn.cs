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
        [TestCategory("Signin")]
        public async Task VerifyAllTestAccounts()
        {
            var steps = new List<BotTestCase>();

            steps.Add(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User1));
            steps.Add(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User2));
            steps.Add(TestUtils.SignOut());

            steps.AddRange(TestUtils.SignIn(TestUtils.User3));
            steps.Add(TestUtils.SignOut());

            await TestRunner.RunTestCases(steps, null, 0);
        }
    }
}
