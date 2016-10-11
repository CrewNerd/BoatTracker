=begin rdoc

=Alien Ruby RFID Library Examples
==ex_error_handling.rb	

Error handling for an application using the Alien RFID Library for Ruby.
Uses the AlienReader.open command with default values for port, username and password. 

(Change the "reader_address" parameter in your config.dat file to the one appropriate for your reader.)

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

# add the default relative library location to the search path
$:.unshift File.join(File.dirname(__FILE__), '..', 'lib')

require 'alienreader'
require 'alienconfig'

begin

# grab various parameters out of a configuration file
  config = AlienConfig.new('config.ini')

# Create the new reader
	r = AlienReader.new
	 
	puts '----------------------------------'
	
	# Open a connection to the reader and get the reader's name.
	if r.open( config.fetch('reader_address', 'localhost')) 
	# We set the .raise_errors value to True. 
	# Reader protocol errors (Where the reader responds with "Error:...") will raise runtime errors. 
		r.raise_errors = true

		puts 'This should raise an error...'
		begin
		# send something the reader doesn't understand... 
			s = r.sendreceive('Foo!')
		  
		# we shouldn't get here...
			puts 'The reader responded with: '
			puts s    
		rescue     
			puts 'Boom! Caught the error! '
			puts $!   
		end

		puts 
		puts 'This should just return the error message...'

	# turn off raise_errors.
		r.raise_errors = false

		begin
		# send something the reader doesn't understand...
			s = r.sendreceive('Foo!')

		# we should get here!
			puts 'The reader responded with: '
			puts s
		rescue
		# we shouldn't get here...
			puts "Boom! the reader raised an error! #{$!}"
		end

		puts
		puts 'Runtime error should occur still...'
		# runtime errors of other types are still raised, though...
		begin
		# this method doesn't exist...
			r.foo = 'Foo!'
		rescue
			puts "Boom! caught the Runtime error! #{$!}"
		end
	end

	puts '----------------------------------'

# close the connection.
	r.close

rescue
# print out any errors
	puts $!
end
