=begin rdoc

=Alien Ruby RFID Library Examples
==tagserver.rb

An example program to serve reader tag data. TagStreamMode and NotifyMode on the reader both
require you to wait for the reader to connect to your service, and feed you tag data. This
application captures this streamed data from the reader and serves it to any TCP sockets that
may have connected to it - hence, a TagServer instead of a TagStreamer. 

* Connect to an Alien RFID reader and login. Set TagStreamAddress/Port and/or NotifyAddress/Port
  to direct reader data to this application.
* Use one thread to continually listen for TCP connections from the reader, and grab any data.
* Use other threads to listen for external TCP connections and accept them.
* Any time tag data is received from the reader, send it to each of the external TCP sockets too.
* If any socket fails to write, immediately close it.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

#Add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alientag'
require 'alienconfig'
require 'thread'

begin
	# grab various parameters out of a configuration file
	config = AlienConfig.new('config.ini')

	# In config.dat, change "reader_address" to the IP address of your reader,
	# or use "localhost" when running the app on-board the reader.
	reader_address  = config.fetch('reader_address', 'localhost')
	reader_port     = config.fetch('reader_port', 2300).to_i # 23=external, 2300=internal
	reader_username = config.fetch('username', 'alien')
	reader_password = config.fetch('password', 'password')
	
	# Direct the reader's TagStream to this app
	stream_address  = config.fetch('tagstream_address', 'localhost')
	stream_port     = config.fetch('tagstream_port', 9999).to_i
	
	# Listen on this port for clients to receive data
	server_port     = config.fetch('tcp_listener_port', 10000).to_i

	# Enable verbose output to stdout
	@verbose = false
	
	# Keep track of all of the connected clients
	@clients = Array.new
	@mutex = Mutex.new # semaphone for multi-thread access to this array
	
	# Connect to the reader and configure TagStreamAddress.
	# You could also direct IOStreamAddress, or NotifyAddress here too.
	begin
		r = AlienReader.new
		if r.open(reader_address, reader_port, reader_username, reader_password)
			if @verbose
				puts '=================================='
				puts "Connected to: #{r.readername}"
				r.tagstreamaddress = "#{stream_address}:#{stream_port}"
				puts 'Reader configured.'
			end
		end
	rescue
		puts $!
		# OK if this fails; reader may be already configured. We can still listen for streamed reader data.
	ensure
		r.close
	end

	# Handle incoming data from the reader on a separate thread.
	reader_thread = Thread.start {
		reader_listener = TCPServer.new(stream_port)
		loop {
			# Accept each connection in a separate thread.
			# This lets us serve separate TagStream, IOStream, and even Notifications.
			Thread.start(reader_listener.accept) do |session|
				puts '++++++++ Reader Listen Thread +++++++++' if @verbose
				data = "" # read one byte at a time into this variable
	
				# Read until we receive a \0 (end of message) or nothing (socket closed).
				loop do
					char = session.recv(1)      # read one character
					break if char.length == 0   # socket closed
					data << char                # append character to "data"
					if char == "\0"             # end of message
						if @verbose
							puts '============== New  Data =============='
							puts data.strip
							puts '-------------- Send Data --------------'
						end
						
						# Tell each connected client about the new data!
						# Iterate over a deep copy of the clients array, so we can remove inactive clients as we go.
						@mutex.synchronize {
							clients_temp = @clients.dup
							clients_temp.each do |client|
								begin
									puts "Sending data to #{client}" if @verbose
									client.send(data, 0)
								rescue
									# Failure to write usually means the client disconnected.
									puts "#{$!} - Client #{client} disconnected?" if @verbose
									
									# Remove the client socket from the clients array.
									@clients.delete(client)
								end
							end # each client
						}
						
						data = ""
					end # char == \0?
				end # read data loop
				
				# If we get here, the reader likely has closed the socket.
				puts '(reader closed the socket)' if @verbose
			end # reader accept thread
		} # reader listen loop
	} # reader listen thread
	
	# Listen for incoming client connections.
	server = TCPServer.new(server_port)
	loop {
	  puts "Waiting for clients to connect on port #{server_port}..." if @verbose
	  
	  # When clients connect, accept it and then just add the client to the clients array.
	  client = server.accept
	  @mutex.synchronize {
	    @clients << client
		  (client_domain, client_port, client_hostname, client_ip) = client.peeraddr
  	  puts "Client connected from #{client_ip}:#{client_port} (#{client_hostname})" if @verbose
	  }
	}
rescue 
	puts $!
end