using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Contains information about a club that we're configured to access
    /// </summary>
    public class ClubInfo
    {
        /// <summary>
        /// Gets or sets the display name for the club.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL where we access the club's API.
        /// </summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets the user name for accessing the club.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password we use to access our account.
        /// </summary>
        public string Password { get; set; }
    }
}