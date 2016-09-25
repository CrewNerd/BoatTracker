using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.DataObjects;

namespace BoatTracker.Bot.Utils
{
    public class BookedSchedulerCache
    {
        static BookedSchedulerCache()
        {
            Instance = new BookedSchedulerCache();
        }

        public static BookedSchedulerCache Instance { get; private set; }

        private BookedSchedulerCache()
        {
            this.ResetCache();
        }

        private ConcurrentDictionary<string, BookedSchedulerCacheEntry> entries;

        public BookedSchedulerCacheEntry this[string clubId]
        {
            get
            {
                if (!entries.ContainsKey(clubId))
                {
                    var newEntry = new BookedSchedulerCacheEntry(clubId);
                    this.entries.TryAdd(clubId, newEntry);
                }

                return this.entries[clubId];
            }
        }

        public void ResetCache()
        {
            this.entries = new ConcurrentDictionary<string, BookedSchedulerCacheEntry>();
        }

        public class BookedSchedulerCacheEntry
        {
            private readonly TimeSpan CacheTimeout = TimeSpan.FromHours(8);

            private string clubId;

            private JArray resources;

            private JArray users;

            private JArray groups;

            private JArray schedules;

            private DateTime timestamp;

            private JToken botUser;

            private ConcurrentDictionary<long, RfidEvent> mapResourceIdToLastEvent;

            public BookedSchedulerCacheEntry(string clubId)
            {
                this.clubId = clubId;
                this.mapResourceIdToLastEvent = new ConcurrentDictionary<long, RfidEvent>();
            }

            #region Cache accessor methods

            public async Task<JArray> GetResourcesAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.resources;
            }

            public async Task<JArray> GetUsersAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.users;
            }

            public async Task<JArray> GetGroupsAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.groups;
            }

            public async Task<JArray> GetSchedulesAsync()
            {
                await this.EnsureCacheIsCurrentAsync();
                return this.schedules;
            }

            public async Task<JToken> GetBotUserAsync()
            {
                if (this.botUser == null)
                {
                    await this.EnsureCacheIsCurrentAsync();

                    var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.clubId];

                    this.botUser = this.users
                        .Where(u => u.UserName() == clubInfo.UserName)
                        .FirstOrDefault();
                }

                return this.botUser;
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
                TimeSpan EventLifetime = TimeSpan.FromSeconds(30);

                if (!ev.Timestamp.HasValue)
                {
                    ev.Timestamp = DateTime.Now;
                }

                var boat = await this.GetResourceFromRfidTagAsync(ev.Id);

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
                else if (lastEvent.Timestamp.Value + EventLifetime < DateTime.Now)
                {
                    // If we haven't seen an event for this boat in a while, this isn't redundant
                    isRedundant = false;
                }
                else if (lastEvent.EventType != ev.EventType || lastEvent.Antenna != ev.Antenna)
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

            public async Task<JToken> GetResourceFromIdAsync(long id)
            {
                var resources = await this.GetResourcesAsync();

                var resource = resources.FirstOrDefault(r => r.ResourceId() == id);

                return resource;
            }

            public async Task<string> GetResourceNameFromIdAsync(long id)
            {
                var resource = await this.GetResourceFromIdAsync(id);

                return resource != null ? resource.Name() : "**Unknown!**";
            }

            public async Task<JToken> GetResourceFromRfidTagAsync(string rfidTag)
            {
                var resources = await this.GetResourcesAsync();

                var resource = resources
                    .Where(t => t.GetBoatTagIds().Contains(rfidTag, StringComparer.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                return resource;
            }

            #endregion

            #region Cache management

            private async Task EnsureCacheIsCurrentAsync()
            {
                if (this.timestamp + CacheTimeout < DateTime.Now)
                {
                    await this.RefreshCacheAsync();
                }
            }

            private long RefreshInProgress = 0;

            private async Task RefreshCacheAsync()
            {
                //
                // If there's a refresh in progress on another thread, we return and let
                // the caller proceed with date that's soon to be replaced. The priority
                // is to ensure that two threads aren't updating the cache at once.
                //
                if (Interlocked.CompareExchange(ref this.RefreshInProgress, 1, 0) != 0)
                {
                    return;
                }

                var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.clubId];

                BookedSchedulerClient client = new BookedSchedulerLoggingClient(this.clubId);

                await client.SignIn(clubInfo.UserName, clubInfo.Password);

                this.resources = await client.GetResourcesAsync();
                this.users = await client.GetUsersAsync();
                this.groups = await client.GetGroupsAsync();
                this.schedules = await client.GetSchedulesAsync();

                this.timestamp = DateTime.Now;
                this.RefreshInProgress = 0;
            }

            #endregion
        }
    }
}