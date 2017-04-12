# Unit Tests

The unit test framework is designed to be run against the development deployment
slot, and a test instance of BookedScheduler. The tests communicate with the bot
through the DirectLine API and verify all major areas of the bot.

## Setup

Requirements & assumptions:

* User accounts
    * testuser1
      * Member of the novice group
      * Doesn't own any boats
    * testuser
      * Member of the advanced group
      * Doesn't own any boats
    * testuser3
      * Member of the advanced group
      * Owns a private single (santa maria)
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
      * owned by testuser3
