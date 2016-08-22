using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;

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
            this.entries = new ConcurrentDictionary<string, BookedSchedulerCacheEntry>();
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

        public class BookedSchedulerCacheEntry
        {
            private readonly TimeSpan CacheTimeout = TimeSpan.FromHours(8);

            private string clubId;

            private JArray resources;

            private JArray users;

            private JArray groups;

            private JArray schedules;

            private DateTime timestamp;

            public BookedSchedulerCacheEntry(string clubId)
            {
                this.clubId = clubId;
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

            #endregion

            #region Public utility methods

            public async Task<string> GetResourceNameFromIdAsync(long id)
            {
                var resources = await this.GetResourcesAsync();

                var resource = resources.FirstOrDefault(r => r.Value<long>("resourceId") == id);

                return resource != null ? resource.Value<string>("name") : "**Unknown!**";
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

                BookedSchedulerClient client = new BookedSchedulerClient(clubInfo.Url);

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