# Unit Tests

The unit test framework is designed to be run against the development deployment
slot, and a test instance of BookedScheduler. The tests communicate with the bot
through the DirectLine API and verify all major areas of the bot.

## Setup

Requirements & assumptions:

* Club setup
  * Club hours 00:15AM - 11:14PM
  * Minimum duration: 30 minutes
  * Maximum duration: 4 hours
  * NOTE: some unit tests will not work from 11:15pm to 12:15am each day
* User accounts
    * testuser1
      * Member of the novice group
      * Doesn't own any boats
    * testuser2
      * Member of the advanced group
      * Doesn't own any boats
    * testuser3
      * Member of the advanced group
      * Co-owns a private single (nina)
    * testuser4
      * Member of the advanced group
      * Owns a private single (shadowfax)
      * Co-owns a private single (nina)
* Groups
    * Novice
    * Advanced - has access to all club boats
* Resources
    * pinta
      * alternate names: pinto, pinte
      * club 1x
      * novice, advanced
    * santa maria
      * club 2x
      * advanced
    * nina
      * private 1x
      * owned by testuser3 & testuser4
    * shadowfax
      * private 1x
      * owned by testuser4
    * lemon
      * club 1x
      * novice, advanced
      * boat status is "unavailable"