namespace BoatTrackerBot.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using BoatTracker.BookedScheduler;

    internal class TestRunner
    {
        internal static async Task RunTestCase(BotTestCase testCase)
        {
            await RunTestCases(new List<BotTestCase> { testCase }, new List<BotTestCase>());
        }

        internal static async Task RunTestCases(IList<BotTestCase> steps, BotTestCase completionTestCase = null, int completionChecks = 1)
        {
            await RunTestCases(steps, new List<BotTestCase> { completionTestCase }, completionChecks);
        }

        internal static async Task RunTestCases(IList<BotTestCase> steps, IList<BotTestCase> completionTestCases = null, int completionChecks = 1, bool strictCheck = true)
        {
            if (completionTestCases != null && completionTestCases.Count > 1 && completionTestCases.Count < completionChecks)
            {
                Assert.Fail($"There are completion test cases missing. Completion Test Cases: {completionTestCases.Count} for {completionChecks} completionChecks");
            }

            foreach (var step in steps)
            {
                await General.BotHelper.SendMessageNoReply(step.Action);

                Action<IList<string>> action = (replies) =>
                {
                    var match = replies.FirstOrDefault(stringToCheck => stringToCheck.ToLowerInvariant().Contains(step.ExpectedReply));
                    Assert.IsTrue(match != null, step.ErrorMessageHandler(step.CallerFile, step.CallerLineNumber, step.Action, step.ExpectedReply, string.Join(", ", replies)));
                    step.Verified?.Invoke(replies.LastOrDefault());
                };
                await General.BotHelper.WaitForLongRunningOperations(action, 1);
            }

            if (completionTestCases != null && completionTestCases.Any())
            {
                Action<IList<string>> action = (replies) =>
                {
                    var singleCompletionTestCase = completionTestCases.Count == 1;

                    for (int i = 0; i < replies.Count(); i++)
                    {
                        if (!strictCheck && completionChecks > replies.Count())
                        {
                            var skip = completionChecks - replies.Count();

                            completionTestCases = completionTestCases.Skip(skip).ToList();
                        }

                        var completionIndex = singleCompletionTestCase ? 0 : i;
                        var completionTestCase = completionTestCases[completionIndex];

                        Assert.IsTrue(
                            replies[i].ToLowerInvariant().Contains(completionTestCase.ExpectedReply.ToLowerInvariant()),
                            completionTestCase.ErrorMessageHandler(
                                completionTestCase.CallerFile,
                                completionTestCase.CallerLineNumber,
                                completionTestCase.Action,
                                completionTestCase.ExpectedReply,
                                replies[i]));

                        completionTestCase.Verified?.Invoke(replies[i]);
                    }
                };

                await General.BotHelper.WaitForLongRunningOperations(action, completionChecks);
            }
        }


        /// <summary>
        /// Remove all reservations from our test site between each test.
        /// </summary>
        /// <param name="context">The text context</param>
        /// <returns>Task that finishes when the reservations are removed.</returns>
        internal static async Task EnsureAllReservationsCleared(TestContext context)
        {
            var client = new BookedSchedulerClient(new Uri(context.GetBookedSchedulerUrl()), TimeSpan.FromSeconds(10));

            await client.SignInAsync(context.GetBotUsername(), context.GetBotPassword());

            var reservations = await client.GetReservationsAsync(start: DateTime.Now - TimeSpan.FromMinutes(30));

            foreach (var r in reservations)
            {
                await client.DeleteReservationAsync(r.ReferenceNumber());
            }
        }
    }
}