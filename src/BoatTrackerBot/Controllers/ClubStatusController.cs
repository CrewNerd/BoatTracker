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
        /// <summary>
        /// Gets the club status page. The clubid specifies the club to display. The checkin and
        /// checkout buttons direct here with the corresponding query parameter containing the
        /// reference id for the reservation of interest.
        /// </summary>
        /// <param name="clubId">Required parameter specifying the id of the club to show</param>
        /// <param name="checkin">If present, the reference id of a reservation to check in</param>
        /// <param name="checkout">If present, the reference id of a reservation to check out</param>
        /// <returns>The club status page</returns>
        public async Task<ActionResult> Index(
            [FromUri] string clubId,
            [FromUri] string checkin = null,
            [FromUri] string checkout = null)
        {
            if (string.IsNullOrEmpty(clubId) || !EnvironmentDefinition.Instance.MapClubIdToClubInfo.ContainsKey(clubId))
            {
                return this.HttpNotFound("Missing or unknown club id");
            }

            // TODO: Add a simple security check here using a shared secret set up in the club configuration

            ClubStatus model = new ClubStatus(clubId);

            // If the return value is non-null, it's an error message from the checkin or checkout
            // and we display it as an alert at the top of the page.
            ViewBag.Message = await model.LoadDataAsync(checkin, checkout);

            return View("ClubStatus", model);
        }
    }
}