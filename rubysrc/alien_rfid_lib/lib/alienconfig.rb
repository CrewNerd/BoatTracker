=begin rdoc
=Alien Ruby RFID Library 
==alienconfig.rb	
  A class to load/save application configuration data in a simple text file.
  Minimal error handling.
  Configuration parameters are maintained as a hash.
  Copyright 2014, Alien Technology Corporation. All rights reserved.
=end

class AlienConfig < Hash

	def initialize(fn, symbolsForKeys=false)
		super(nil)
		@symbolsForKeys = symbolsForKeys
		load(fn)
	end
	
#	private
# Open a file and read out the configuration parameters. Save into a hash structure.
# Blank lines and comment lines, beginning with '#' are ignored.
	def load(fn)
		if File.file?(fn)
			begin
			File.open(fn).each do |line|
			#peel off terminators/leading spaces, etc.
				line.strip!
				
			#ignore comment lines...
				if (line[0..0]!="#")
					keyval = line.split("=") # split on equal sign

				#ignore blank lines
					if keyval.size>0
						key   = keyval[0].strip
						value = keyval[1].nil? ? "" : keyval[1].strip
						
						if (@symbolsForKeys)
							self[key.intern] = value
						else
							self[key] = value;
						end
					end
				end
			end
			rescue
				raise "Error: trouble loading data from file: #{fn}.\nDetails: #{$!}"
			end
		else
			raise "Error: cannot find configuration file: #{fn}.\nDetails: File not found."
		end
	end

	#Save the hash data to a file.
	def save(fn)
		begin
			File.open(fn, "w") do |file|
				self.each do |key, value|
					file.print key.to_s + "=" + value.to_s + "\r\n"
				end
			end
		rescue
			raise "Error: trouble saving configuration file: #{fn}.\nDetails: #{$!}"
		end
	end
end
