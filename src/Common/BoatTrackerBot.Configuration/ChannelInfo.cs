using System;

namespace BoatTracker.Bot.Configuration
{
    public class ChannelInfo
    {
        public ChannelInfo(string displayName, bool supportsButtons, bool supportsMarkdown)
        {
            this.DisplayName = displayName;
            this.SupportsButtons = supportsButtons;
            this.SupportsMarkdown = supportsMarkdown;
        }

        public string DisplayName { get; private set; }

        public bool SupportsButtons { get; private set; }

        public bool SupportsMarkdown { get; private set; }
    }
}