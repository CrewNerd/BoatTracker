=begin rdoc

=Alien Ruby RFID Library Examples
==ex_io.rb

Alien RFID reader have multiple GPIO ports -- some for input and others for output. 
This example shows how to read data from and write data to the ports.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

#Add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

begin
# grab parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.dat file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

# create our reader 
	r = AlienReader.new

# connect to the reader
	if r.open(ipaddress)
		puts '----------------------------------'
		puts 'Connected to ' + r.readername	

	#read input and init variables
		dig_in     = r.gpio.to_i
		old_dig_in = dig_in

		puts "Initial input state: #{dig_in}"
		r.gpio = dig_in # set the output to match the input

	# spin here until the input changes to zero.	
		loop do		
		  dig_in = r.gpio.to_i

			if dig_in != old_dig_in
				puts "Digital input Changed to: #{dig_in}"
				old_dig_in = dig_in
				r.gpio = dig_in # set the output to match the input
			end
	
			# bail out when the inputs are all "off"
			break if dig_in == 0

			# wait a bit before going around again...
			sleep 0.25
		end

		puts '----------------------------------'	
		puts 'Input changed to zero! We are done.'

	# be nice. Close the connection to the reader.
		r.close
	end
rescue
	puts $!
end
