namespace BoatTrackerBot.Tests
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal class BotTestCase
    {
        public string CallerFile { get; set; }
        public int CallerLineNumber { get; set; }

        private string _expectedReply;

        public BotTestCase(
            [CallerFilePath] string file = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            this.CallerFile = Path.GetFileName(file);
            this.CallerLineNumber = lineNumber;

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

        public Func<string, int, string, string, string, string> ErrorMessageHandler { get; internal set; }

        public Action<string> Verified { get; internal set; }

        private static string DefaultErrorMessageHandler(string file, int line, string action, string expectedReply, string receivedReply)
        {
            return $"{file}:{line} '{action}' received reply '{receivedReply}' that doesn't contain the expected message: '{expectedReply}'";
        }
    }
}