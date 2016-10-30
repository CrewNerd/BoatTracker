using System;
using System.Web;
using System.Web.Http;
using System.Web.Optimization;
using System.Web.Routing;

using BoatTracker.Bot.Utils;

namespace BoatTracker.Bot
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
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
