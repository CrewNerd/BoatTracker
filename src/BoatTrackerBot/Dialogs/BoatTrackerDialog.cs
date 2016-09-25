﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot
{
    [Serializable]
    public class BoatTrackerDialog : LuisDialog<object>
    {
        [NonSerialized]
        private UserState currentUserState;

        [NonSerialized]
        private ChannelInfo currentChannelInfo;

        [NonSerialized]
        private BookedSchedulerClient cachedClient;

        private string pendingReservationToCancel;

        private List<string> pendingReservationsToCancel;

        public BoatTrackerDialog(ILuisService service)
            : base(service)
        {
        }

        [NonSerialized]
        private TelemetryClient telemetryClient;

        private TelemetryClient TelemetryClient
        {
            get
            {
                if (this.telemetryClient == null)
                {
                    this.telemetryClient = new TelemetryClient();
                }

                return this.telemetryClient;
            }
        }

        #region Intents

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            if (result.Query.StartsWith("#!"))
            {
                string command = result.Query.Substring(2);

                this.TelemetryClient.TrackEvent("ControlMessage", new Dictionary<string, string> { ["Command"] = command });

                await this.ProcessControlMessage(context, command);
                context.Wait(MessageReceived);
                return;
            }

            if (!await this.CheckUserIsRegistered(context)) { return; }

            this.TrackIntent(context, "None");

            bool forceHelp = result.Query.ToLower().Contains("help") || result.Query.Equals("?");

            if (forceHelp || !this.currentUserState.HelpMessageShown)
            {
                await this.ShowHelpMessage(context);
            }
            else
            {
                string message = "I'm sorry, I don't understand. Enter '?' to see what you can say at any time.";
                await context.PostAsync(message);
            }

            context.Wait(MessageReceived);
        }

        private async Task ProcessControlMessage(IDialogContext context, string msg)
        {
            Func<Task> help = async () =>
            {
                await context.PostAsync(
                    "clearuserdata - clear all data for the calling user\n\n" +
                    "resetcache - clear the cache of BookedScheduler data");
            };

            if (msg == "clearuserdata")
            {
                context.UserData.Clear();
                await context.FlushAsync(CancellationToken.None);
                await context.PostAsync("User data cleared");
            }
            else if (msg == "resetcache")
            {
                BookedSchedulerCache.Instance.ResetCache();
                await context.PostAsync("Cache reset complete");
            }
            else if (msg.StartsWith("CancelReservation "))
            {
                //
                // This command is generated by the buttons we produce when the user wants
                // to cancel a reservation and their intent specifies multiple possibilities.
                //
                if (!await this.CheckUserIsRegistered(context)) { return; }

                string referenceNumber = msg.Split(' ')[1];

                var client = await this.GetClient();
                try
                {
                    await client.DeleteReservationAsync(referenceNumber);
                    await context.PostAsync("Okay, your reservation is cancelled!");
                }
                catch (Exception)
                {
                    await context.PostAsync("I'm sorry, but I couldn't cancel your reservation. Please try again later.");
                }
            }
            else if (msg == "help")
            {
                await help.Invoke();
            }
            else
            {
                await context.PostAsync($"Unrecognized command: {msg}");
                await help.Invoke();
            }
        }

        [LuisIntent("CreateReservation")]
        public async Task CreateReservation(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            this.TrackIntent(context, "CreateReservation");

            var boatName = await this.currentUserState.FindBestResourceNameAsync(result);
            var startDate = result.FindStartDate(this.currentUserState);
            var startTime = result.FindStartTime(this.currentUserState);
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

            this.TrackIntent(context, "CheckBoatAvailability");

            //
            // Check for (and apply) a boat name filter
            //
            var boat = await this.currentUserState.FindBestResourceMatchAsync(result);

            if (result.ContainsBoatNameEntity() && boat == null)
            {
                await context.PostAsync($"I'm sorry, but I don't recognize the boat name '{result.BoatName()}'.");
            }

            var client = await this.GetClient();

            IList<JToken> reservations = null;
            string filterDescription = string.Empty;

            if (boat != null)
            {
                reservations = (await client.GetReservationsAsync(resourceId: boat.ResourceId())).ToList();

                filterDescription += $" for the {boat.Name()}";
            }
            else
            {
                reservations = (await client.GetReservationsAsync()).ToList();
            }

            var startDate = result.FindStartDate(this.currentUserState);

            bool showDate = true;

            if (startDate.HasValue)
            {
                reservations = reservations
                    .Where(r =>
                    {
                        var rStartDate = this.currentUserState.ConvertToLocalTime(
                            DateTime.Parse(r.StartDate()));
                        return rStartDate.DayOfYear == startDate.Value.DayOfYear;
                    })
                    .ToList();

                filterDescription += $" on {startDate.Value.ToShortDateString()}";
                showDate = false;
            }

            if (reservations.Count == 0)
            {
                await context.PostAsync($"I don't see any reservations{filterDescription}, currently.");
            }
            else
            {
                string reservationDescription = await this.currentUserState.DescribeReservationsAsync(
                    reservations,
                    showOwner:true,
                    showDate:showDate,
                    useMarkdown:this.currentChannelInfo.SupportsMarkdown);

                await context.PostAsync($"I found the following reservation{this.Pluralize(reservations.Count)}{filterDescription}:\n\n---{reservationDescription}");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("Return")]
        public async Task Return(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            this.TrackIntent(context, "Return");

            string boatName = await this.currentUserState.FindBestResourceNameAsync(result);

            if (string.IsNullOrEmpty(boatName))
            {
                await context.PostAsync("It sounds like you want to return a boat but I don't know how to do that yet.");
            }
            else
            {
                await context.PostAsync($"It sounds like you want to return the '{boatName}' but I don't know how to do that yet.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("TakeOut")]
        public async Task TakeOut(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            this.TrackIntent(context, "TakeOut");

            string boatName = await this.currentUserState.FindBestResourceNameAsync(result);

            if (string.IsNullOrEmpty(boatName))
            {
                await context.PostAsync("It sounds like you want to take out a boat but I don't know how to do that yet.");
            }
            else
            {
                await context.PostAsync($"It sounds like you want to take out the '{boatName}' but I don't know how to do that yet.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("CheckReservations")]
        public async Task CheckReservations(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            this.TrackIntent(context, "CheckReservations");

            var client = await this.GetClient();

            var reservations = (await client.GetReservationsForUserAsync(this.currentUserState.UserId)).ToList();

            string filterDescription = string.Empty;

            //
            // Check for (and apply) a boat name filter
            //
            var boat = await this.currentUserState.FindBestResourceMatchAsync(result);

            if (boat != null)
            {
                reservations = reservations
                    .Where(r => r.ResourceId() == boat.ResourceId())
                    .ToList();

                filterDescription = $" for the {boat.Name()}";
            }
            else
            {
                filterDescription = " for you";
            }

            var startDate = result.FindStartDate(this.currentUserState);

            bool showDate = true;

            if (startDate.HasValue)
            {
                reservations = reservations
                    .Where(r =>
                    {
                        var rStartDate = this.currentUserState.ConvertToLocalTime(
                            DateTime.Parse(r.StartDate()));
                        return rStartDate.DayOfYear == startDate.Value.DayOfYear;
                    })
                    .ToList();

                showDate = false;
                filterDescription += $" on {startDate.Value.ToShortDateString()}";
            }

            if (reservations.Count == 0)
            {
                await context.PostAsync($"I don't see any reservations{filterDescription}, currently.");
            }
            else
            {
                string reservationDescription = await this.currentUserState.DescribeReservationsAsync(
                    reservations,
                    showDate: showDate,
                    useMarkdown: this.currentChannelInfo.SupportsMarkdown);

                await context.PostAsync($"I found the following reservation{this.Pluralize(reservations.Count)}{filterDescription}:\n\n---{reservationDescription}");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("CancelReservation")]
        public async Task CancelReservation(IDialogContext context, LuisResult result)
        {
            if (!await this.CheckUserIsRegistered(context)) { return; }

            this.TrackIntent(context, "CancelReservation");

            var client = await this.GetClient();

            var reservations = (await client.GetReservationsForUserAsync(this.currentUserState.UserId)).ToList();

            string filterDescription = string.Empty;

            //
            // Check for (and apply) a boat name filter
            //
            var boat = await this.currentUserState.FindBestResourceMatchAsync(result);

            if (boat != null)
            {
                reservations = reservations
                    .Where(r => r.ResourceId() == boat.ResourceId())
                    .ToList();

                filterDescription = $" for the {boat.Name()}";
            }
            else
            {
                filterDescription = " for you";
            }

            var startDate = result.FindStartDate(this.currentUserState);

            bool showDate = true;

            if (startDate.HasValue)
            {
                reservations = reservations
                    .Where(r =>
                    {
                        var rStartDate = this.currentUserState.ConvertToLocalTime(
                            DateTime.Parse(r.StartDate()));
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
                        showDate: showDate,
                        useMarkdown: this.currentChannelInfo.SupportsMarkdown);

                    this.pendingReservationToCancel = reservations[0].ReferenceNumber();

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
                    if (this.currentChannelInfo.SupportsButtons)
                    {
                        var response = context.MakeMessage();
                        List<CardAction> cardButtons = new List<CardAction>();

                        foreach (var reservation in reservations)
                        {
                            cardButtons.Add(new CardAction
                            {
                                Type = ActionTypes.ImBack,
                                Value = $"#!CancelReservation {reservation.ReferenceNumber()}",
                                Title = await this.currentUserState.SummarizeReservationAsync(reservation)
                            });
                        }

                        HeroCard card = new HeroCard()
                        {
                            Title = $"I found multiple reservations{filterDescription}",
                            Subtitle = "Please press the button for each reservation you want to cancel.",
                            Buttons = cardButtons
                        };

                        response.Attachments.Add(card.ToAttachment());

                        await context.PostAsync(response);
                        context.Wait(MessageReceived);
                    }
                    else
                    {
                        reservationDescription = await this.currentUserState.DescribeReservationsAsync(
                            reservations,
                            showDate: showDate,
                            showIndex: true,
                            useMarkdown: this.currentChannelInfo.SupportsMarkdown);

                        await context.PostAsync($"I found multiple reservations{filterDescription}:\n\n{reservationDescription}\n\n");

                        this.pendingReservationsToCancel = reservations.Select(r => r.ReferenceNumber()).ToList();

                        PromptDialog.Number(
                            context,
                            AfterSelectingReservation_DeleteReservation,
                            $"Please enter the number of the reservation you want to cancel, or {reservations.Count + 1} for 'none'.",
                            "I'm sorry, but that isn't a valid response. Please select one of the options listed above.",
                            3);
                    }
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

        #region Misc Helpers

        /// <summary>
        /// Checks to see if the user has connected their channel and BookedScheduler accounts. If so, then
        /// the currentUserState and currentChannelInfo members are initialized and we return true. If the
        /// accounts are not yet connected, we return false and no further action of any significance should
        /// be performed. Intent handlers (and other callbacks) should invoke this method first and bail out
        /// if we return false. If we return false, we also post instructions to the user for making the
        /// connection.
        /// </summary>
        /// <param name="context">The caller's dialog context</param>
        /// <returns>True if the user accounts are connected, false otherwise.</returns>
        private async Task<bool> CheckUserIsRegistered(IDialogContext context)
        {
            this.currentChannelInfo = EnvironmentDefinition.Instance.GetChannelInfo(context.GetChannel());

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

            var builtUserState = await EnvironmentDefinition.Instance.TryBuildStateForUser(userState.BotAccountKey, context.GetChannel());

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
                await context.PostAsync(
                    "It looks like you haven't registered your Bot account with BookedScheduler yet. " +
                    "To connect your Skype account to BookedScheduler, please go to your BookedScheduler " +
                    $"profile and set your '{this.currentChannelInfo.BotAccountKeyDisplayName}' to {userState.BotAccountKey}");

                context.Wait(MessageReceived);
                return false;
            }
        }

        private void TrackIntent(IDialogContext context, string intent)
        {
            this.TelemetryClient.TrackEvent(intent, new Dictionary<string, string>
            {
                ["ClubId"] = this.currentUserState.ClubId,
                ["Channel"] = context.GetChannel()
            });
        }

        /// <summary>
        /// Gets a client object for the user's BookedScheduler instance. These are cached and reused.
        /// </summary>
        /// <returns>A BookedSchedulerClient instance that is signed in and ready for use.</returns>
        private async Task<BookedSchedulerClient> GetClient()
        {
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[this.currentUserState.ClubId];

            if (this.cachedClient == null)
            {
                this.cachedClient = new BookedSchedulerLoggingClient(this.currentUserState.ClubId);
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
                "## Taking out a boat\n\n" +
                "* Take out the Little Thunder for two hours\n\n" +
                "## Returning a boat\n\n" +
                "* Return the Little Thunder"
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