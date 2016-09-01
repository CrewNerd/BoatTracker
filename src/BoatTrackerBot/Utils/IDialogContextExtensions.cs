using System;

using Microsoft.Bot.Builder.Dialogs;

namespace BoatTracker.Bot.Utils
{
    public static class IDialogContextExtensions
    {
        public static string GetChannel(this IDialogContext context)
        {
            var msg = context.MakeMessage();
            return msg.ChannelId;
        }
    }
}