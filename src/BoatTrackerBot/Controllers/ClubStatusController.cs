using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

using BoatTracker.Bot.Configuration;
using BoatTracker.Bot.Models;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace BoatTracker.Bot
{
    public class ClubStatusController : Controller
    {
        private TelemetryClient telemetryClient;

        private TelemetryClient TelemetryClient
        {
            get
            {
                if (this.telemetryClient == null)
                {
                    this.telemetryClient = new TelemetryClient();
                }

                return this.telemetryClient;
            }
        }

        /// <summary>
        /// Gets the club status page. The club id specifies the club to display. The checkin and
        /// checkout buttons direct here with the corresponding query parameter containing the
        /// reference id for the reservation of interest.
        /// </summary>
        /// <param name="clubId">Required parameter specifying the id of the club to show</param>
        /// <param name="clubStatusSecret">If a club status secret is configured, it must be provided here</param>
        /// <param name="checkin">If present, the reference id of a reservation to check in</param>
        /// <param name="checkout">If present, the reference id of a reservation to check out</param>
        /// <returns>The club status page</returns>
        public async Task<ActionResult> Index(
            [FromUri] string clubId,
            [FromUri] string clubStatusSecret = null,
            [FromUri] string checkin = null,
            [FromUri] string checkout = null)
        {
            if (string.IsNullOrEmpty(clubId) || !EnvironmentDefinition.Instance.MapClubIdToClubInfo.ContainsKey(clubId))
            {
                return this.HttpNotFound("Missing or unknown club id");
            }

            ClubStatus model = new ClubStatus(clubId);

            model.IsKiosk = clubStatusSecret != null && clubStatusSecret == model.ClubInfo.ClubStatusSecret; 

            // If the return value is non-null, it's an error message from the checkin or checkout
            // and we display it as an alert at the top of the page.
            ViewBag.Message = await model.LoadDataAsync(checkin, checkout);

            var viewResult = View("ClubStatus", model);

            // Send the telemetry as late as possible to get more accurate page load times.
            var pageViewTelemetry = new PageViewTelemetry("clubStatus");
            pageViewTelemetry.Properties["clubId"] = clubId;
            pageViewTelemetry.Properties["isCheckIn"] = (checkin != null).ToString();
            pageViewTelemetry.Properties["isCheckOut"] = (checkout != null).ToString();
            pageViewTelemetry.Properties["isKiosk"] = model.IsKiosk.ToString();

            this.TelemetryClient.TrackPageView(pageViewTelemetry);

            return viewResult;
        }
    }
}