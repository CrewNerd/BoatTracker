using System;

namespace BoatTracker.Bot.Configuration
{
    public class ChannelInfo
    {
        public ChannelInfo(string displayName, string botAccountKeyDisplayName, bool supportsButtons, bool supportsMarkdown)
        {
            this.DisplayName = displayName;
            this.BotAccountKeyDisplayName = botAccountKeyDisplayName;
            this.SupportsButtons = supportsButtons;
            this.SupportsMarkdown = supportsMarkdown;
        }

        public string DisplayName { get; private set; }

        public string BotAccountKeyDisplayName { get; private set; }

        public bool SupportsButtons { get; private set; }

        public bool SupportsMarkdown { get; private set; }
    }
}