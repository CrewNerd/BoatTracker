=begin rdoc

=Alien Ruby RFID Library Examples
==ex_get_taglist.rb

An example program to play with taglists. 

* Connect to an Alien RFID reader. Login.
* Grab some tag data.
* Scan the data for interesting tags and display the results.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

#Add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alientag'
require 'alienconfig'


begin
# grab various parameters out of a configuration file
	config = AlienConfig.new('config.ini')

# change "reader_address" in the config.ini file to the IP address of your reader.
	ipaddress = config.fetch('reader_address', 'localhost') 

# create a reader
	r = AlienReader.new

# use your reader's IP address here.
	if r.open(ipaddress)
		puts '----------------------------------'
		puts "Connected to: #{r.readername}"

	# construct a taglist from the reader's tag list string
	# Note: if automode is running this will contain the latest tags. --If not,
	# the reader will read tags and then return the data.
		tl = AlienTagList.new(r.taglist)

	# how many tags did we find?
		puts
		puts "Number of tags found: #{tl.length}"

	# sort your list to make reading easier. 
	# (The comparison operator <=>, used by sort, is part of the Tag class in alientag.rb)
		tl.sort!
		puts tl

	# did we find a particular tag(s)? You can use a regular expression to check if
	# elements in the list are tags that match what you are interested in.
		puts
		puts 'Tag List Matches:'
		puts tl.filter(/.*/) # use a regex to filter specific tags
		puts '----------------------------------'

	# be nice. Close the connection.
		r.close
	end
rescue 
	puts $!
end
