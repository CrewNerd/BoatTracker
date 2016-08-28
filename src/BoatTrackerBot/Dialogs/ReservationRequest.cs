﻿using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.FormFlow;

using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Utils;

using Newtonsoft.Json.Linq;

namespace BoatTracker.Bot
{
    [Serializable]
    public class ReservationRequest
    {
        private TimeSpan rawDuration;
        private string duration;

        public JToken Boat { get; set; }

        public UserState UserState { get; set; }

        [Prompt("What boat would you like to reserve?")]
        public string BoatName { get; set; }

        [Prompt("What day do you want to row?")]
        public DateTime? StartDate { get; set; }

        [Prompt("What time do you want to row?")]
        public DateTime? StartTime { get; set; }

        [Prompt("How long will you be out?")]
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
            state.Boat = await state.UserState.FindBestResourceMatchAsync(boatName);

            if (state.Boat != null)
            {
                return new ValidateResult
                {
                    IsValid = true,
                    Value = state.Boat.Value<string>("name")
                };
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

            if (startDate.Date < DateTime.Now.Date)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "You can't make reservations in the past"
                });
            }

            // TODO: This should be a club-specific policy setting
            if (startDate.Date > (DateTime.Now.Date + TimeSpan.FromDays(14)))
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

            // TODO: should be based on the club's calendar

            TimeSpan StartLowerBound = TimeSpan.FromHours(5);
            TimeSpan StartUpperBound = TimeSpan.FromHours(19);

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

        private static Task<ValidateResult> ValidateDuration(ReservationRequest state, object value)
        {
            var duration = TimeSpanExtensions.FromDisplayString((string)value);

            if (!duration.HasValue)
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "That doesn't look like a valid duration. Please try again."
                });
            }

            // TODO: This should be per-club policy.

            if (duration.Value < TimeSpan.FromMinutes(30))
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "Reservatons must be for at least 30 minutes"
                });
            }

            if (duration.Value > TimeSpan.FromHours(24))
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "Multi-day reservations are not permitted."
                });
            }

            return Task.FromResult(new ValidateResult
            {
                IsValid = true,
                Value = duration.Value.ToDisplayString()
            });
        }

        private static Task<PromptAttribute> GenerateConfirmationMessage(ReservationRequest state)
        {
            return Task.FromResult(
                new PromptAttribute(
                    $"You want to reserve the {state.BoatName} on {state.StartDate.Value.ToLongDateString()} at {state.StartTime.Value.ToShortTimeString()} for {state.Duration}. Is that right?"));
        }
    }
}