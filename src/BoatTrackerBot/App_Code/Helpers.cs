using System;

using BoatTracker.Bot.Configuration;

static public class Helpers
{
    public static string ClubName(string clubId)
    {
        return EnvironmentDefinition.Instance.MapClubIdToClubInfo[clubId].Name;
    }
}