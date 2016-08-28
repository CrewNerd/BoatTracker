using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.FormFlow;

using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot
{
    [Serializable]
    public class ReservationRequest
    {
        private TimeSpan rawDuration;
        private string duration;

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
                    return this.StartDate.Value.Date + this.StartTime.Value.TimeOfDay;
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
                .Field(nameof(BoatName))
                .Field(nameof(StartDate))
                .Field(nameof(StartTime))
                .Field(nameof(Duration))
                .Confirm(GenerateConfirmationMessage)
                .Build();
        }

        private static Task<PromptAttribute> GenerateConfirmationMessage(ReservationRequest state)
        {
            return Task.FromResult(
                new PromptAttribute(
                    $"You want to reserve the {state.BoatName} on {state.StartDate.Value.ToLongDateString()} at {state.StartTime.Value.ToShortTimeString()} for {state.Duration}. Is that right?"));
        }
    }
}