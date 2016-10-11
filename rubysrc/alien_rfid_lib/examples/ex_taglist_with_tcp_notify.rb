=begin rdoc

=Alien Ruby RFID Library Examples
==ex_taglist_with_tcp_notify.rb

An example program to play with taglists and notifications using a TCP connection. 

* Connect to an Alien RFID reader. Login.
* Grab some tag data.
* Scan the data for interesting tags and display the results.
* Send the interesting data to someone else over a TCP socket connection.
 
Use ex_tcp_listener.rb as the destination. 
First start ex_tcp_listener and then run this application. 
(both applicatons should be running on the same machine)

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alientag'
require 'alienconfig'
require 'socket'

# Send a TCP message to a server running at an IP address on a particular port.
def notify (msg)
	ipaddress = @config['tcp_listener_address']
	port = @config['tcp_listener_port']

	begin
		sock = TCPSocket.new(ipaddress, port)
		sock.puts msg    # format the array nicely
		sock.write "\0"  # end each message with a null character
		sock.close
	rescue 
		puts $!
	end
end


begin
# grab various parameters out of a configuration file
	@config = AlienConfig.new('config.ini')

# change "reader_address" in the config.ini file to the IP address of your reader.
	ipaddress = @config.fetch('reader_address', 'localhost')

# create a reader. 
	r = AlienReader.new

# use your reader's IP address here.
	if r.open(ipaddress)
		puts '----------------------------------'
		puts "Connected to: #{r.readername}"
		puts

	# construct a taglist from the reader's tag list string
		tl = AlienTagList.new(r.taglist)

	# how many tags did we find?
		puts "Number of tags found: #{tl.length}"
		puts tl
		puts
		
	# sort your list to make reading easier. 
	# (the comparison operator <=>, used by sort, is part of the Tag class)
		tl.sort!

	# did we find a particular tag(s)? You can use a regular expression to check if
	# elements in the list are tags that match what you are interested in.
		puts 'Tag List Matches:'
		filtered = tl.filter(/0000/) # keep only tags containing "0000"
		puts filtered
		
	# tell someone about what we saw! 
		notify(filtered)
		
		puts '----------------------------------'
		
	# be nice. Close the connection to the reader.
		r.close
	end
rescue 
  puts $!
end