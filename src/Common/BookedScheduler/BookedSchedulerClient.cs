using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Newtonsoft.Json.Linq;

namespace BoatTracker.BookedScheduler
{
    public class BookedSchedulerClient
    {
        private const string SessionTokenHeader = "X-Booked-SessionToken";
        private const string UserIdHeader = "X-Booked-UserId";

        private Uri baseUri;

        private bool isSignedIn;

        private string userName;
        private string password;
        private long userId;
        private string sessionToken;
        private DateTime sessionExpires;

        public BookedSchedulerClient(Uri baseUri)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            this.baseUri = baseUri;
        }

        public BookedSchedulerClient(string service)
            : this(new Uri(service))
        {
        }

        public long UserId { get { return this.userId; } }

        public bool IsSignedIn { get { return this.isSignedIn; } }

        public bool IsSessionExpired
        {
            get
            {
                return DateTime.Now + TimeSpan.FromMinutes(2) > this.sessionExpires;
            }
        }

        #region Authentication

        public async Task SignIn(string userName, string password)
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

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                if (resp["isAuthenticated"].Value<bool>())
                {
                    this.userName = userName;
                    this.password = password;

                    this.sessionToken = resp["sessionToken"].Value<string>();
                    this.sessionExpires = DateTime.Parse(resp["sessionExpires"].Value<string>());
                    this.userId = resp["userId"].Value<long>();

                    this.isSignedIn = true;
                }
                else
                {
                    throw new HttpRequestException($"Authentication failed: {resp["message"].Value<string>()}");
                }
            }
        }

        public async Task SignOut()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.PostAsync(
                    "Authentication/SignOut",
                    new StringContent($"{{\"userId\":\"{this.userId}\", \"sessionToken\":\"{this.sessionToken}\" }}"));

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"SignOut failed: {httpResponse.ReasonPhrase}");
                }

                this.userName = null;
                this.password = null;
                this.userId = 0;
                this.sessionToken = null;
                this.sessionToken = null;

                this.isSignedIn = false;
            }
        }

        #endregion

        #region Users

        public async Task<JArray> GetUsersAsync()
        {
            // TODO: support queries

            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Users/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["users"].Value<JArray>();
            }
        }

        public async Task<JToken> GetUserAsync(string userId)
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

        public async Task<JArray> GetResourcesAsync()
        {
            // TODO: support queries

            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Resources/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["resources"].Value<JArray>();
            }
        }

        public async Task<JToken> GetResourceAsync(string resourceId)
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

        public async Task<JArray> GetGroupsAsync()
        {
            // TODO: support queries

            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Groups/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["groups"].Value<JArray>();
            }
        }

        public async Task<JToken> GetGroupAsync(string groupId)
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

        public async Task<JArray> GetSchedulesAsync()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Schedules/");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["schedules"].Value<JArray>();
            }
        }

        public async Task<JToken> GetScheduleAsync(string scheduleId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Schedules/{scheduleId}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        public async Task<JToken> GetScheduleSlotsAsync(string scheduleId)
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

        public async Task<JArray> GetReservationsAsync(long? userId = null, long? resourceId = null, DateTime? start = null, DateTime? end = null)
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

                    query += $"startDateTime={XmlConvert.ToString(start.Value)}";
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

                    query += $"endDateTime={XmlConvert.ToString(end.Value)}";
                }

                query = Uri.EscapeUriString(query ?? string.Empty);

                var httpResponse = await client.GetAsync($"Reservations/{query}");

                await this.CheckResponseAsync(httpResponse);

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["reservations"].Value<JArray>();
            }
        }

        public async Task<JToken> GetReservationAsync(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Reservations/{referenceNumber}");

                await this.CheckResponseAsync(httpResponse);

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        public async Task<JArray> GetReservationsForUserAsync(long userId)
        {
            return await this.GetReservationsAsync(userId: userId);
        }

        public async Task DeleteReservationAsync(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.DeleteAsync($"Reservations/{referenceNumber}");

                await this.CheckResponseAsync(httpResponse);
            }
        }

        public async Task CreateReservationAsync(JToken boat, long userId, DateTimeOffset start, TimeSpan duration, string title = null, string description = null)
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
                sb.Append($"\"resourceId\": {boat.Value<long>("resourceId")}, ");
                sb.Append($"\"startDateTime\": \"{start.ToString("s")}{start.ToString("zzz")}\", ");
                var end = start + duration;
                sb.Append($"\"endDateTime\": \"{end.ToString("s")}{end.ToString("zzz")}\"");

                sb.Append("}");

                var requestBody = sb.ToString();

                var httpResponse = await client.PostAsync($"Reservations/", new StringContent(requestBody));

                await this.CheckResponseAsync(httpResponse);
            }
        }

        #endregion

        #region Helper methods

        private HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = this.baseUri;

            if (this.isSignedIn)
            {
                client.DefaultRequestHeaders.Add(SessionTokenHeader, this.sessionToken);
                client.DefaultRequestHeaders.Add(UserIdHeader, this.userId.ToString());
            }

            return client;
        }

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

                throw new HttpRequestException(message);
            }
        }

        #endregion
    }
}
