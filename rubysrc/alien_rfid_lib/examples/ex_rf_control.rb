=begin rdoc
=Alien Ruby RFID Library Examples
==ex_rf_control.rb 

Sometimes controlling the details of the way the reader broadcasts commands to the tags can be
important for optimum performance (particularly in multi-reader environments). Here we show how to
control the output power of the reader, the antennas used, and the RF modulation scheme.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

#Add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

begin
# grab parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.ini file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

# make a reader
	r = AlienReader.new

# connect to the reader
	if r.open(ipaddress)

		puts '----------------------------------'
		puts "Reader Model: #{r.readertype}"
		puts "Reader Name:  #{r.readername}"

	# the readers have a function to detect which ports have an antenna connected. 
		available_antennas = r.antennastatus
		puts "Connected antenna ports: #{available_antennas}" 

		old_rf_level = r.rflevel

	# our reader returns power in dBm *10 
		puts "Current RF Power: #{old_rf_level.to_f / 10}dBm"

	# grab the current antenna configuration
		old_antenna_sequence = r.antennasequence	
		puts "Current Antenna Sequence: #{old_antenna_sequence}" 

	# set the reader to use all the connected antennas
		puts 'Change antenna sequence to all available antennas...' 
		r.antennasequence = available_antennas
		puts "New antenna sequence: #{r.antennasequence}"

		puts 'Try to change power to 30.0dBm...' 
		r.rflevel = 300
		puts "RF Level: #{r.rflevel.to_f / 10}dBm"

		puts 'Set RF modulation mode to Dense Reader Mode...'
		r.rfmodulation = 'drm' 
		puts "RF Modulation: #{r.rfmodulation}"

		puts '----------------------------------'
		
	# be nice. Close the connection.
		r.close
	end
rescue
	puts $!
end
