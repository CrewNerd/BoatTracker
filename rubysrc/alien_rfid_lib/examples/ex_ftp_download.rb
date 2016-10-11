=begin rdoc

=Alien Ruby RFID Library Examples
==ex_ftp_download.rb

This is more of a Ruby example than an RFID example. --

We'll use the net/ftp library to go to the Alien ftp server and pull down the 
latest version of reader firmware for the ALR-9900 reader.

Copyright 2014, Alien Technology Corporation. All rights reserved.

=end

require 'net/ftp'

begin
	puts '----------------------------------'

	servername = 'ftp.alientechnology.com'
	username   = 'anonymous'
	password   = ''
	serverdir  = 'pub/readers/alr9900/firmware/130913'
	
	ftp = Net::FTP.new(servername)
	ftp.login(username, password)
	ftp.chdir(serverdir)

	files = ftp.nlst
	puts 'Files in FTP folder:'
	puts files

# create a directory (if needed) for the files
	targdir = 'ftp_downloads'

	if !Dir.entries('.').include?(targdir)
	  Dir.mkdir(targdir)
	end

	Dir.chdir(targdir)

	puts 'Copying files...'

	files.each do |item| 
	  ftp.getbinaryfile(item)
	  puts '...'	
	end

	ftp.close	

	puts 'Done. Downloaded files: '

# show what we copied
	puts Dir.entries('.')

	Dir.chdir('..')

	puts '----------------------------------'

rescue
  puts $!
end
