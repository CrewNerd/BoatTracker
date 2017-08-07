using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.DataObjects;

using Newtonsoft.Json.Linq;

namespace BoatTracker.Bot.Utils
{
    public class BookedSchedulerCache
    {
        private ConcurrentDictionary<string, BookedSchedulerCacheEntry> entries;

        static BookedSchedulerCache()
        {
            Instance = new BookedSchedulerCache();
        }

        private BookedSchedulerCache()
        {
            this.entries = new ConcurrentDictionary<string, BookedSchedulerCacheEntry>();
        }

        public static BookedSchedulerCache Instance { get; private set; }

        public BookedSchedulerCacheEntry this[string clubId]
        {
            get
            {
                if (!this.entries.ContainsKey(clubId))
                {
                    // This should never happen now that we load the cache on startup.
                    var newEntry = new BookedSchedulerCacheEntry(clubId);
                    this.entries.TryAdd(clubId, newEntry);
                }

                return this.entries[clubId];
            }
        }

        /// <summary>
        /// Iterate over all of the configured clubs, initializing the cache for each one. We use
        /// very generous retries and timeouts because this is done at app startup on a non-production
        /// deployment slot, where we can afford to wait. We must have the cache populated before the
        /// service is exposed to clients.
        /// </summary>
        public void Initialize()
        {
            var envName = EnvironmentDefinition.Instance.Name;

            Trace.TraceInformation($"{envName} Starting BookedSchedulerCache initialization");

            foreach (var clubId in EnvironmentDefinition.Instance.MapClubIdToClubInfo.Keys)
            {
                Trace.TraceInformation($"{envName} Starting BookedSchedulerCache initialization for '{clubId}'");

                this.entries.TryAdd(clubId, new BookedSchedulerCacheEntry(clubId));

                for (int retries = 10; retries > 0; retries--)
                {
                    try
                    {
                        Task t = this.entries[clubId].RefreshCacheAsync(failSilently: false);
                        t.Wait(TimeSpan.FromMinutes(5));

                        this.entries[clubId].IsInitialized = true;

                        Trace.TraceInformation($"{envName} Finished BookedSchedulerCache initialization for '{clubId}'");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceInformation($"{envName} BookedSchedulerCache initialization for '{clubId}' failed - {ex.Message}");
                    }
                }
            }

            Trace.TraceInformation($"{envName} Finished BookedSchedulerCache initialization");
        }

        public async Task RefreshCacheAsync(string clubId = null)
        {
            var env = EnvironmentDefinition.Instance;

            if (clubId != null && !env.MapClubIdToClubInfo.ContainsKey(clubId))
            {
                throw new ArgumentException($"Unknown club id: {clubId}");
            }

            IEnumerable<string> clubIds;

            if (clubId != null)
            {
                clubIds = new string[] { clubId };
            }
            else
            {
                clubIds = env.MapClubIdToClubInfo.Keys;
            }

            foreach (var id in clubIds)
            {
                var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[id];

                var entry = this[id];

                await entry.RefreshCacheAsync(failSilently: false);
            }
        }

        public class BookedSchedulerCacheEntry
        {
            private static readonly TimeSpan CacheTimeout = TimeSpan.FromHours(8);
            private static readonly TimeSpan CacheRetryTime = TimeSpan.FromMinutes(10);
            private static readonly TimeSpan EventLifetime = TimeSpan.FromSeconds(30);

            private long refreshInProgress = 0;

            private string clubId;

            private JArray resources;

            private Dictionary<long, JToken> userMap;

            private Dictionary<long, JToken> groupMap;

#if UNUSED
            private JArray schedules;
#endif

            private DateTime refreshTime;

            private JToken botUser;

            private ConcurrentDictionary<long, RfidEvent> mapResourceIdToLastEvent;

            public BookedSchedulerCacheEntry(string clubId)
            {
                this.clubId = clubId;
                this.mapResourceIdToLastEvent = new ConcurrentDictionary<long, RfidEvent>();
            }

            public bool IsInitialized { get; set; }

            public string ClubId { get { return this.clubId; } }

            #region Cache accessor methods

            public async Task<JArray> GetResourcesAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.resources;
            }

            public async Task<IEnumerable<JToken>> GetUsersAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.userMap.Values.ToList();
            }

            public async Task<JToken> GetUserAsync(long userId)
            {
                await this.EnsureCacheIsCurrentAsync();

                JToken user;
                if (this.userMap.TryGetValue(userId, out user))
                {
                    return user;
                }

                return null;
            }

            public async Task<IEnumerable<JToken>> GetGroupsAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.groupMap.Values.ToList();
            }

            public async Task<JToken> GetGroupAsync(long groupId)
            {
                await this.EnsureCacheIsCurrentAsync();

                JToken group;
                if (this.groupMap.TryGetValue(groupId, out group))
                {
                    return group;
                }

                return null;
            }

#if UNUSED
            public async Task<JArray> GetSchedulesAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.schedules;
            }
#endif

            public async Task<JToken> GetBotUserAsync()
            {
                if (this.botUser == null)
                {
                    await this.EnsureCacheIsCurrentAsync();

                    var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.clubId];

                    this.botUser = this.userMap
                        .Values
                        .Where(u => u.UserName() == clubInfo.UserName)
                        .FirstOrDefault();
                }

                return this.botUser;
            }

            public async Task<UserState> GetBotUserStateAsync()
            {
                var botUser = await this.GetBotUserAsync();

                return new UserState
                {
                    ClubId = this.clubId,
                    UserId = botUser.Id()
                };
            }

            #endregion

            #region Event handling methods

            /// <summary>
            /// It will be common to get two events for the same boat close together since we
            /// normally have two tags per boat. We want to make sure we only process the first
            /// event in this case and ignore the redundant ones.
            /// </summary>
            /// <param name="ev">An incoming event</param>
            /// <returns>True if the event is redundant</returns>
            public async Task<bool> IsEventRedundantAsync(RfidEvent ev)
            {
                if (!ev.ReadTime.HasValue)
                {
                    ev.ReadTime = DateTime.Now;
                }

                var boat = await this.GetResourceFromRfidTagAsync(ev.EPC);

                if (boat == null)
                {
                    // If the tag isn't associated with a boat, it can't be redundant
                    // but we also don't want to cache it.
                    return false;
                }

                var boatId = boat.ResourceId();

                RfidEvent lastEvent;

                bool isRedundant = true;

                // If no event for this boat, this isn't redundant
                if (!this.mapResourceIdToLastEvent.TryGetValue(boatId, out lastEvent))
                {
                    isRedundant = false;
                }
                else if (lastEvent.ReadTime.Value + EventLifetime < DateTime.Now)
                {
                    // If we haven't seen an event for this boat in a while, this isn't redundant
                    isRedundant = false;
                }
                else if (lastEvent.Direction != ev.Direction || lastEvent.ReadZone != ev.ReadZone)
                {
                    // If the door or direction are different, this is a new event
                    isRedundant = false;
                }

                if (!isRedundant)
                {
                    this.mapResourceIdToLastEvent.TryAdd(boatId, ev);
                }

                return isRedundant;
            }

            #endregion

            #region Public utility methods

            /// <summary>
            /// Return a boat resource given its id.
            /// </summary>
            /// <param name="id">The id of the boat</param>
            /// <returns>The JToken representing the boat resource or null if no boat was found.</returns>
            public async Task<JToken> GetResourceFromIdAsync(long id)
            {
                var resources = await this.GetResourcesAsync();

                var resource = resources.FirstOrDefault(r => r.ResourceId() == id);

                return resource;
            }

            /// <summary>
            /// Gets the name of a boat given it's id.
            /// </summary>
            /// <param name="id">The id of the boat</param>
            /// <returns>The boat's name or a fixed string if the boat couldn't be found.</returns>
            public async Task<string> GetResourceNameFromIdAsync(long id)
            {
                var resource = await this.GetResourceFromIdAsync(id);

                return resource != null ? resource.Name() : "**Unknown!**";
            }

            /// <summary>
            /// Return a boat resource given the id of one of its RFID tags.
            /// </summary>
            /// <param name="rfidTag">The RFID tag value.</param>
            /// <returns>The JToken representing the boat, or null if not boat was found.</returns>
            public async Task<JToken> GetResourceFromRfidTagAsync(string rfidTag)
            {
                var resources = await this.GetResourcesAsync();

                var resource = resources
                    .Where(t => t.BoatTagIds().Contains(rfidTag, StringComparer.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                return resource;
            }

            #endregion

            #region Cache management

            private async Task EnsureCacheIsCurrentAsync()
            {
                if (DateTime.Now > this.refreshTime)
                {
                    await this.RefreshCacheAsync();
                }
            }

            public async Task RefreshCacheAsync(bool failSilently = true)
            {
                BookedSchedulerRetryClient client = null;

                try
                {
                    //
                    // If there's a refresh in progress on another thread, we return and let
                    // the caller proceed with date that's soon to be replaced. The priority
                    // is to ensure that two threads aren't updating the cache at once.
                    //
                    if (Interlocked.CompareExchange(ref this.refreshInProgress, 1, 0) != 0)
                    {
                        return;
                    }

                    var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.clubId];

                    client = new BookedSchedulerRetryClient(this.clubId, false);

                    await client.SignInAsync(clubInfo.UserName, clubInfo.Password);

                    var newResources = await client.GetResourcesAsync();
                    var users = await client.GetUsersAsync();
                    var newUserMap = new Dictionary<long, JToken>();

                    foreach (var u in users)
                    {
                        var fullUser = await client.GetUserAsync(u.Id());
                        newUserMap.Add(fullUser.Id(), fullUser);
                    }

                    var groups = await client.GetGroupsAsync();
                    var newGroupMap = new Dictionary<long, JToken>();

                    foreach (var g in groups)
                    {
                        var fullGroup = await client.GetGroupAsync(g.Id());
                        newGroupMap.Add(fullGroup.Id(), fullGroup);
                    }

#if UNUSED
                    var newSchedules = await client.GetSchedulesAsync();
                    this.schedules = newSchedules;
#endif

                    this.resources = newResources;
                    this.userMap = newUserMap;
                    this.groupMap = newGroupMap;

                    // Schedule the next cache refresh
                    this.refreshTime = DateTime.Now + CacheTimeout;
                }
                catch (Exception)
                {
                    //
                    // During normal operation, we fail silently since we have existing (but stale) data
                    // that we can continue to use. During initialization, we must rethrow so we can retry.
                    //
                    if (failSilently)
                    {
                        // If there were any errors, leave the stale data in place and schedule another
                        // refresh fairly soon.
                        this.refreshTime = DateTime.Now + CacheRetryTime;
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    this.refreshInProgress = 0;

                    if (client != null && client.IsSignedIn)
                    {
                        try
                        {
                            await client.SignOutAsync();
                        }
                        catch (Exception)
                        {
                            // best effort only
                        }
                    }
                }
            }

            #endregion
        }
    }
}