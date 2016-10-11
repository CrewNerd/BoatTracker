=begin rdoc

=Alien Ruby RFID Library Examples
==ex_tcp_listener.rb	

This is a simple server that accepts connections on a socket and prints out 
data that comes in on the connection.

Messages are assumed to be null-terminated strings. On receiving a null, 
the server prints out the data and continues reading. When nothing is read,
it is assumed the reader closed down it's side of the socket.

Use this example in conjunction with ex_taglist_with_notify.rb. First run
this program on your local machine and then run ex_taglist_with_notify.rb on 
the same machine to see notification messages.

Alternatively, you can manually configure your reader to stream tag or I/O
data, or send notifications, to this program.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib') 

require 'socket'
require 'alienconfig'

begin
# grab various parameters out of a configuration file
	config = AlienConfig.new('config.ini')
	tcp_listener_address = config['tcp_listener_address']
	tcp_listener_port    = config['tcp_listener_port'].to_i

	server = TCPServer.new(tcp_listener_address, tcp_listener_port)

	# Continually accept new connections
	loop do
		# Accept each connection in a separate thread
		Thread.start(server.accept) do |session|
			data = ""
			puts '---------------------------------------'

			# Read until a \0 (end of message) or nothing (socket closed)
			loop do
				char = session.recv(1)      # read one character
				break if char.length == 0   # socket closed
				data << char                # append character
				if char == "\0"             # end of message
					puts data.strip
					puts '---------------------------------------'
					data = ""
				end
			end
			puts '(reader closed the socket)'
		end
	end

rescue 
	puts $!
end