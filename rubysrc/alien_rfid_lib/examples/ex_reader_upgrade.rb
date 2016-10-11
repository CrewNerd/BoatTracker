=begin rdoc

=Alien Ruby RFID Library Examples
==ex_reader_upgrade.rb	

Extend the "Hello World" application using the Alien RFID Library for Ruby to upgrade the reader.

Uses the AlienReader.open command with default values for port, username and password. 

(Change the "reader_address" parameter in your config.dat file to the one 
appropriate for your reader and the "reader_upgrade_address" to the location of 
the upgrade server.)

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

begin
# grab various parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.dat file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

# create the new reader
	r = AlienReader.new

	puts '----------------------------------'

# open a connection to the reader and get the reader's name.
	if r.open(ipaddress)
		puts "Hello World! I am #{r.readername}."
		r.upgradeaddress = config['reader_upgrade_address']
		puts r.upgradeaddress
		puts r.upgradenow
	end

	puts '----------------------------------'
	
# close the connection.
	r.close
rescue
# print out any errors
	STDERR.puts $!
end