using System;
using System.Threading.Tasks;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Utils;

using Microsoft.Bot.Builder.FormFlow;

namespace BoatTracker.Bot
{
    [Serializable]
    public class ReservationRequest
    {
        private TimeSpan rawDuration;
        private string duration;

        public long BoatId { get; set; }

        public long? PartnerUserId { get; set; }

        public DateTime? OriginalStartDate { get; set; }

        public DateTime? OriginalStartTime { get; set; }

        public UserState UserState { get; set; }

        public bool CheckInAfterCreation { get; set; }

        [Prompt("What boat do you want to reserve?")]
        [Template(TemplateUsage.StatusFormat, "Boat name: {}")]
        [Template(TemplateUsage.NavigationFormat, "Boat Name ({})")]
        public string BoatName { get; set; }

        [Prompt("Who are you rowing with?")]
        [Template(TemplateUsage.StatusFormat, "Partner name: {}")]
        [Template(TemplateUsage.NavigationFormat, "Partner Name ({})")]
        public string PartnerName { get; set; }

        [Prompt("What day do you want to reserve it?")]
        [Template(TemplateUsage.StatusFormat, "Start date: {:d}")]
        [Template(TemplateUsage.NavigationFormat, "Start Date ({:d})")]
        public DateTime? StartDate { get; set; }

        [Prompt("What time do you want to start?")]
        [Template(TemplateUsage.StatusFormat, "Start time: {:t}")]
        [Template(TemplateUsage.NavigationFormat, "Start Time ({:t})")]
        public DateTime? StartTime { get; set; }

        [Prompt("How long do you want to use the boat?")]
        [Template(TemplateUsage.StatusFormat, "Duration: {}")]
        [Template(TemplateUsage.NavigationFormat, "Duration ({})")]
        public string Duration
        {
            get
            {
                return this.duration;
            }

            set
            {
                TimeSpan? ts = TimeSpanExtensions.FromDisplayString(value);

                if (ts.HasValue)
                {
                    this.rawDuration = ts.Value;
                    this.duration = ts.Value.ToDisplayString();
                }
                else
                {
                    // TODO: throw an exception here instead?

                    this.rawDuration = TimeSpan.Zero;
                    this.duration = null;
                }
            }
        }

        public TimeSpan RawDuration
        {
            get
            {
                return this.rawDuration;
            }

            set
            {
                this.rawDuration = value;
                this.duration = this.rawDuration.ToDisplayString();
            }
        }

        public DateTime? StartDateTime
        {
            get
            {
                if (this.StartDate.HasValue && this.StartTime.HasValue)
                {
                    return new DateTime((this.StartDate.Value.Date + this.StartTime.Value.TimeOfDay).Ticks, DateTimeKind.Unspecified);
                }
                else
                {
                    return null;
                }
            }
        }

        public static IForm<ReservationRequest> BuildForm()
        {
            return new FormBuilder<ReservationRequest>()
                .Field(nameof(BoatName), validate: ValidateBoatName)
                .Field(nameof(StartDate), validate: ValidateStartDate)
                .Field(nameof(StartTime), validate: ValidateStartTime)
                .Field(nameof(Duration), validate: ValidateDuration)
                .Field(
                    nameof(PartnerName),
                    state =>
                    {
                        // We only need another rower name if the boat isn't a single.
                        var boatTask = BookedSchedulerCache.Instance[state.UserState.ClubId].GetResourceFromIdAsync(state.BoatId);
                        boatTask.Wait();
                        var boat = boatTask.Result;
                        return boat.MaxParticipants() > 1;
                    },
                    validate: ValidatePartnerName)
                .Confirm(GenerateConfirmationMessage)
                .Build();
        }

        private static async Task<ValidateResult> ValidateBoatName(ReservationRequest state, object value)
        {
            var boatName = (string)value;
            var boatMatch = await state.UserState.FindBestResourceMatchAsync(boatName);

            if (boatMatch.Item1 != null)
            {
                if (await state.UserState.HasPermissionForResourceAsync(boatMatch.Item1))
                {
                    bool partnerRemoved = false;

                    //
                    // If the user selects a single, make sure we clear any partner that they mentioned.
                    //
                    if (boatMatch.Item1.MaxParticipants() == 1 && !string.IsNullOrEmpty(state.PartnerName))
                    {
                        partnerRemoved = true;
                        state.PartnerUserId = null;
                        state.PartnerName = null;
                    }

                    state.BoatId = boatMatch.Item1.ResourceId();
                    return new ValidateResult
                    {
                        IsValid = true,
                        Value = boatMatch.Item1.Name(),
                        Feedback = partnerRemoved ? "The boat you selected only holds a single person, so I removed the partner you mentioned before." : null
                    };
                }
                else
                {
                    return new ValidateResult
                    {
                        IsValid = false,
                        Value = null,
                        Feedback = "Sorry, but you don't have permission to use that boat."
                    };
                }
            }
            else
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = boatMatch.Item2
                };
            }
        }

        private static async Task<ValidateResult> ValidatePartnerName(ReservationRequest state, object value)
        {
            var partnerUserName = (string)value;
            var partnerUserMatch = await state.UserState.FindBestUserMatchAsync(partnerUserName);

            var boat = await BookedSchedulerCache.Instance[state.UserState.ClubId].GetResourceFromIdAsync(state.BoatId);

            if (partnerUserMatch.Item1 != null)
            {
                var partnerUserId = partnerUserMatch.Item1.Id();

                //
                // Private boats are a special case. The boat owner can invite anyone they wish to row
                // with them. But for club boats, both rowers must have permission.
                //
                if (boat.IsPrivate() || await state.UserState.HasPermissionForResourceAsync(boat, partnerUserId))
                {
                    state.PartnerUserId = partnerUserId;
                    return new ValidateResult
                    {
                        IsValid = true,
                        Value = partnerUserMatch.Item1.FullName()
                    };
                }
                else
                {
                    return new ValidateResult
                    {
                        IsValid = false,
                        Value = null,
                        Feedback = "Sorry, but your partner doesn't have permission to use that boat."
                    };
                }
            }
            else
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = partnerUserMatch.Item2
                };
            }
        }

        private static Task<ValidateResult> ValidateStartDate(ReservationRequest state, object value)
        {
            DateTime startDate = (DateTime)value;

            if (startDate.Date < state.UserState.LocalTime().Date)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "You can't make reservations in the past"
                });
            }

            // TODO: This should be a club-specific policy setting
            if (startDate.Date > (state.UserState.LocalTime() + TimeSpan.FromDays(14)))
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "You can only make reservations for the next two weeks"
                });
            }

            //
            // If the date was changed during the FormDialog, then we can no longer attempt
            // a checkin, even if that was the original intent. This recovers from some
            // cases where the original intent was misunderstood.
            //
            if (state.StartDate != state.OriginalStartDate)
            {
                state.CheckInAfterCreation = false;
            }

            return Task.FromResult(new ValidateResult
            {
                IsValid = true,
                Value = startDate.Date
            });
        }

        private static Task<ValidateResult> ValidateStartTime(ReservationRequest state, object value)
        {
            DateTime startTime = (DateTime)value;
            TimeSpan time = startTime.TimeOfDay;

            TimeSpan startLowerBound = state.UserState.ClubInfo().EarliestUseTime;
            TimeSpan startUpperBound = state.UserState.ClubInfo().LatestUseTime;

            if (time.Minutes != 0 && time.Minutes != 15 && time.Minutes != 30 && time.Minutes != 45)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations must start on an even 15-minute slot."
                });
            }

            if (time < startLowerBound)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations can't be made earlier than {(DateTime.MinValue + startLowerBound).ToShortTimeString()}"
                });
            }

            if (time >= startUpperBound)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations can't be made later than {(DateTime.MinValue + startUpperBound).ToShortTimeString()}"
                });
            }

            //
            // If the time was changed during the FormDialog, then we can no longer attempt
            // a checkin, even if that was the original intent. This recovers from some
            // cases where the original intent was misunderstood.
            //
            if (state.StartTime != state.OriginalStartTime)
            {
                state.CheckInAfterCreation = false;
            }

            return Task.FromResult(new ValidateResult
            {
                IsValid = true,
                Value = startTime
            });
        }

        private static async Task<ValidateResult> ValidateDuration(ReservationRequest state, object value)
        {
            var duration = TimeSpanExtensions.FromDisplayString((string)value);

            if (!duration.HasValue)
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "That doesn't look like a valid duration. Please try again."
                };
            }

            if (duration.Value.Minutes % 15 != 0)
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations must last for quarter-hour increments."
                };
            }

            var minDuration = state.UserState.ClubInfo().MinimumDuration;

            if (duration.Value < minDuration)
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations must be for at least {minDuration.TotalMinutes} minutes"
                };
            }

            var boatResource = await BookedSchedulerCache.Instance[state.UserState.ClubId].GetResourceFromIdAsync(state.BoatId);
            var maxDuration = state.UserState.ClubInfo().MaximumDuration;

            if (duration.Value > maxDuration && !boatResource.IsPrivate())
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"The maximum duration allowed is {maxDuration.TotalHours} hours."
                };
            }

            return new ValidateResult
            {
                IsValid = true,
                Value = duration.Value.ToDisplayString()
            };
        }

        private static Task<PromptAttribute> GenerateConfirmationMessage(ReservationRequest state)
        {
            if (string.IsNullOrEmpty(state.PartnerName))
            {
                return Task.FromResult(
                    new PromptAttribute(
                        $"You want to reserve the {state.BoatName} on {state.StartDate.Value.ToLongDateString()} at {state.StartTime.Value.ToShortTimeString()} for {state.Duration}. Is that right? (yes/no)"));
            }
            else
            {
                return Task.FromResult(
                    new PromptAttribute(
                        $"You want to reserve the {state.BoatName} with {state.PartnerName} on {state.StartDate.Value.ToLongDateString()} at {state.StartTime.Value.ToShortTimeString()} for {state.Duration}. Is that right? (yes/no)"));
            }
        }
    }
}