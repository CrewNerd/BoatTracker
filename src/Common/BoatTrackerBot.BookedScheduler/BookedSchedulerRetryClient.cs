﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using BoatTracker.Bot.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json.Linq;

namespace BoatTracker.BookedScheduler
{
    /// <summary>
    /// Wrapper class for BookedSchedulerClient that adds logging and retries.
    /// </summary>
    [Serializable]
    public class BookedSchedulerRetryClient : BookedSchedulerClient, ITransientErrorDetectionStrategy
    {
        private string dependencyName;
        private bool isInteractive;

        [NonSerialized]
        private string inProgressCallName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BookedSchedulerRetryClient"/> class.
        /// </summary>
        /// <param name="clubId">The clubId we communicate with.</param>
        /// <param name="isInteractive">True if we're being used in an interactive scenario.</param>
        public BookedSchedulerRetryClient(string clubId, bool isInteractive)
            : base(
                  EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId].Url,
                  TimeSpan.FromSeconds(isInteractive ? 20 : 30))
        {
            this.isInteractive = isInteractive;
            this.dependencyName = "bs_" + clubId;
        }

        #region BookedScheduler API wrappers

        public override Task<JToken> CreateReservationAsync(
            JToken boat,
            long userId,
            DateTimeOffset start,
            TimeSpan duration,
            string title = null,
            string description = null,
            long? secondUserId = null)
        {
            return this.DoCallWithRetry(() => base.CreateReservationAsync(boat, userId, start, duration, title, description, secondUserId));
        }

        public override Task DeleteReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(() => base.DeleteReservationAsync(referenceNumber));
        }

        public override Task<JToken> GetGroupAsync(long groupId)
        {
            return this.DoCallWithRetry(() => base.GetGroupAsync(groupId));
        }

        public override Task<JArray> GetGroupsAsync()
        {
            return this.DoCallWithRetry(() => base.GetGroupsAsync());
        }

        public override Task<JToken> GetReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(() => base.GetReservationAsync(referenceNumber));
        }

        public override Task<JToken> CheckInReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(() => base.CheckInReservationAsync(referenceNumber));
        }

        public override Task<JToken> CheckOutReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(() => base.CheckOutReservationAsync(referenceNumber));
        }

        public override Task<JArray> GetReservationsAsync(long? userId = null, long? resourceId = null, DateTime? start = null, DateTime? end = null)
        {
            return this.DoCallWithRetry(() => base.GetReservationsAsync(userId, resourceId, start, end));
        }

        public override Task<JArray> GetReservationsForUserAsync(long userId)
        {
            return this.DoCallWithRetry(() => base.GetReservationsForUserAsync(userId));
        }

        public override Task<JToken> GetResourceAsync(long resourceId)
        {
            return this.DoCallWithRetry(() => base.GetResourceAsync(resourceId));
        }

        public override Task<JArray> GetResourcesAsync()
        {
            return this.DoCallWithRetry(() => base.GetResourcesAsync());
        }

        public override Task<JToken> GetScheduleAsync(string scheduleId)
        {
            return this.DoCallWithRetry(() => base.GetScheduleAsync(scheduleId));
        }

        public override Task<JArray> GetSchedulesAsync()
        {
            return this.DoCallWithRetry(() => base.GetSchedulesAsync());
        }

        public override Task<JToken> GetScheduleSlotsAsync(string scheduleId)
        {
            return this.DoCallWithRetry(() => base.GetScheduleSlotsAsync(scheduleId));
        }

        public override Task<JToken> GetUserAsync(long userId)
        {
            return this.DoCallWithRetry(() => base.GetUserAsync(userId));
        }

        public override Task<JArray> GetUsersAsync()
        {
            return this.DoCallWithRetry(() => base.GetUsersAsync());
        }

        public override Task SignInAsync(string userName, string password)
        {
            return this.DoCallWithRetry(() => base.SignInAsync(userName, password));
        }

        public override Task SignOutAsync()
        {
            return this.DoCallWithRetry(() => base.SignOutAsync());
        }

        #endregion

        #region Retry helpers

        /// <summary>
        /// Perform the given function with a retry policy as appropriate depending on whether this is
        /// an interactive or background situation.
        /// </summary>
        /// <typeparam name="TResult">The result type of the function.</typeparam>
        /// <param name="func">The function to be performed with retries.</param>
        /// <param name="name">The name of the operation (for logging).</param>
        /// <returns>The result of the given function.</returns>
        private TResult DoCallWithRetry<TResult>(
            Func<TResult> func,
            [CallerMemberName] string name = null)
        {
            this.inProgressCallName = char.ToLower(name[0]) + name.Replace("Async", string.Empty).Substring(1);

            RetryStrategy retryStrategy;

            if (this.isInteractive)
            {
                retryStrategy = new Incremental(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
            }
            else
            {
                retryStrategy = new ExponentialBackoff(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0.5));
            }

            var retryPolicy = new RetryPolicy(this, retryStrategy);

            return retryPolicy.ExecuteAction(() => func.Invoke());
        }

        /// <summary>
        /// Returns true if the given exception indicates a condition that should be retried. We always log
        /// the exception, and for now we retry everything.
        /// </summary>
        /// <param name="ex">The exception that was thrown.</param>
        /// <returns>True if the retry framework should retry this exception.</returns>
        public bool IsTransient(Exception ex)
        {
            // TODO: Be more discriminating here...
            bool requestRetry = true;

            new TelemetryClient().TrackException(
                ex,
                new Dictionary<string, string>
                {
                    ["willRetry"] = requestRetry.ToString(),
                    ["dependency"] = this.dependencyName,
                    ["name"] = this.inProgressCallName
                });

            return requestRetry;
        }

        #endregion
    }
}