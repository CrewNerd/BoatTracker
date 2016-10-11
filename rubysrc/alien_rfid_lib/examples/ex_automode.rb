=begin rdoc

=Alien Ruby RFID Library Examples
==ex_automode.rb

The reader supports a simple state machine to control when it reads tags and 
reports data to hosts. When "automode" is "on", the state machine is active. 
Using Automode is the best way to ensure reliable, low-latency tag reads. 
This example shows how to set up automode for continuous reading for three seconds.

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
	puts ipaddress
# create our reader 
	r = AlienReader.new

	if r.open(ipaddress)    
		puts '----------------------------------'
		puts "Connected to #{r.readername}"	
		puts "Automode is currently #{r.automode}"
		r.automodereset # reset to the default automode settings (no triggers, no delays, etc.)
		r.automode = 'on'
		puts 'Reading for 3 seconds...'	
		sleep (3)		
		puts '...Done!'
		puts 'Tags Found:'
		puts r.taglist
		r.automode = 'off'
		puts '----------------------------------'
		
	# be nice. Close the connection to the reader.
		r.close
	end
rescue
	puts $!
end
