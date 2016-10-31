using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;

using BoatTracker.Bot.Configuration;

namespace BoatTracker.BookedScheduler
{
    public class BookedSchedulerLoggingClient : BookedSchedulerClient
    {
        private string dependencyName;
        private TelemetryClient telemetryClient;

        private DateTime callStartTime;
        private Stopwatch callTimer;

        public BookedSchedulerLoggingClient(string clubId)
            : base(EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId].Url)
        {
            this.dependencyName = "BS_" + clubId;
            this.telemetryClient = new TelemetryClient();
        }

        private void StartCall()
        {
            this.callStartTime = DateTime.UtcNow;
            this.callTimer = Stopwatch.StartNew();
        }

        private void FinishCall(string callName, bool success)
        {
            this.callTimer.Stop();
            this.telemetryClient.TrackDependency(this.dependencyName, callName, this.callStartTime, this.callTimer.Elapsed, success);
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
            this.StartCall();

            bool success = true;
            try
            {
                return base.CreateReservationAsync(boat, userId, start, duration, title, description, secondUserId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("createReservation", success);
            }
        }

        public override Task DeleteReservationAsync(string referenceNumber)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.DeleteReservationAsync(referenceNumber);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("deleteReservation", success);
            }
        }

        public override Task<JToken> GetGroupAsync(long groupId)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetGroupAsync(groupId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getGroup", success);
            }
        }

        public override Task<JArray> GetGroupsAsync()
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetGroupsAsync();
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getGroups", success);
            }
        }

        public override Task<JToken> GetReservationAsync(string referenceNumber)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetReservationAsync(referenceNumber);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getReservation", success);
            }
        }

        public override Task<JToken> CheckInReservationAsync(string referenceNumber)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.CheckInReservationAsync(referenceNumber);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("checkinReservation", success);
            }
        }

        public override Task<JToken> CheckOutReservationAsync(string referenceNumber)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.CheckOutReservationAsync(referenceNumber);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("checkoutReservation", success);
            }
        }

        public override Task<JArray> GetReservationsAsync(long? userId = default(long?), long? resourceId = default(long?), DateTime? start = default(DateTime?), DateTime? end = default(DateTime?))
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetReservationsAsync(userId, resourceId, start, end);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getReservations", success);
            }
        }

        public override Task<JArray> GetReservationsForUserAsync(long userId)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetReservationsForUserAsync(userId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getReservationsForUser", success);
            }
        }

        public override Task<JToken> GetResourceAsync(long resourceId)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetResourceAsync(resourceId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getResource", success);
            }
        }

        public override Task<JArray> GetResourcesAsync()
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetResourcesAsync();
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getResources", success);
            }
        }

        public override Task<JToken> GetScheduleAsync(string scheduleId)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetScheduleAsync(scheduleId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getSchedule", success);
            }
        }

        public override Task<JArray> GetSchedulesAsync()
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetSchedulesAsync();
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getSchedules", success);
            }
        }

        public override Task<JToken> GetScheduleSlotsAsync(string scheduleId)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetScheduleSlotsAsync(scheduleId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getScheduleSlots", success);
            }
        }

        public override Task<JToken> GetUserAsync(long userId)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetUserAsync(userId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getUser", success);
            }
        }

        public override Task<JArray> GetUsersAsync()
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.GetUsersAsync();
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("getUsers", success);
            }
        }

        public override Task SignIn(string userName, string password)
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.SignIn(userName, password);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("signIn", success);
            }
        }

        public override Task SignOut()
        {
            this.StartCall();

            bool success = true;
            try
            {
                return base.SignOut();
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                this.FinishCall("signOut", success);
            }
        }
    }
}