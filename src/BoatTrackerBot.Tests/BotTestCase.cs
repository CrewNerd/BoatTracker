﻿namespace BoatTrackerBot.Tests
{
    using System;

    internal class BotTestCase
    {
        private string _expectedReply;
        public BotTestCase()
        {
            this.ErrorMessageHandler = DefaultErrorMessageHandler;
        }

        public string Action { get; set; }

        public string ExpectedReply {
            get
            {
                return _expectedReply;
            }
            internal set
            {
                _expectedReply = value.ToLowerInvariant();
            }
        }

        public Func<string, string, string, string> ErrorMessageHandler { get; internal set; }

        public Action<string> Verified { get; internal set; }

        private static string DefaultErrorMessageHandler(string action, string expectedReply, string receivedReply)
        {
            return $"'{action}' received reply '{receivedReply}' that doesn't contain the expected message: '{expectedReply}'";
        }
    }
}