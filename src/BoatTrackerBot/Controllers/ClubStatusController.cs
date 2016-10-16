using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Models;

namespace BoatTracker.Bot
{
    public class ClubStatusController : Controller
    {
        // GET: ClubStatus
        public async Task<ActionResult> Index([FromUri] string clubId)
        {
            if (string.IsNullOrEmpty(clubId) || !EnvironmentDefinition.Instance.MapClubIdToClubInfo.ContainsKey(clubId))
            {
                return this.HttpNotFound("Missing or unknown club id");
            }

            // TODO: Add a simple security check here using a shared secret set up in the club configuration

            ClubStatus model = new ClubStatus(clubId);
            await model.LoadDataAsync();

            return View("ClubStatus", model);
        }
    }
}