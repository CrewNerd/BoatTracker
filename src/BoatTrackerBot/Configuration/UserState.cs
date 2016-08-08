using System;
using System.Collections.Generic;

namespace BoatTracker.Bot.Configuration
{
    [Serializable]
    public class UserState
    {
        public const string PropertyName = "userState";

        /// <summary>
        /// The attribute display name on BookedScheduler must match this string exactly.
        /// </summary>
        public const string BotAccountKeyDisplayName = "Bot account key";

        public string ClubId { get; set; }

        public long UserId { get; set; }

        public string BotAccountKey { get; set; }

        public string Timezone { get; set; }

        public DateTime Timestamp { get; set; }
    }
}