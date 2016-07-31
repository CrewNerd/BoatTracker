using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

namespace BoatTracker.Bot
{
    [LuisModel("b85dc175-ebf1-450c-96a8-0f7ffe0fd1b7", "bccaeb74ae1648c7a7eb38debaa7de21")]
    [Serializable]
    public class BoatTrackerDialog : LuisDialog<object>
    {
        public const string EntityBoatName = "boatName";
        public const string EntityStart = "DateTime::start";
        public const string EntityDuration = "DateTime::duration";
        public const string EntityBuiltinDateTime = "builtin.datetime.date";
        public const string EntityBuiltinDuration = "builtin.datetime.duration";

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = "Sorry I did not understand: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("CreateReservation")]
        public async Task CreateReservation(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It sounds like you want to create a boat reservation");
            context.Wait(MessageReceived);
        }

        [LuisIntent("CheckBoatAvailability")]
        public async Task CheckBoatAvailability(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It sounds like you want to check on the availability of a boat");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Checkout")]
        public async Task Checkout(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It sounds like you want to check out a boat");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Checkin")]
        public async Task Checkin(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It sounds like you want to check in a boat");
            context.Wait(MessageReceived);
        }

        [LuisIntent("CheckReservations")]
        public async Task CheckReservations(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It sounds like you want to check on your reservations");
            context.Wait(MessageReceived);
        }

        [LuisIntent("CancelReservation")]
        public async Task CancelReservation(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("It sounds like you want to cancel a reservation");
            context.Wait(MessageReceived);
        }
    }
}