﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;

using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot
{
    [Serializable]
    public class BoatTrackerDialog : LuisDialog<object>
    {
        [NonSerialized]
        private UserState currentUserState;

        [NonSerialized]
        private BookedSchedulerClient cachedClient;

        private string pendingReservationToCancel;

        private List<string> pendingReservationsToCancel;

        public BoatTrackerDialog(ILuisService service)
            : base(service)
        {
        }

        #region Intents

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            if (result.Query.StartsWith("#!"))
            {
                await this.ProcessControlMessage(context, result.Query.Substring(2));
                context.Wait(MessageReceived);
                return;
            }

            if (!await this.CheckUserIsRegistered(context)) { return; }

            bool forceHelp = result.Query.ToLower().Contains("help");

            if (forceHelp || !this.currentUserState.HelpMessageShown)
            {
                await this.ShowHelpMessage(context);
            }
            else
            {
                string message = "I'm sorry, I don't understand. Enter 'help' to see what you can say.";
                await context.PostAsync(message);
            }

            context.Wait(MessageReceived);
        }

        private async Task ProcessControlMessage(IDialogContext context, string msg)
        {
            if (msg == "clearuserdata")
            {
                context.UserData.Clear();
                await context.FlushAsync(CancellationToken.None);
                await context.PostAsync("User data cleared");
            }
            else
            {
                await context.PostAsync($"Unrecognized command: {msg}");
            }
        }

        [LuisIntent("CreateReservation")]
        public async Task CreateReservation(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            var boatName = await this.FindBoatNameAsync(result);
            var startDate = result.FindStartDate();
            var startTime = result.FindStartTime();
            var duration = result.FindDuration();

            ReservationRequest reservationRequest = new ReservationRequest
            {
                UserState = this.currentUserState,
                BoatName = boatName,
                StartDate = startDate,
                StartTime = startTime
            };

            if (duration.HasValue)
            {
                reservationRequest.RawDuration = duration.Value;
            }

            if (result.ContainsBoatNameEntity() && boatName == null)
            {
                await context.PostAsync($"I'm sorry, but I don't recognize the boat name '{result.BoatName()}'.");
            }

            var reservationForm = new FormDialog<ReservationRequest>(reservationRequest, ReservationRequest.BuildForm, FormOptions.PromptInStart, result.Entities);
            context.Call(reservationForm, ReservationComplete);
        }

        private async Task ReservationComplete(IDialogContext context, IAwaitable<ReservationRequest> result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            ReservationRequest request = null;

            try
            {
                request = await result;
            }
            catch (FormCanceledException)
            {
                await context.PostAsync("Okay, I'm aborting your reservation request.");
                context.Wait(MessageReceived);
                return;
            }

            if (request != null)
            {
                DateTimeOffset start = new DateTimeOffset(request.StartDateTime.Value, this.currentUserState.LocalOffsetForDate(request.StartDateTime.Value));

                var client = await this.GetClient();

                try
                {
                    JToken boat = await BookedSchedulerCache.Instance[this.currentUserState.ClubId].GetResourceFromIdAsync(request.BoatId);

                    if (boat == null)
                    {
                        boat = await this.currentUserState.FindBestResourceMatchAsync(request.BoatName);
                    }

                    await client.CreateReservationAsync(boat, this.currentUserState.UserId, start, request.RawDuration, $"Practice in the {request.BoatName}", $"Created by BoatTracker Bot");

                    await context.PostAsync("Okay, you're all set!");
                }
                catch (Exception ex)
                {
                    await context.PostAsync($"I'm sorry, but the reservation system rejected your request. {ex.Message}");
                }
            }
            else
            {
                await context.PostAsync("ERROR: Form returned an empty response!!");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("CheckBoatAvailability")]
        public async Task CheckBoatAvailability(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            //
            // Check for (and apply) a boat name filter
            //
            var boat = await this.FindBoatAsync(result);

            if (result.ContainsBoatNameEntity() && boat == null)
            {
                await context.PostAsync($"I'm sorry, but I don't recognize the boat name '{result.BoatName()}'.");
            }

            var client = await this.GetClient();

            IList<JToken> reservations = null;
            string filterDescription = string.Empty;

            if (boat != null)
            {
                long resourceId = boat.Value<long>("resourceId");
                reservations = (await client.GetReservationsAsync(resourceId: resourceId)).ToList();

                filterDescription += $" for the {boat.Value<string>("name")}";
            }
            else
            {
                reservations = (await client.GetReservationsAsync()).ToList();
            }

            var startDate = result.FindStartDate();

            bool showDate = true;

            if (startDate.HasValue)
            {
                reservations = reservations
                    .Where(r =>
                    {
                        var rStartDate = this.currentUserState.ConvertToLocalTime(
                            DateTime.Parse(r.Value<string>("startDate")));
                        return rStartDate.DayOfYear == startDate.Value.DayOfYear;
                    })
                    .ToList();

                filterDescription += $" on {startDate.Value.ToShortDateString()}";
                showDate = false;
            }

            if (reservations.Count == 0)
            {
                await context.PostAsync($"I don't see any reservations {filterDescription}, currently.");
            }
            else
            {
                string reservationDescription = await this.currentUserState.DescribeReservationsAsync(reservations, showOwner:true, showDate:showDate);
                await context.PostAsync($"I found the following reservation{this.Pluralize(reservations.Count)}{filterDescription}:\r\n---{reservationDescription}");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("Checkout")]
        public async Task Checkout(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            string boatName = await this.FindBoatNameAsync(result);

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

            string boatName = await this.FindBoatNameAsync(result);

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

            var reservations = (await client.GetReservationsForUserAsync(this.currentUserState.UserId)).ToList();

            string filterDescription = string.Empty;

            //
            // Check for (and apply) a boat name filter
            //
            var boat = await this.FindBoatAsync(result);

            if (boat != null)
            {
                reservations = reservations
                    .Where(r => r.Value<string>("resourceId") == boat.Value<string>("resourceId"))
                    .ToList();

                filterDescription = $" for the {boat.Value<string>("name")}";
            }
            else
            {
                filterDescription = " for you";
            }

            var startDate = result.FindStartDate();

            bool showDate = true;

            if (startDate.HasValue)
            {
                reservations = reservations
                    .Where(r =>
                    {
                        var rStartDate = this.currentUserState.ConvertToLocalTime(
                            DateTime.Parse(r.Value<string>("startDate")));
                        return rStartDate.DayOfYear == startDate.Value.DayOfYear;
                    })
                    .ToList();

                showDate = false;
                filterDescription += $" on {startDate.Value.ToShortDateString()}";
            }

            if (reservations.Count == 0)
            {
                await context.PostAsync($"I don't see any reservations {filterDescription}, currently.");
            }
            else
            {
                string reservationDescription = await this.currentUserState.DescribeReservationsAsync(reservations, showDate: showDate);
                await context.PostAsync($"I found the following reservation{this.Pluralize(reservations.Count)}{filterDescription}:\r\n---{reservationDescription}");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("CancelReservation")]
        public async Task CancelReservation(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            var client = await this.GetClient();

            var reservations = (await client.GetReservationsForUserAsync(this.currentUserState.UserId)).ToList();

            string filterDescription = string.Empty;

            //
            // Check for (and apply) a boat name filter
            //
            var boat = await this.FindBoatAsync(result);

            if (boat != null)
            {
                reservations = reservations
                    .Where(r => r.Value<string>("resourceId") == boat.Value<string>("resourceId"))
                    .ToList();

                filterDescription = $" for the {boat.Value<string>("name")}";
            }
            else
            {
                filterDescription = " for you";
            }

            var startDate = result.FindStartDate();

            bool showDate = true;

            if (startDate.HasValue)
            {
                reservations = reservations
                    .Where(r =>
                    {
                        var rStartDate = this.currentUserState.ConvertToLocalTime(
                            DateTime.Parse(r.Value<string>("startDate")));
                        return rStartDate.DayOfYear == startDate.Value.DayOfYear;
                    })
                    .ToList();

                showDate = false;
                filterDescription += $" on {startDate.Value.ToShortDateString()}";
            }

            string reservationDescription;

            switch (reservations.Count)
            {
                case 0:
                    await context.PostAsync($"I don't see any reservations{filterDescription}.");
                    context.Wait(MessageReceived);
                    break;

                case 1:
                    //
                    // We found a single matching reservation, so just give the user a prompt
                    // to confirm that this is the one that they want to cancel.
                    //
                    reservationDescription = await this.currentUserState.DescribeReservationsAsync(
                        reservations,
                        showDate: showDate);

                    this.pendingReservationToCancel = reservations[0].Value<string>("referenceNumber");

                    PromptDialog.Confirm(
                        context,
                        AfterConfirming_DeleteReservation,
                        $"Is this the reservation you want to cancel?\n\n---{reservationDescription}",
                        attempts: 3,
                        retry: "Sorry, I don't understand your response. Do you want to cancel the reservation shown above?",
                        promptStyle: PromptStyle.None);

                    break;

                default:
                    //
                    // We found multiple reservations matching the given criteria, so present
                    // them all and ask the user which one they want to cancel.
                    //
                    reservationDescription = await this.currentUserState.DescribeReservationsAsync(
                        reservations,
                        showDate: showDate,
                        showIndex: true);

                    await context.PostAsync($"I found multiple reservations{filterDescription}:\n\n{reservationDescription}\n\n");

                    this.pendingReservationsToCancel = reservations.Select(r => r.Value<string>("referenceNumber")).ToList();

                    PromptDialog.Number(
                        context,
                        AfterSelectingReservation_DeleteReservation,
                        $"Please enter the number of the reservation you want to cancel, or {reservations.Count + 1} for 'none'.",
                        "I'm sorry, but that isn't a valid response. Please select one of the options listed above.",
                        3);

                    break;
            }
        }

        public async Task AfterConfirming_DeleteReservation(IDialogContext context, IAwaitable<bool> confirmation)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            try
            {
                if (await confirmation)
                {
                    var client = await this.GetClient();
                    try
                    {
                        await client.DeleteReservationAsync(this.pendingReservationToCancel);
                        await context.PostAsync("Okay, your reservation is cancelled!");
                    }
                    catch (Exception)
                    {
                        await context.PostAsync("I'm sorry, but I couldn't cancel your reservation. Please try again later.");
                    }
                }
                else
                {
                    await context.PostAsync("Okay, your reservation is unchanged.");
                }
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("Sorry, I don't understand that. I'm leaving your reservation unchanged.");
            }

            context.Wait(MessageReceived);
        }

        public async Task AfterSelectingReservation_DeleteReservation(IDialogContext context, IAwaitable<long> confirmation)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            try
            {
                long index = await confirmation - 1;

                if (index >= 0 && index < this.pendingReservationsToCancel.Count)
                {
                    var client = await this.GetClient();
                    try
                    {
                        await client.DeleteReservationAsync(this.pendingReservationsToCancel[(int)index]);
                        await context.PostAsync("Okay, that reservation is cancelled now!");
                    }
                    catch (Exception)
                    {
                        await context.PostAsync("I'm sorry, but I couldn't cancel your reservation. Please try again later.");
                    }
                }
                else if (index == this.pendingReservationsToCancel.Count)
                {
                    await context.PostAsync("Okay, I'm leaving all of your reservations unchanged.");
                }
                else
                {
                    await context.PostAsync("The index you entered is invalid, so no reservation was cancelled.");
                }
            }
            catch (TooManyAttemptsException)
            {
                await context.PostAsync("Since you didn't select one of the listed options, I'm leaving all of your reservations unchanged.");
            }

            this.pendingReservationsToCancel = null;
            context.Wait(MessageReceived);
        }

        #endregion

        #region Entity Helpers

        private async Task<string> FindBoatNameAsync(LuisResult result)
        {
            var bestResource = await this.currentUserState.FindBestResourceMatchAsync(result.Entities);

            if (bestResource != null)
            {
                return bestResource.Value<string>("name");
            }

            return null;
        }

        private async Task<JToken> FindBoatAsync(LuisResult result)
        {
            return await this.currentUserState.FindBestResourceMatchAsync(result.Entities);
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

            // Check that the user state is complete and has been refreshed in the last 2 days
            if (userState.UserId != 0 && !string.IsNullOrEmpty(userState.ClubId)
                && userState.Timestamp != null && userState.Timestamp + TimeSpan.FromDays(2) > DateTime.Now)
            {
                // The user is fully registered and their data is reasonably current
                this.currentUserState = userState;
                return true;
            }

            var builtUserState = await EnvironmentDefinition.Instance.TryBuildStateForUser(userState.BotAccountKey);

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
                await context.PostAsync($"It looks like you haven't registered your Bot account with BookedScheduler yet. To connect your Skype account to BookedScheduler, please go to your BookedScheduler profile and set your '{EnvironmentDefinition.Instance.BotAccountKeyDisplayName}' to {userState.BotAccountKey}");
                context.Wait(MessageReceived);
                return false;
            }
        }

        private async Task<BookedSchedulerClient> GetClient()
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.currentUserState.ClubId];

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

        private async Task ShowHelpMessage(IDialogContext context)
        {
            await context.PostAsync(
                "I can help you with:\n\n" +
                "## Checking the availability of a boat\n\n" +
                "* Is the Little Thunder available on Thursday?\n\n" +
                "## Creating a reservation\n\n" +
                "* Reserve the Little Thunder on Thursday at 5am for 2 hours\n\n" +
                "## Canceling a reservation\n\n" +
                "* Cancel my reservation for the Little Thunder\n\n" +
                "## Reviewing reservations\n\n" +
                "* Show my reservations\n" +
                "* Did I reserve the Little Thunder?\n" +
                "* Show my reservation for the Little Thunder on Friday\n\n" +
                "## Checking out a boat\n\n" +
                "* Check out the Little Thunder for two hours\n\n" +
                "## Checking in a boat\n\n" +
                "* Check in\n" +
                "* Check in the Little Thunder"
            );

            this.currentUserState.HelpMessageShown = true;
            context.UserData.SetValue(UserState.PropertyName, this.currentUserState);
        }

        private string Pluralize(int count)
        {
            return count > 1 ? "s" : string.Empty;
        }

        #endregion
    }
}