=begin rdoc

=Alien Ruby RFID Library Examples
==ex_command_line_interface.rb

Here we create our own command-line interface to the reader, similar to what you'd find if you used
telnet. The sendreceive method may be found in the AlienConnection class AlienReader inherits from.

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

# create the new reader
	r = AlienReader.new

	puts '----------------------------------'

# open a connection to the reader and display the reader's name.
	if r.open(ipaddress)
		print "Connected to: #{r.readername}.\r\n"

		# raise_errors=false causes the reader class not to raise a runtime error when the 
		# reader returns "Error:...", but instead just passes the message along to us.
		r.raise_errors = false

		while r.connected # loops until you enter 'q' to close the connection
			print 'Ruby CLI>'
			cmd = gets.strip

			puts r.sendreceive(cmd)
			puts
		end
	end

	puts '----------------------------------'

# close the connection.
	r.close

rescue
# print out any errors at the top level
	puts $!
end
