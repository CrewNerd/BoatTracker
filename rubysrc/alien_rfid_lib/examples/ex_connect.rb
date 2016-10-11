=begin rdoc

=Alien Ruby RFID Library Examples
==ex_connect.rb	

Once you have a reader's IP address, you can to connect to it. This example shows how to make a
connection to a reader, login, and query the reader for basic information.

Uses the AlienReader.open command with default values for port, username and password. 

Change the "reader_address" parameter in your config.dat file to the one appropriate for your reader.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

begin
# grab various parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.ini file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost')

	r = AlienReader.new

	puts '----------------------------------'

	if r.open(ipaddress)
		puts 'Hello World!'
		puts "My name is: #{r.readername}."
		puts "I am an:  #{r.readertype}"
	end

	puts '----------------------------------'

	# be nice. Close the connection to the reader.
	r.close
rescue
	puts $!
end
