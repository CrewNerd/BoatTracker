using Microsoft.Bot.Builder.Dialogs;

namespace BoatTracker.Bot.Utils
{
    public static class IDialogContextExtensions
    {
        /// <summary>
        /// Gets the channel name for the caller. It seems strange that we can't get
        /// this in a more direct way.
        /// </summary>
        /// <param name="context">The caller's context</param>
        /// <returns>The name of the channel in use.</returns>
        public static string GetChannel(this IDialogContext context)
        {
            return context.MakeMessage().ChannelId;
        }
    }
}