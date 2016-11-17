using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json.Linq;

using BoatTracker.Bot.Configuration;

namespace BoatTracker.BookedScheduler
{
    /// <summary>
    /// Wrapper class for BookedSchedulerClient that adds logging and retries.
    /// </summary>
    public class BookedSchedulerLoggingClient : BookedSchedulerClient, ITransientErrorDetectionStrategy
    {
        private string dependencyName;
        private TelemetryClient telemetryClient;

        private DateTime callStartTime;
        private Stopwatch callTimer;

        private RetryStrategy retryStrategy;
        private RetryPolicy retryPolicy;

        public BookedSchedulerLoggingClient(string clubId, bool isInteractive)
            : base(
                  EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId].Url,
                  TimeSpan.FromSeconds(isInteractive ? 10 : 30))
        {
            this.dependencyName = "bs_" + clubId;
            this.telemetryClient = new TelemetryClient();

            if (isInteractive)
            {
                this.retryStrategy = new Incremental(5, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
            }
            else
            {
                this.retryStrategy = new ExponentialBackoff(10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0.5));
            }

            this.retryPolicy = new RetryPolicy(this, this.retryStrategy);
        }

        public bool IsTransient(Exception ex)
        {
            // TODO: Be more discriminating here...
            return true;
        }

        private TResult DoCallWithLogging<TResult>(string name, Func<TResult> func)
        {
            this.callStartTime = DateTime.UtcNow;
            this.callTimer = Stopwatch.StartNew();

            bool success = true;
            try
            {
                return func.Invoke();
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.callTimer.Stop();
                this.telemetryClient.TrackDependency(this.dependencyName, name, this.callStartTime, this.callTimer.Elapsed, success);
            }
        }

        private TResult DoCallWithRetry<TResult>(string name, Func<TResult> func)
        {
            return this.retryPolicy.ExecuteAction(() => this.DoCallWithLogging(name, func));
        }

        /// <summary>
        /// Convert a member name to the form we prefer for logging. Remove the trailing "Async"
        /// if present, and convert to camel-casing.
        /// </summary>
        /// <param name="caller">The calling method name</param>
        /// <returns>The caller name adjusted for logging.</returns>
        private string FixName([CallerMemberName] string caller = null)
        {
            return char.ToLower(caller[0]) + caller.Replace("Async", string.Empty).Substring(1);
        }

        public override Task<JToken> CreateReservationAsync(
            JToken boat,
            long userId,
            DateTimeOffset start,
            TimeSpan duration,
            string title = null,
            string description = null,
            long? secondUserId = null)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.CreateReservationAsync(boat, userId, start, duration, title, description, secondUserId));
        }

        public override Task DeleteReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.DeleteReservationAsync(referenceNumber));
        }

        public override Task<JToken> GetGroupAsync(long groupId)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetGroupAsync(groupId));
        }

        public override Task<JArray> GetGroupsAsync()
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetGroupsAsync());
        }

        public override Task<JToken> GetReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetReservationAsync(referenceNumber));
        }

        public override Task<JToken> CheckInReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.CheckInReservationAsync(referenceNumber));
        }

        public override Task<JToken> CheckOutReservationAsync(string referenceNumber)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.CheckOutReservationAsync(referenceNumber));
        }

        public override Task<JArray> GetReservationsAsync(long? userId = null, long? resourceId = null, DateTime? start = null, DateTime? end = null)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetReservationsAsync(userId, resourceId, start, end));
        }

        public override Task<JArray> GetReservationsForUserAsync(long userId)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetReservationsForUserAsync(userId));
        }

        public override Task<JToken> GetResourceAsync(long resourceId)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetResourceAsync(resourceId));
        }

        public override Task<JArray> GetResourcesAsync()
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetResourcesAsync());
        }

        public override Task<JToken> GetScheduleAsync(string scheduleId)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetScheduleAsync(scheduleId));
        }

        public override Task<JArray> GetSchedulesAsync()
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetSchedulesAsync());
        }

        public override Task<JToken> GetScheduleSlotsAsync(string scheduleId)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetScheduleSlotsAsync(scheduleId));
        }

        public override Task<JToken> GetUserAsync(long userId)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetUserAsync(userId));
        }

        public override Task<JArray> GetUsersAsync()
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.GetUsersAsync());
        }

        public override Task SignInAsync(string userName, string password)
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.SignInAsync(userName, password));
        }

        public override Task SignOutAsync()
        {
            return this.DoCallWithRetry(
                this.FixName(),
                () => base.SignOutAsync());
        }
    }
}