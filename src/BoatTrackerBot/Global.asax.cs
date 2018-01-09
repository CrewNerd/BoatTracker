using System;
using System.Configuration;
using System.Web;
using System.Web.Http;
using System.Web.Optimization;
using System.Web.Routing;

using Autofac;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;

using BoatTracker.Bot.Utils;
using BoatTracker.Bot.Configuration;

namespace BoatTracker.Bot
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            var store = new TableBotDataStore(
                EnvironmentDefinition.Instance.StorageConnectionString,
                EnvironmentDefinition.Instance.BotStateTableName);

            Conversation.UpdateContainer(
                builder =>
                {
                    builder.Register(c => store)
                             .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                             .AsSelf()
                             .SingleInstance();

                    builder.Register(c => new CachingBotDataStore(store,
                              CachingBotDataStoreConsistencyPolicy
                              .ETagBasedConsistency))
                              .As<IBotDataStore<BotData>>()
                              .AsSelf()
                              .InstancePerLifetimeScope();
                }
            );

            //
            // Initialize our cache of BookedScheduler data before the server starts.
            // This includes users, resources, and groups which change very slowly
            // compared to reservations. We use a webjob to request a cache refresh
            // every two hours, but the service will also attempt to refresh the cache
            // at a slower rate if the webjob fails for some reason.
            //
            BookedSchedulerCache.Instance.Initialize();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
