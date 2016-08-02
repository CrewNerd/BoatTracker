using System;
using System.Diagnostics;
using System.Web.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

#if DEBUG
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
#endif
        }
    }
}
