using System;
using System.Globalization;
using System.Linq;
using Microsoft.Azure;

namespace BoatTracker.Bot.Configuration
{
    /// <summary>
    /// Represents the configuration necessary to operate in an environment.
    /// </summary>
    /// <remarks>
    /// This class should only contain members that differ from environment to environment or might differ in the
    /// future.
    /// </remarks>
    public abstract class EnvironmentDefinition
    {
        /// <summary>
        /// Gets the expiration time for pending trigger items
        /// </summary>
        public abstract TimeSpan TriggerExpirationTime
        {
            get;
        }

        /// <summary>
        /// Create an environment definition from the environment environment variable.
        /// </summary>
        /// <returns>The created environment definition.</returns>
        public static EnvironmentDefinition CreateFromEnvironment()
        {
            const string HostnameEnvVar = "WEBSITE_HOSTNAME";

            var hostName = Environment.GetEnvironmentVariable(HostnameEnvVar).Split('.').FirstOrDefault();

            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentException("Current Environment could not be determined");
            }

#if true
            return new ProductionEnvironmentDefinition();
#else
            switch (hostName.ToUpperInvariant())
            {
                case "MSBANDIFTTTCHANNEL-DEV":
                case "MSBANDIFTTTMOBILE-DEV":
                    return new DevelopmentEnvironmentDefinition();

                case "MSBANDIFTTTCHANNEL-STG":
                case "MSBANDIFTTTMOBILE-STG":
                    return new StagingEnvironmentDefinition();

                case "MSBANDIFTTTCHANNEL":
                case "MSBANDIFTTTMOBILE":
                    return new ProductionEnvironmentDefinition();

                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Environment {0} does not have a corresponding Environment definition defined",
                            hostName));
            }
#endif
        }
    }
}