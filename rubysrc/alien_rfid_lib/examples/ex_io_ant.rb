=begin rdoc

=Alien Ruby RFID Library Examples
==ex_io_ant.rb

An example program to control the active antennas used by an Alien RFID reader in response to
changes on the Reader's GPIO input lines.

* Connect to an Alien RFID Reader and login
* Grab the Readername and display
* Repeatedly read the digital input port and change the active antenna sequence based on the value.

If no lines are active, turn off automode.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

#This builds an antenna sequence string from a number representing a digital input value.
#Bits (pins) on the input are used to flag active antennas.
def map_input_to_antennas(dig_in)
	antseq = ""
	for bit in 0..@maxantenna do
		antseq << bit.to_s + " " if dig_in[bit] == 1
	end
	return antseq
end

begin
# grab parameters out of a configuration file
	config = AlienConfig.new('config.ini')
	
# change "reader_address" in the config.dat file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

# create our reader 
	r = AlienReader.new
	
	if r.open(ipaddress)
		puts '----------------------------------'
		puts "Connected to #{r.readername}"
		@maxantenna = r.maxantenna.to_i

  # read input and init variables
		dig_in = r.gpio.to_i
		old_dig_in = dig_in

	# spin here forever... (or ctrl-c)
		loop do
			dig_in = r.gpio.to_i

			if dig_in != old_dig_in
				if dig_in == 0
					r.automode = 'off'
				else
					r.antennasequence = map_input_to_antennas(dig_in)
					r.automode = 'on'
				end
			end

			old_dig_in = dig_in
			sleep 1
		end

		puts '----------------------------------'

	# be nice. Close the connection to the reader.
		r.close
	end

rescue
	puts $!
end