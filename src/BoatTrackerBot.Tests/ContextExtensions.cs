namespace BoatTrackerBot.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class ContextExtensions
    {
        public static string GetDirectLineToken(this TestContext context)
        {
            return context.Properties["DirectLineToken"].ToString();
        }

        public static string GetMicrosoftAppId(this TestContext context)
        {
            return context.Properties["MicrosoftAppId"].ToString();
        }

        public static string GetFromUser(this TestContext context)
        {
            return context.Properties["FromUser"].ToString();
        }

        public static string GetBotId(this TestContext context)
        {
            return context.Properties["BotId"].ToString();
        }
    }
}