using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.FormFlow;

namespace BoatTracker.Bot
{
    [Serializable]
    public class ReservationRequest
    {
        [Prompt("What boat would you like to reserve?")]
        public string BoatName { get; set; }

        [Prompt("What day do you want to row?")]
        public DateTime? StartDate { get; set; }

        [Prompt("What time do you want to row?")]
        public DateTime? StartTime { get; set; }

        [Prompt("How long will you be out?")]
        public string Duration { get; set; }

        public DateTime? StartDateTime
        {
            get
            {
                if (this.StartDate.HasValue && this.StartTime.HasValue)
                {
                    return new DateTime(
                        this.StartDate.Value.Year,
                        this.StartDate.Value.Month,
                        this.StartDate.Value.Day,
                        this.StartTime.Value.Hour,
                        this.StartTime.Value.Minute,
                        this.StartTime.Value.Second);
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
                    $"You asked to reserve the {state.BoatName} on {state.StartDate.Value.ToShortDateString()} at {state.StartTime.Value.ToShortTimeString()} for {state.Duration}. Is that right?"));
        }
    }
}