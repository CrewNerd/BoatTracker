using System;
using System.Net;
using System.Net.Http;
using System.Web.Mvc;

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
