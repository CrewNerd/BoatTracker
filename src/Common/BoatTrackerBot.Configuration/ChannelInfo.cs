using System;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents configuration information for a bot communication channel.
    /// </summary>
    public class ChannelInfo
    {
        /// <summary>
        /// Initializes a new instance of the ChannelInfo class.
        /// </summary>
        /// <param name="displayName">The channel display name</param>
        /// <param name="supportsButtons">Whether the channel supports buttons</param>
        /// <param name="supportsMarkdown">Whether the channel supports markdown</param>
        public ChannelInfo(string displayName, bool supportsButtons, bool supportsMarkdown)
        {
            this.DisplayName = displayName;
            this.SupportsButtons = supportsButtons;
            this.SupportsMarkdown = supportsMarkdown;
        }

        /// <summary>
        /// Gets the human-friendly name for the channel.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the channel supports buttons.
        /// </summary>
        public bool SupportsButtons { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the channel can render markdown.
        /// </summary>
        public bool SupportsMarkdown { get; private set; }
    }
}