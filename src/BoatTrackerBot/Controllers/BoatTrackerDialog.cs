using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;

using BoatTracker.Bot.Configuration;
using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Utils;

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

        [NonSerialized]
        private UserState currentUserState;

        [NonSerialized]
        private BookedSchedulerClient cachedClient;

        [NonSerialized]
        private EnvironmentDefinition env;

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

            var client = await this.GetClient();

            var reservations = await client.GetReservationsForUser(this.currentUserState.UserId);

            // TODO: check for entities that suggest filtering by date or resource name

            if (reservations.Count == 0)
            {
                await context.PostAsync($"I don't see any reservations for you, currently.");
            }
            else
            {
                string reservationDescription = await this.DescribeReservations(reservations);
                await context.PostAsync($"I found the following reservations:\r\n---{reservationDescription}");
            }

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

        private EnvironmentDefinition Env
        {
            get
            {
                if (this.env == null)
                {
                    this.env = EnvironmentDefinition.CreateFromEnvironment();
                }

                return this.env;
            }
        }

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

            // Check that the user state is complete and has been refreshed in the last 2 days
            if (userState.UserId != 0 && !string.IsNullOrEmpty(userState.ClubId)
                && userState.Timestamp != null && userState.Timestamp + TimeSpan.FromDays(2) > DateTime.Now)
            {
                // The user is fully registered and their data is reasonably current
                this.currentUserState = userState;
                return true;
            }

            var builtUserState = await this.Env.TryBuildStateForUser(userState.BotAccountKey);

            if (builtUserState != null)
            {
                // The user just registered. Save their club info now so we don't have to
                // do this lookup every time.
                userState.ClubId = builtUserState.ClubId;
                userState.UserId = builtUserState.UserId;
                userState.Timezone = builtUserState.Timezone;
                userState.Timestamp = DateTime.Now;

                context.UserData.SetValue(UserState.PropertyName, userState);
                this.currentUserState = userState;
                return true;
            }
            else
            {
                await context.PostAsync($"It looks like you haven't registered your Bot account with BookedScheduler yet. To connect your Skype account to BookedScheduler, please go to your BookedScheduler profile and set your '{UserState.BotAccountKeyDisplayName}' to {userState.BotAccountKey}.");
                context.Wait(MessageReceived);
                return false;
            }
        }

        private async Task<BookedSchedulerClient> GetClient()
        {
            var clubInfo = this.Env.MapClubIdToClubInfo[this.currentUserState.ClubId];

            if (this.cachedClient == null)
            {
                this.cachedClient = new BookedSchedulerClient(clubInfo.Url);
            }

            if (!this.cachedClient.IsSignedIn || this.cachedClient.IsSessionExpired)
            {
                await this.cachedClient.SignIn(clubInfo.UserName, clubInfo.Password);
            }

            return this.cachedClient;
        }

        private async Task<string> DescribeReservations(JArray reservations)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var reservation in reservations)
            {
                DateTime startDate = DateTime.Parse(reservation.Value<string>("startDate"));
                startDate = this.ConvertToLocalTime(this.currentUserState, startDate);
                var duration = reservation.Value<string>("duration");

                var boatName = await BookedSchedulerCache
                    .Instance[this.currentUserState.ClubId]
                    .GetResourceNameFromIdAsync(reservation.Value<long>("resourceId"));

                sb.AppendFormat("\r\n\r\n**{0} {1}** {2} *({3})*", startDate.ToLocalTime().ToString("d"), startDate.ToLocalTime().ToString("t"), boatName, duration);
            }

            return sb.ToString();
        }

        private DateTime ConvertToLocalTime(UserState userState, DateTime dateTime)
        {
            var mappings = TzdbDateTimeZoneSource.Default.WindowsMapping.MapZones;
            var mappedTz = mappings.FirstOrDefault(x => x.TzdbIds.Any(z => z.Equals(userState.Timezone, StringComparison.OrdinalIgnoreCase)));

            if (mappedTz == null)
            {
                return dateTime;
            }

            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(mappedTz.WindowsId);
            return dateTime + tzInfo.GetUtcOffset(dateTime);
        }

        #endregion
    }
}