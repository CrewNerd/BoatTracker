using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace BoatTracker.BookedScheduler
{
    /// <summary>
    /// Client API wrapper for BookedScheduler. Logging and retries are implemented in separate
    /// class that wraps this one.
    /// </summary>
    [Serializable]
    public class BookedSchedulerClient
    {
        private const string SessionTokenHeader = "X-Booked-SessionToken";
        private const string UserIdHeader = "X-Booked-UserId";
        private const long InvalidUserId = -1;

        private Uri baseUri;
        private TimeSpan timeout;

        private long sessionUserId;
        private string sessionToken;
        private DateTime sessionExpires;

        /// <summary>
        /// Initializes a new instance of the BookedSchedulerClient class.
        /// </summary>
        /// <param name="baseUri">The base URI for the BS instance that we target</param>
        /// <param name="timeout">The timeout for API calls (default is 30 seconds)</param>
        public BookedSchedulerClient(Uri baseUri, TimeSpan? timeout = null)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            this.baseUri = baseUri;
            this.timeout = timeout ?? TimeSpan.FromSeconds(30);
            this.sessionUserId = InvalidUserId;
        }

        /// <summary>
        /// Gets a value indicating whether we have successfully signed in.
        /// </summary>
        public bool IsSignedIn
        {
            get
            {
                return this.sessionUserId != InvalidUserId;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the session token has expired.
        /// </summary>
        public bool IsSessionExpired
        {
            get
            {
                return string.IsNullOrEmpty(this.sessionToken) || DateTime.Now + TimeSpan.FromMinutes(2) > this.sessionExpires;
            }
        }

        #region Authentication

        /// <summary>
        /// Sign into BookedScheduler.
        /// </summary>
        /// <param name="userName">The user name to sign in with</param>
        /// <param name="password">The login password</param>
        /// <returns>A task that completes when login is successful.</returns>
        public virtual async Task SignInAsync(string userName, string password)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.PostAsync(
                    "Authentication/Authenticate",
                    new StringContent($"{{\"username\":\"{userName}\", \"password\":\"{password}\" }}"));

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"SignIn failed: {httpResponse.ReasonPhrase}");
                }

                var respBody = await httpResponse.Content.ReadAsStringAsync();

                // Detect the common case where the API just hasn't been enabled on the site.
                if (respBody.Contains("API") && respBody.Contains("disabled"))
                {
                    throw new HttpRequestException($"Authentication failed: the API is currently disabled.");
                }

                var resp = JToken.Parse(respBody);

                if (resp["isAuthenticated"].Value<bool>())
                {
                    this.sessionToken = resp["sessionToken"].Value<string>();
                    this.sessionExpires = DateTime.Parse(resp["sessionExpires"].Value<string>());
                    this.sessionUserId = resp["userId"].Value<long>();
                }
                else
                {
                    throw new HttpRequestException($"Authentication failed: {resp["message"].Value<string>()}");
                }
            }
        }

        /// <summary>
        /// Sign out from BookedScheduler.
        /// </summary>
        /// <returns>A task that completes when the signout finishes.</returns>
        public virtual async Task SignOutAsync()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.PostAsync(
                    "Authentication/SignOut",
                    new StringContent($"{{\"userId\":\"{this.sessionUserId}\", \"sessionToken\":\"{this.sessionToken}\" }}"));

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"SignOut failed: {httpResponse.ReasonPhrase}");
                }

                this.sessionUserId = InvalidUserId;
                this.sessionToken = null;
            }
        }

        #endregion

        #region Users

        /// <summary>
        /// Get all BookedScheduler users.
        /// </summary>
        /// <returns>A task returning a JArray of users (JTokens)</returns>
        public virtual async Task<JArray> GetUsersAsync()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Users/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["users"].Value<JArray>();
            }
        }

        /// <summary>
        /// Get the BookedScheduler object for a specific user. This returns more data
        /// for the user than GetUsersAsync.
        /// </summary>
        /// <param name="userId">The id of the user to retrieve</param>
        /// <returns>A task containing the user data (JToken)</returns>
        public virtual async Task<JToken> GetUserAsync(long userId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Users/{userId}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Resources

        /// <summary>
        /// Gets all resources for the BookedScheduler instance.
        /// </summary>
        /// <returns>A JArray containing all resources (JTokens)</returns>
        public virtual async Task<JArray> GetResourcesAsync()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Resources/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["resources"].Value<JArray>();
            }
        }

        /// <summary>
        /// Gets a particular resource from BookedScheduler.
        /// </summary>
        /// <param name="resourceId">The id of the resource to retrieve.</param>
        /// <returns>A task containing the resource data (JToken)</returns>
        public virtual async Task<JToken> GetResourceAsync(long resourceId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Resources/{resourceId}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Groups

        /// <summary>
        /// Gets all groups from the BookedScheduler instance.
        /// </summary>
        /// <returns>A JArray containing the groups (JTokens)</returns>
        public virtual async Task<JArray> GetGroupsAsync()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Groups/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["groups"].Value<JArray>();
            }
        }

        /// <summary>
        /// Gets a particular group from the BookedScheduler instance.
        /// </summary>
        /// <param name="groupId">The id of the group to retrieve.</param>
        /// <returns>A task containing the group data (JToken)</returns>
        public virtual async Task<JToken> GetGroupAsync(long groupId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Groups/{groupId}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Schedules

        /// <summary>
        /// Get the all schedules for the BookedScheduler instance.
        /// </summary>
        /// <returns>A JArray of schedules (JToken)</returns>
        public virtual async Task<JArray> GetSchedulesAsync()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Schedules/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["schedules"].Value<JArray>();
            }
        }

        /// <summary>
        /// Gets a particular schedule from the BookedScheduler instance.
        /// </summary>
        /// <param name="scheduleId">The id of the schedule to retrieve.</param>
        /// <returns>A task containing the schedule data (JToken)</returns>
        public virtual async Task<JToken> GetScheduleAsync(string scheduleId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Schedules/{scheduleId}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Gets the schedule slots for a given schedule.
        /// </summary>
        /// <param name="scheduleId">The id of the schedule whose slots are being retrieved.</param>
        /// <returns>A task containing the schedule slots (JToken)</returns>
        public virtual async Task<JToken> GetScheduleSlotsAsync(string scheduleId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Schedules/{scheduleId}/Slots");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Reservations

        /// <summary>
        /// Gets reservations from the BookedScheduler instance based on a set of query parameters.
        /// </summary>
        /// <param name="userId">If not null, get reservations owned by the given user id.</param>
        /// <param name="resourceId">If not null, get reservations for the given resource id.</param>
        /// <param name="start">If not null, get reservations starting after the given DateTime.</param>
        /// <param name="end">If not null, get reservations ending before the given DateTime.</param>
        /// <returns>A task containing a JArray of reservations (JToken)</returns>
        public virtual async Task<JArray> GetReservationsAsync(
            long? userId = null,
            long? resourceId = null,
            DateTime? start = null,
            DateTime? end = null)
        {
            using (var client = this.GetHttpClient())
            {
                string query = null;

                if (userId != null && userId.HasValue)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = "?";
                    }
                    else
                    {
                        query += "&";
                    }

                    query += $"userId={userId.Value}";
                }

                if (resourceId != null && resourceId.HasValue)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = "?";
                    }
                    else
                    {
                        query += "&";
                    }

                    query += $"resourceId={resourceId.Value}";
                }

                if (start != null)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = "?";
                    }
                    else
                    {
                        query += "&";
                    }

                    query += $"startDateTime={XmlConvert.ToString(start.Value, XmlDateTimeSerializationMode.Utc)}";
                }

                if (end != null)
                {
                    if (string.IsNullOrEmpty(query))
                    {
                        query = "?";
                    }
                    else
                    {
                        query += "&";
                    }

                    query += $"endDateTime={XmlConvert.ToString(end.Value, XmlDateTimeSerializationMode.Utc)}";
                }

                query = Uri.EscapeUriString(query ?? string.Empty);

                var httpResponse = await client.GetAsync($"Reservations/{query}");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["reservations"].Value<JArray>();
            }
        }

        /// <summary>
        /// Gets a single reservation from the BookedScheduler instance.
        /// </summary>
        /// <param name="referenceNumber">The reference number of the reservation to retrieve.</param>
        /// <returns>A task containing the reservation (JToken)</returns>
        public virtual async Task<JToken> GetReservationAsync(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Reservations/{referenceNumber}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Check in for a reservation.
        /// </summary>
        /// <param name="referenceNumber">The reference number of the reservation to be checked in.</param>
        /// <returns>A task containing the updated reservation data.</returns>
        public virtual async Task<JToken> CheckInReservationAsync(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.PostAsync($"Reservations/{referenceNumber}/CheckIn", new StringContent(string.Empty));

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Check out of a reservation.
        /// </summary>
        /// <param name="referenceNumber">The reference number of the reservation to be checked out.</param>
        /// <returns>A task containing the updated reservation data.</returns>
        public virtual async Task<JToken> CheckOutReservationAsync(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.PostAsync($"Reservations/{referenceNumber}/CheckOut", new StringContent(string.Empty));

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        /// <summary>
        /// Gets all reservations for a particular user.
        /// </summary>
        /// <param name="userId">The user whose reservations should be fetched.</param>
        /// <returns>A JArray containing the user's reservations.</returns>
        public virtual async Task<JArray> GetReservationsForUserAsync(long userId)
        {
            return await this.GetReservationsAsync(userId: userId);
        }

        /// <summary>
        /// Deletes a particular reservation.
        /// </summary>
        /// <param name="referenceNumber">The reference number of the reservation to be deleted.</param>
        /// <returns>A task that completes when the reservation is deleted.</returns>
        public virtual async Task DeleteReservationAsync(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.DeleteAsync($"Reservations/{referenceNumber}");

                await this.CheckResponseAsync(httpResponse);
            }
        }

        /// <summary>
        /// Creates a new reservation.
        /// </summary>
        /// <param name="boat">The id of the boat to be reserved.</param>
        /// <param name="userId">The user creating the reservation.</param>
        /// <param name="start">The reservation start time.</param>
        /// <param name="duration">The reservation duration.</param>
        /// <param name="title">An optional title for the reservation.</param>
        /// <param name="description">An optional description for the reservation.</param>
        /// <param name="secondUserId">An optional second user for pairs and doubles.</param>
        /// <returns>A task containing the data for the created reservation.</returns>
        public virtual async Task<JToken> CreateReservationAsync(
            JToken boat,
            long userId,
            DateTimeOffset start,
            TimeSpan duration,
            string title = null,
            string description = null,
            long? secondUserId = null)
        {
            using (var client = this.GetHttpClient())
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("{");

                if (!string.IsNullOrEmpty(title))
                {
                    sb.Append($"\"title\": \"{title}\", ");
                }

                if (!string.IsNullOrEmpty(title))
                {
                    sb.Append($"\"description\": \"{description}\", ");
                }

                sb.Append($"\"userId\": {userId}, ");

                if (secondUserId.HasValue)
                {
                    sb.Append($"\"participants\": [{secondUserId.Value}], ");
                }

                sb.Append($"\"resourceId\": {boat.ResourceId()}, ");
                sb.Append($"\"startDateTime\": \"{start.ToString("s")}{start.ToString("zzz")}\", ");
                var end = start + duration;
                sb.Append($"\"endDateTime\": \"{end.ToString("s")}{end.ToString("zzz")}\"");

                sb.Append("}");

                var requestBody = sb.ToString();

                var httpResponse = await client.PostAsync($"Reservations/", new StringContent(requestBody));

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Gets an HTTP client object with the proper timeout, base URL, and headers.
        /// </summary>
        /// <returns>The initialized HttpClient.</returns>
        private HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = this.baseUri;
            client.Timeout = this.timeout;

            if (this.IsSignedIn)
            {
                client.DefaultRequestHeaders.Add(SessionTokenHeader, this.sessionToken);
                client.DefaultRequestHeaders.Add(UserIdHeader, this.sessionUserId.ToString());
            }

            return client;
        }

        /// <summary>
        /// Checks the response of an HTTP call. Throws an exception if an error is detected.
        /// </summary>
        /// <param name="response">The response to be checked.</param>
        /// <returns>A task that completes when the response has been checked.</returns>
        private async Task CheckResponseAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                string message = response.ReasonPhrase;

                // If we got a response body, look for formatted error messages from the server
                // to pass along to the user.
                if (!string.IsNullOrEmpty(responseBody))
                {
                    string errors = string.Empty;

                    try
                    {
                        var resp = JToken.Parse(await response.Content.ReadAsStringAsync());

                        foreach (var error in resp["errors"])
                        {
                            errors += error.Value<string>() + " ";
                        }

                        if (!string.IsNullOrEmpty(errors))
                        {
                            message = errors;
                        }
                    }
                    catch (Exception)
                    {
                        // If the response doesn't parse as valid JSON, then fall through
                        // and return our fallback message.
                    }
                }

                throw new HttpRequestException(message);
            }
        }

        #endregion
    }
}
