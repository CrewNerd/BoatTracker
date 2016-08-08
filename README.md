# BoatTracker

The goal of this project is to customize the [BookedScheduler](http://bookedscheduler.com/) scheduling
and reservation system to address the problem of reserving &amp; checking out boats in rowing
&amp; paddling clubs.

BookedScheduler version 2.6 (currently in Beta) includes several new features that turn out to be
very helpful for the scenarios we need to address. It also provides a reactive UI that probably
eliminates the need for a dedicated mobile app for rowers and paddlers.

To customize BookedScheduler, we add a few custom attributes to resources to allow boats to be
described more fully. We will also document how to use groups and permissions to implement most
access policies based on skill level or membership type.

Two projects we're exploring involve new coding.

* Using an RFID reader to automatically track boat usage (in addition to the manual check-out
and check-in feature of BookedScheduler).
* A Bot which provides a conversational interface to the scheduling &amp; reservation system.
