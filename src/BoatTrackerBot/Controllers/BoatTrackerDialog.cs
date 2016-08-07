using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

using BoatTracker.Bot.Configuration;

namespace BoatTracker.Bot
{
    [Serializable]
    public class BoatTrackerDialog : LuisDialog<object>
    {
        public const string EntityBoatName = "boatName";
        public const string EntityStart = "DateTime::start";
        public const string EntityDuration = "DateTime::duration";
        public const string EntityBuiltinDate = "builtin.datetime.date";
        public const string EntityBuiltinTime = "builtin.datetime.time";
        public const string EntityBuiltinDuration = "builtin.datetime.duration";

        private UserState currentUserState;

        public BoatTrackerDialog(ILuisService service)
            : base(service)
        {
        }

        #region Intents

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            string message = "Sorry I did not understand: " + string.Join(", ", result.Intents.Select(i => i.Intent));
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("CreateReservation")]
        public async Task CreateReservation(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            var boatName = this.FindBoatName(result);
            var startDate = this.FindStartDate(result);
            var startTime = this.FindStartTime(result);
            var duration = this.FindDuration(result);

            string replyMessage = string.Empty;

            if (boatName == null)
            {
                replyMessage = "I think you want to reserve a boat but I can't find a boat name in your request.";
            }
            else if (startDate == DateTime.MinValue)
            {
                replyMessage = "I think you want to reserve a boat but I don't know what day you want to reserve it for.";
            }
            else if (startTime == DateTime.MinValue)
            {
                replyMessage = "I think you want to reserve a boat but I don't know what time you want to reserve it for.";
            }
            else if (duration == TimeSpan.Zero)
            {
                replyMessage = "I think you want to reserve a boat but I don't know how long you want to use it.";
            }
            else
            {
                string confirmMsg = string.Format(
                    "Do you want to reserve boat '{0}' on {1} at {2} for {3} minutes?",
                    boatName,
                    startDate.ToLongDateString(),
                    startTime.ToShortTimeString(),
                    duration.TotalMinutes);

                PromptDialog.Confirm(context, AfterConfirming_CreateReservation, confirmMsg, promptStyle: PromptStyle.None);
                return;
            }

            await context.PostAsync(replyMessage);
            context.Wait(MessageReceived);
        }

        public async Task AfterConfirming_CreateReservation(IDialogContext context, IAwaitable<bool> confirmation)
        {
            if (await confirmation)
            {
                await context.PostAsync("Okay, your reservation is confirmed!");
            }
            else
            {
                await context.PostAsync("Okay, I'm aborting that reservation.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("CheckBoatAvailability")]
        public async Task CheckBoatAvailability(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            string boatName = this.FindBoatName(result);

            if (string.IsNullOrEmpty(boatName))
            {
                await context.PostAsync("It sounds like you want to check on the availability of a boat but I don't know how to do that yet.");
            }
            else
            {
                await context.PostAsync($"It sounds like you want to check on the availability of the '{boatName}' but I don't know how to do that yet.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("Checkout")]
        public async Task Checkout(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            string boatName = this.FindBoatName(result);

            if (string.IsNullOrEmpty(boatName))
            {
                await context.PostAsync("It sounds like you want to check out a boat but I don't know how to do that yet.");
            }
            else
            {
                await context.PostAsync($"It sounds like you want to check out the '{boatName}' but I don't know how to do that yet.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("Checkin")]
        public async Task Checkin(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            string boatName = this.FindBoatName(result);

            if (string.IsNullOrEmpty(boatName))
            {
                await context.PostAsync("It sounds like you want to check in a boat but I don't know how to do that yet.");
            }
            else
            {
                await context.PostAsync($"It sounds like you want to check in the '{boatName}' but I don't know how to do that yet.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("CheckReservations")]
        public async Task CheckReservations(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            await context.PostAsync("It sounds like you want to check on your reservations but I don't know how to do that yet.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("CancelReservation")]
        public async Task CancelReservation(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            await context.PostAsync("It sounds like you want to cancel a reservation but I don't know how to do that yet.");
            context.Wait(MessageReceived);
        }

        #endregion

        #region Entity Helpers

        private string FindBoatName(LuisResult result)
        {
            EntityRecommendation boatName;

            if (result.TryFindEntity(EntityBoatName, out boatName))
            {
                return boatName.Entity;
            }

            return null;
        }

        private DateTime FindStartDate(LuisResult result)
        {
            EntityRecommendation builtinDate = null;
            result.TryFindEntity(EntityBuiltinDate, out builtinDate);

            if (builtinDate != null && builtinDate.Resolution.ContainsKey("date"))
            {
                var parser = new Chronic.Parser();
                var span = parser.Parse(builtinDate.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;
                    return when.Value;
                }
            }

            return DateTime.MinValue;
        }

        private DateTime FindStartTime(LuisResult result)
        {
            EntityRecommendation builtinTime = null;
            result.TryFindEntity(EntityBuiltinTime, out builtinTime);

            if (builtinTime != null && builtinTime.Resolution.ContainsKey("time"))
            {
                var parser = new Chronic.Parser();
                var span = parser.Parse(builtinTime.Entity);

                if (span != null)
                {
                    var when = span.Start ?? span.End;
                    return when.Value;
                }
            }

            return DateTime.MinValue;
        }

        private TimeSpan FindDuration(LuisResult result)
        {
            EntityRecommendation builtinDuration = null;
            result.TryFindEntity(EntityBuiltinDuration, out builtinDuration);

            if (builtinDuration != null && builtinDuration.Resolution.ContainsKey("duration"))
            {
                return System.Xml.XmlConvert.ToTimeSpan(builtinDuration.Resolution["duration"]);
            }

            return TimeSpan.Zero;
        }

        #endregion

        #region Misc Helpers

        private async Task<bool> CheckUserIsRegistered(IDialogContext context)
        {
            UserState userState = null;

            //
            // If we haven't seen the user before, establish their BotAccountKey and store it.
            //
            if (!context.UserData.TryGetValue(UserState.PropertyName, out userState))
            {
                userState = new UserState { BotAccountKey = Guid.NewGuid().ToString().Trim('{', '}') };
                context.UserData.SetValue(UserState.PropertyName, userState);
            }

            if (userState.UserId != 0 && !string.IsNullOrEmpty(userState.ClubId))
            {
                // The user is fully registered
                this.currentUserState = userState;
                return true;
            }

            // We haven't obtained the user's info before. See if we can find it now.
            EnvironmentDefinition env = EnvironmentDefinition.CreateFromEnvironment();

            userState = await env.TryBuildStateForUser(userState.BotAccountKey);

            if (userState != null)
            {
                // The user just registered. Save their club info now so we don't have to
                // do this lookup every time.
                context.UserData.SetValue(UserState.PropertyName, userState);
                return true;
            }
            else
            {
                await context.PostAsync($"It looks like you haven't registered your Bot account with BookedScheduler yet. To connect your Skype account to BookedScheduler, please go to your BookedScheduler profile and set your '{UserState.BotAccountKeyDisplayName}' to {userState.BotAccountKey}.");
                context.Wait(MessageReceived);
                return false;
            }
        }

        #endregion
    }
}