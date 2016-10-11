=begin rdoc

=Alien Ruby RFID Library Examples
==ex_discovery.rb

You can use the reader's UDP Heartbeat Message to discover readers on your LAN. 
This program listens for a heartbeat, parses the XML data therein and extracts information about the
reader. Using a technique like this, you can determine the IP address of each active reader on your
LAN. This is handy when your readers are using DHCP and you don't know what IP address they get!

Copyright 2014, Alien Technology Corporation. All rights reserved.
=end

require 'socket'
require 'rexml/document'

begin
# default port for Alien RFID Reader UDP Heartbeat messages
	$port = 3988

	udp_listener = UDPSocket.open
	udp_listener.bind('', $port)
	
	puts 'Listening for heartbeats. It may take up to 30 seconds...'
  
# listen for a packet of information on the UDP port.
	xml_data= udp_listener.recvfrom(1024) [0]
	puts
	puts 'Reader heartbeat message received:'
	puts '----------------------------------'
	print xml_data 
	puts '----------------------------------'
	puts

# extract the heartbeat from the XML packet by populating an XML class
	doc = REXML::Document.new(xml_data)

# print out the individual XML-tagged elements.
	puts 'Elements of the XML message:'
	puts '----------------------------------'
	doc.elements.each('Alien-RFID-Reader-Heartbeat/*') do |ele|
		puts "#{ele.name}	=> #{ele.text}"
	end
	puts '----------------------------------'
	puts

# you can grab an element out of the elements array directly by treating it as a hash...
	this_reader  = doc.elements['Alien-RFID-Reader-Heartbeat/IPAddress'].text
	this_version = doc.elements['Alien-RFID-Reader-Heartbeat/ReaderVersion'].text

	puts 'Values extracted via hash lookup:'
	puts '----------------------------------'
	puts "Reader IP  : #{this_reader}"
	puts "FW Version : #{this_version}"
	puts '----------------------------------'
	puts
	puts 'Exiting program...'
rescue
	puts $!
end
