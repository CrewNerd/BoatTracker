=begin rdoc
=Alien Ruby RFID Library 
==alientaglist.rb	

A simple class to hold an array of taglist data elements.

(The format of strings handled by the build_tag method is assumed to be compatible with Alien's 'text' taglist format.)

Copyright 2008, Alien Technology Corporation. All rights reserved.
=end

#Takes a string returned from a Taglist function call and builds an array of tags.

class AlienTagList < Array
	
	def initialize(taglist_string="")
		super()
		string_to_taglist(taglist_string) if taglist_string != ""
	end
	

	# Takes a taglist string from a reader and appends it to the array.
	def string_to_taglist(taglist_string)
		lines = taglist_string.split("\r\n")
		
		lines.each do |line|
			if line != "(No Tags)"
				add_tag(AlienTag.new(line))
			end
		end
		
		return self
	end
	
	# Adds an AlienTag to the list.
	def add_tag(t)
		self.push(t)
		return self  
	end
	
	# A little regular expression scanner. Looks at the list of tags and returns a
	# new taglist containing those tag IDs that match a regular expression filter.
	def filter(filter)
		tl = AlienTagList.new
		
		self.each do |ele|
			if ele.tag =~ filter 
				tl.add_tag(ele)
			end
		end
		
		return tl
	end
	
	# A self-modifying version of filter_taglist.
	# Excercise caution. Elements in the taglist array that do not match
	# the regular expression are deleted.
	def filter!(filter)
		self.delete_if { |ele| !(ele.tag =~ filter)}
		return self
	end

end #class AlienTagList