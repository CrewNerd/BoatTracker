using System;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;

namespace BoatTracker.Bot.Controllers
{
    public class DebugWarmUpController : Controller
    {
        public HttpResponseMessage Index()
        {
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
