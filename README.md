# BoatTracker

The goal of this project is to create a powerful and flexible fleet management solution for rowing &amp;
paddling clubs. For too long, boathouses have relied on crude logbooks to track boat usage and
reserve equipment. BoatTracker seeks to modernize club operations through the intelligent use of
technology to benefit both club members and boathouse managers.

To keep the size of the project as small as possible, we sought to leverage existing technology wherever
practical. For example, we use the open-source [BookedScheduler](http://bookedscheduler.com/) system
to provide support for scheduling and reserving resources (i.e. boats). BookedScheduler is a powerful system with many
features that serve our needs well:

* An extensible data model for users, resources, and reservations
* A rich security model that allows typical club policies (e.g. based on membership or skill level) to be implemented easily
* A rich web UI with good mobile support
* A flexible reporting feature to track equipment usage
* A complete REST API

On top of this, the BoatTracker project adds:

* A simple, powerful conversational interface over SMS, FB Messenger, and Skype.
* A "kiosk"-style status view customized for use in the boathouse, showing current activity and upcoming reservations.
* Nightly reports for the boathouse administrator showing daily usage along with any policy violations.
* RFID-based tracking of boats leaving &amp; entering the boathouse (*in progress*)
* [IFTTT](https://ifttt.com) integration for alerting when exceptional conditions arise (*in progress*)

The overall architecture is shown below:

![BoatTracker Architecture](https://github.com/CrewNerd/BoatTracker/raw/master/artwork/BoatTracker.png "BoatTracker Architecture")

BoatTracker is designed to serve multiple clubs through a single service instance. Each club
must maintain its own instance of the BookedScheduler service, but can be served by a common BoatTracker service.
A configuration file provides club-specific information to BoatTracker, including credentials to be used
with the club's BookedScheduler service instance. Caching is used to improve performance and reduce
the load on the BookedScheduler servers.

## BoatTracker Bot

The BoatTracker bot is fully implemented and supports a complete set of commands and queries.
The bot communicates over SMS, FB Messenger, and Skype. The following "intents" are understood
by the bot:

  * Create a reservation
  * Cancel a reservation
  * List reservations
  * Check availability of a boat
  * Take out a boat (with or without a reservation)
  * Return a boat
  * List all boats
  * Change club affiliation (i.e. sign out)
  * Get help

The bot is built using the [Microsoft Bot Framework](https://dev.botframework.com/) and uses "slot-filling" dialogs to gather required
information whenever input from the user appears to be incomplete.

In the example below, the user
is reserving a boat and specifies the boat name, date, time, and duration for the reservation. However,
BoatTracker knows that the named boat is a double, so it asks the user for the name of the other
person who will be rowing.

The bot also provides a flexible approach for naming users and boats. A custom resource extension in BookedScheduler allows
each boat to have a set of "alternate" names, to allow for common spelling errors. For both user and
boat names, the bot will accept any subset of words in the name, provided they are unique within the
set of users or boats for that club. In the example below, the user name "Hanna" is unique, so the
last name is not required.

![Bot Example](https://github.com/CrewNerd/BoatTracker/raw/master/artwork/BotSample.png "Bot Example")

## Club Status view

BoatTracker exposes a simple HTML view showing upcoming reservations, boats currently in use, and any
boats that are overdue to return. Clubs may optionally allow users to check in and check out of
reservations using buttons on the club status page. The page refreshes automatically each minute during
normal club operations, and every 10 minutes otherwise.

The status view is designed to be used with an iPad or Android tablet and one of the many available
"kiosk" applications that lock the tablet to a configured web page. There are many secure cases that
can be used to protect the tablet and prevent theft.

![Status View](https://github.com/CrewNerd/BoatTracker/raw/master/artwork/StatusView.png "Club Status View")

## RFID Tracking

We're currently working on adding support for automatically tracking movement of boats in and out
of the boathouse using RFID tags mounted on the boat and RFID antennas mounted over the bay doors.
Carbon fiber hulls present a challenge due to their electrical properties, but we have found a variety
of RFID tags that work well.

On most boats, the best location to mount the tags will be on the bottom lip of the gunwale, such that
the tags will be facing upward when the boat is carried out of the boathouse upside down. The cost will be
under $4 per boat for the tags, and normally under $2000 for the RFID reader and antennas.

This work is currently in progress.

