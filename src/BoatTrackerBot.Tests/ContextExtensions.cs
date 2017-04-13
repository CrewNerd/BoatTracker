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

        public static string GetBookedSchedulerUrl(this TestContext context)
        {
            return context.Properties["BookedSchedulerUrl"].ToString();
        }

        public static string GetClubId(this TestContext context)
        {
            return context.Properties["ClubId"].ToString();
        }

        public static string GetBotUsername(this TestContext context)
        {
            return context.Properties["BotUsername"].ToString();
        }

        public static string GetBotPassword(this TestContext context)
        {
            return context.Properties["BotPassword"].ToString();
        }
    }
}