using System;

namespace BoatTracker.Bot.DataObjects
{
    [Serializable]
    public class UserState
    {
        public const string PropertyName = "userState";

        public string ClubId { get; set; }

        public long UserId { get; set; }

        public bool HelpMessageShown { get; set; }

        public bool IsComplete
        {
            get
            {
                return (this.UserId != 0 && !string.IsNullOrEmpty(this.ClubId));
            }
        }
    }
}