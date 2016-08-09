using System;
using System.Net.Http;
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

        public BookedSchedulerClient(string baseUri)
        {
            if (string.IsNullOrEmpty(baseUri))
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            this.baseUri = new Uri(baseUri);
        }

        public long UserId {  get { return this.userId; } }

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

        public async Task<JArray> GetUsers()
        {
            // TODO: support queries

            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Users/");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetUsers failed: {httpResponse.ReasonPhrase}");
                }

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["users"].Value<JArray>();
            }
        }

        public async Task<JToken> GetUser(string userId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Users/{userId}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetUser failed: {httpResponse.ReasonPhrase}");
                }

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Resources

        public async Task<JArray> GetResources()
        {
            // TODO: support queries

            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Resources/");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetResources failed: {httpResponse.ReasonPhrase}");
                }

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["resources"].Value<JArray>();
            }
        }

        public async Task<JToken> GetResource(string resourceId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Resources/{resourceId}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetResource failed: {httpResponse.ReasonPhrase}");
                }

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Groups

        public async Task<JArray> GetGroups()
        {
            // TODO: support queries

            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Groups/");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetGroups failed: {httpResponse.ReasonPhrase}");
                }

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["groups"].Value<JArray>();
            }
        }

        public async Task<JToken> GetGroup(string groupId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Groups/{groupId}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetGroup failed: {httpResponse.ReasonPhrase}");
                }

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Schedules

        public async Task<JArray> GetSchedules()
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync("Schedules/");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetSchedules failed: {httpResponse.ReasonPhrase}");
                }

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["schedules"].Value<JArray>();
            }
        }

        public async Task<JToken> GetSchedule(string scheduleId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Schedules/{scheduleId}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetSchedule failed: {httpResponse.ReasonPhrase}");
                }

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        public async Task<JToken> GetScheduleSlots(string scheduleId)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Schedules/{scheduleId}/Slots");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetScheduleSlots failed: {httpResponse.ReasonPhrase}");
                }

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        #endregion

        #region Reservations

        public async Task<JArray> GetReservations(long? userId = null, DateTime? start = null, DateTime? end = null)
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

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetReservations failed: {httpResponse.ReasonPhrase}");
                }

                var resp = JToken.Parse(await httpResponse.Content.ReadAsStringAsync());

                return resp["reservations"].Value<JArray>();
            }
        }

        public async Task<JToken> GetReservation(string referenceNumber)
        {
            using (var client = this.GetHttpClient())
            {
                var httpResponse = await client.GetAsync($"Reservations/{referenceNumber}");

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"GetReservation failed: {httpResponse.ReasonPhrase}");
                }

                return JToken.Parse(await httpResponse.Content.ReadAsStringAsync());
            }
        }

        public async Task<JArray> GetReservationsForUser(long userId)
        {
            return await this.GetReservations(userId: userId);
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

        #endregion
    }
}
