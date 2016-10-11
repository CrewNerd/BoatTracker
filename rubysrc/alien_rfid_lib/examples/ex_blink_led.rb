=begin rdoc

=Alien Ruby RFID Library Examples
==ex_blink_led.rb

The LEDS on the reader may be flashed using the reader.blink_led command. 
This can be useful for diagnostics when a display is not available.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

begin
# grab parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.ini file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

# create our reader 
	r = AlienReader.new

  if r.open(ipaddress)
		puts '----------------------------------'
		puts "Connected to #{r.readername}"	

		state1   = 0    # all LEDs off
		state2   = 1    # Just LED #1
		duration = 100  # milliseconds between state changes
		count    = 5    # how many times to switch states

		puts 'LED Sweep...'
		while state2 < 255
			r.blinkled(state1, state2, duration, count)
			state2 <<= 1  # Shift the bit left --> light up next LED
		end

		puts 'LED Flash...'
		state1 = 0
		state2 = 255
		count  = 10
		r.blinkled(state1, state2, duration, count)

		puts 'LED Dance...'
		state1 = 85
		state2 = 170
		r.blinkled(state1, state2, duration, count)
		puts '----------------------------------'

	# be nice. Close the connection to the reader.
		r.close
  end
rescue
	puts $!
end
