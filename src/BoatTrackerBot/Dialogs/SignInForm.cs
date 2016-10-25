﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot
{
    [Serializable]
    public class SignInForm
    {
        [NonSerialized]
        private JToken matchedUser;

        [Prompt("What are the initials of your rowing or paddling club?")]
        [Template(TemplateUsage.StatusFormat, "Club initials: {}")]
        [Template(TemplateUsage.NavigationFormat, "Club Initials ({})")]
        public string ClubInitials { get; set; }

        [Prompt("What is your user name?")]
        [Template(TemplateUsage.StatusFormat, "User name: {}")]
        [Template(TemplateUsage.NavigationFormat, "User Name ({})")]
        public string UserName { get; set; }

        [Prompt("What is your password?")]
        [Template(TemplateUsage.StatusFormat, "Password: {}")]
        [Template(TemplateUsage.NavigationFormat, "Password ({})")]
        public string Password { get; set; }

        public static IForm<SignInForm> BuildForm()
        {
            return new FormBuilder<SignInForm>()
                .Field(nameof(ClubInitials), validate: ValidateClubInitials)
                .Field(nameof(UserName), validate: ValidateUserName)
                .Field(nameof(Password), validate: ValidatePassword)
                .Build();
        }

        private static Task<ValidateResult> ValidateClubInitials(SignInForm state, object value)
        {
            EnvironmentDefinition env = EnvironmentDefinition.Instance;
            string clubId = ((string)value).ToLower();

            // TODO: May eventually need to support duplicate club id's if we're successful enough.

            if (env.MapClubIdToClubInfo.ContainsKey(clubId))
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = true,
                    Value = clubId
                });
            }
            else
            {
                return Task.FromResult(new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = "Sorry, but I don't recognize the initials you entered."
                });
            }
        }

        private static async Task<ValidateResult> ValidateUserName(SignInForm state, object value)
        {
            var userName = (string)value;

            var matchedUser = (await BookedSchedulerCache.Instance[state.ClubInitials].GetUsersAsync())
                .Where(u => string.Compare(u.UserName(), userName, true) == 0)
                .FirstOrDefault();

            if (matchedUser != null)
            {
                return new ValidateResult
                {
                    IsValid = true,
                    Value = matchedUser.UserName()
                };
            }
            else
            {
                var clubName = EnvironmentDefinition.Instance.MapClubIdToClubInfo[state.ClubInitials].Name;

                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"I don't see that user name in the roster for {clubName}. You may need to check with your club's administrator."
                };
            }
        }

        private static async Task<ValidateResult> ValidatePassword(SignInForm state, object value)
        {
            string password = (string)value;
            var clubInfo = EnvironmentDefinition.Instance.MapClubIdToClubInfo[state.ClubInitials];

            var client = new BookedSchedulerClient(clubInfo.Url);

            try
            {
                await client.SignIn(state.UserName, password);
            }
            catch (HttpRequestException)
            {
                return new ValidateResult
                {
                    IsValid = false,
                    Value = null,
                    Feedback = $"I'm sorry but your password is incorrect. Please try again."
                };
            }

            return new ValidateResult
            {
                IsValid = true,
                Value = password
            };
        }
    }
}