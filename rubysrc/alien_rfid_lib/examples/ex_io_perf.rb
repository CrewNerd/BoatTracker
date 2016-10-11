=begin rdoc

=Alien Ruby RFID Library Examples
==ex_io_performance.rb

A little test program to evaluate how long repeated commands take.

Here we read the digital input port a number of times and report the average time for a transaction.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end 

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienblocktimer'
require 'alienconfig'

begin
# grab parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.ini file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

# create a reader
	r = AlienReader.new

	if r.open(ipaddress) # connect and login w/defaults

		puts '----------------------------------'
		puts "Connected to #{r.readername}"

		num_meas = 200

		t = AlienBlockTimer.new 
		t.measure {
			num_meas.times do
				dig_in = r.gpio
			end
		}

		puts "Total time: #{t.elapsed} sec"
		puts "Time per transaction: #{1000*t.elapsed.to_f/num_meas} msec"
		puts '----------------------------------'

		r.close
	end

rescue
	puts $!
end