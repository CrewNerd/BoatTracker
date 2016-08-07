using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using BoatTracker.BookedScheduler;
using BoatTracker.Bot.Configuration;

namespace BoatTracker.Bot
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

#if TEST
            RunTests().Wait();
#endif
        }

        private static async Task RunTests()
        {
            EnvironmentDefinition env = EnvironmentDefinition.CreateFromEnvironment();

            foreach (var id in env.ClubIds)
            {
                var clubInfo = env.MapClubIdToClubInfo[id];

                Trace.TraceInformation($"Club id: {id}");
                Trace.TraceInformation($"    Name: {clubInfo.Name}");
                Trace.TraceInformation($"    Url: {clubInfo.Url}");
                Trace.TraceInformation($"    UserName: {clubInfo.UserName}");
                Trace.TraceInformation($"    Password: {clubInfo.Password}");
            }
            var testClub = env.MapClubIdToClubInfo[env.ClubIds.First()];

            BookedSchedulerClient client = new BookedSchedulerClient(testClub.Url);

            await client.SignIn(testClub.UserName, testClub.Password);

            var users = await client.GetUsers();
            var firstUser = users.First();
            var user = await client.GetUser((string)firstUser["id"]);

            var resources = await client.GetResources();
            var firstBoat = resources.First();
            var boat = await client.GetResource((string)firstBoat["resourceId"]);

            var groups = await client.GetGroups();
            var firstGroup = groups.First();
            var group = await client.GetGroup((string)firstGroup["id"]);

            var schedules = await client.GetSchedules();
            var firstSchedule = schedules.First();
            var schedule = await client.GetSchedule((string)firstSchedule["id"]);
            var scheduleSlots = await client.GetScheduleSlots((string)firstSchedule["id"]);

            var reservations = await client.GetReservations();
            var firstReservation = reservations.First();
            var reservation = await client.GetReservation((string)firstReservation["referenceNumber"]);

            var myReservations = await client.GetReservationsForUser(2);

            client.SignOut().Wait();

        }
    }
}
