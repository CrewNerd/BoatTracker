=begin rdoc

=Alien Ruby RFID Library Examples
==ex_udp_listener.rb	

This is a simple server that accepts connections on a UDP socket and prints out 
data that comes in on the connection.

An entire packet of data, up to 4096 bytes is received and then printed to the screen.

Use this example in conjunction with ex_taglist_with_UDP_notify.rb. First run this program on your
local machine and then run ex_taglist_with_udp_notify on the same machine to see notification
messages. Or you can simply listen on port 3988 to detect reader heartbeat messages.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienconfig'
require 'socket'

begin
# grab various parameters out of a configuration file
	config = AlienConfig.new('config.ini')
	port = config['udp_listener_port']

	puts '-----------------------------------'
	puts 'UDP Listener Active (ctrl-c = exit)'
	puts '-----------------------------------'

	server = UDPSocket.open
	server.bind('', port) # first param is hostname, second is port.

# spin forever...
	loop do
	# wait for data...
		s = server.recvfrom(4096)
		puts s[0] # other elements of the result array include ip address & port info
		puts '-----------------------------------'
	end
rescue 
  puts $!
end