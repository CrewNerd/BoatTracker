using System;

namespace BoatTracker.Bot.DataObjects
{
    [Serializable]
    public class UserState
    {
        public const string PropertyName = "userState";

        public string ClubId { get; set; }

        public long UserId { get; set; }

        public string BotAccountKey { get; set; }

        public bool HelpMessageShown { get; set; }
    }
}