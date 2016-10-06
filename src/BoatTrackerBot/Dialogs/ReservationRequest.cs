using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.FormFlow;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.DataObjects;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot
{
    [Serializable]
    public class ReservationRequest
    {
        private TimeSpan rawDuration;
        private string duration;

        public long BoatId { get; set; }

        public UserState UserState { get; set; }

        public bool CheckInAfterCreation { get; set; }

        [Prompt("What boat do you want to reserve?")]
        public string BoatName { get; set; }

        [Prompt("What day do you want to reserve it?")]
        [Template(TemplateUsage.StatusFormat, "Start date: {:d}")]
        public DateTime? StartDate { get; set; }

        [Prompt("What time do you want to start?")]
        [Template(TemplateUsage.StatusFormat, "Start time: {:t}")]
        public DateTime? StartTime { get; set; }

        [Prompt("How long do you want to use the boat?")]
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
                .Confirm(GenerateConfirmationMessage)
                .Build();
        }

        private static async Task<ValidateResult> ValidateBoatName(ReservationRequest state, object value)
        {
            var boatName = (string)value;
            var boat = await state.UserState.FindBestResourceMatchAsync(boatName);

            if (boat != null)
            {
                if (await state.UserState.HasPermissionForResourceAsync(boat))
                {
                    state.BoatId = boat.ResourceId();
                    return new ValidateResult
                    {
                        IsValid = true,
                        Value = boat.Name()
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
                    Feedback = "Sorry, but I don't recognize that boat name"
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

            TimeSpan StartLowerBound = state.UserState.ClubInfo().EarliestUseTime;
            TimeSpan StartUpperBound = state.UserState.ClubInfo().LatestUseTime;

            if (time.Minutes != 0 && time.Minutes != 15 && time.Minutes != 30 && time.Minutes != 45)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations must start on an even 15-minute slot."
                });
            }

            if (time < StartLowerBound)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations can't be made earlier than {(DateTime.MinValue + StartLowerBound).ToShortTimeString()}"
                });
            }

            if (time >= StartUpperBound)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"Reservations can't be made later than {(DateTime.MinValue + StartUpperBound).ToShortTimeString()}"
                });
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
            return Task.FromResult(
                new PromptAttribute(
                    $"You want to reserve the {state.BoatName} on {state.StartDate.Value.ToLongDateString()} at {state.StartTime.Value.ToShortTimeString()} for {state.Duration}. Is that right?"));
        }
    }
}