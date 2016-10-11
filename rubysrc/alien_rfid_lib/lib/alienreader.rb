=begin rdoc
=Alien Ruby RFID Library
==alienreader.rb
  AlienReader is a fairly lightweight class that inherits from AlienConnection. It provides methods that map attributes to reader functions like readername, readertype, etc, and exposes the execute method to send/receive an arbitrary command to the reader.
  This class uses a metaprogramming hack to dynamically build methods for many of the simple reader attributes from a file. This is a pretty flexible way to create the class and avoid lots of repetitive coding.
  Copyright 2015, Alien Technology Corporation. All rights reserved.
=end

require 'alienconnection'
require 'alientaglist'

class AlienReader < AlienConnection
  # The release version of this API
  @@apiVersion = 1.1

	# Make sure we only load methods once for this class
  @@methodsNeedLoading = true

  #call the AlienConnection initializer and build lots of the Getters/Setters from a reader methods file. Default value for the methods file will be the file, readermethods.txt found in the same directory as this file, alienreader.rb.
  def initialize(methodsfile=File.join(File.dirname(__FILE__) , "readermethods.dat"))
    super()

    # look for a local copy of readermethods.dat first, otherwise, use the default
    methodsfile = "readermethods.dat" if (File.exists?("readermethods.dat"))

    if @@methodsNeedLoading
      build_methods(methodsfile)
    end
  end

  #execute a sendreceive and pull off the 'y' payload for messages that
  #return with an 'x = y' style reply
	private
	def execute (cmd)
    s = sendreceive(cmd)
    if s.include? '='
      return s.split('=')[1].strip
    else
      return s
    end
  end

  def add_get(cmd)
    method_name = cmd.to_sym
    self.class.instance_eval {
      send :define_method, method_name do |*val|
      # 'to_s' on an array of strings is Ruby version dependent, hence use 'join':
      #   Ruby 1.8 it is 'join'   : [1,2,3,4].to_s => "1234"
      #   Ruby 1.9 it is 'inspect': [1,2,3,4].to_s => "[1, 2, 3, 4]"
        if val.nil? || val.join.strip.size==0
          execute("#{cmd}?")
        else
      # #{val} is Rub y version dependent, hence use 'join':
      #   Ruby 1.8: "abc"
      #   Ruby 1.9  "[abc]"
          execute("#{cmd} #{val.join(" ")}?")
        end
      end
    }
  end

  def add_set(cmd)
    method_name = (cmd+"=").to_sym
    self.class.instance_eval {
      send :define_method, method_name do |*val|
        execute("#{cmd}=#{val.join(" ")}")
      end
    }
  end

  def add_do(cmd)
    method_name = cmd.to_sym
    self.class.instance_eval {
      send :define_method, method_name do |*val|
        if val.nil? || val.join.strip.size==0
        	sendreceive("#{cmd}")
        else
          sendreceive("#{cmd} #{val.join(" ")}")
        end
      end
    }
  end

  def add_do_set(cmd)
    method_name = cmd.to_sym
    self.class.instance_eval {
      send :define_method, method_name do |*val|
        if val.nil? || val.join.strip.size==0
          execute("#{cmd}")
        else
          execute("#{cmd}=#{val.join(" ")}")
        end
      end
    }
  end

  def add_getset(cmd)
    add_get(cmd)
    add_set(cmd)
  end

#Build lots of the simple get/set methods supported by the class from a configuration file...
  private
  def build_methods(fn)
    #puts "building methods from: #{fn}"
    #open the file and read out the methods to be supported
    if File.file?(fn)
      File.open(fn).each do |line|
        #puts line
        #ignore comment lines...
        if (line[0..0]!="#")
          dat = line.split

          #ignore blank lines
          if dat.size>0
            method_name = dat[0].strip
            use = dat[1].strip

            #puts "Method: #{dat[0]} Use: #{dat[1]}"
            case dat[1].downcase
              when "getset"
                add_getset(dat[0])
              when "get"
                add_get(dat[0])
              when "set"
                add_set(dat[0])
              when "do"
                add_do(dat[0])
              when "doset"
                add_do_set(dat[0])
            end
          end
        end
      end
      @@methodsNeedLoading = false  # don't load methods for this class again
    else
      raise "Error: cannot find method definition file: #{fn}."
    end
  end

  public
# Makes the reader blink its LEDs.
# _blinkled_ is a more complicated function call than an attribute.
# We just put the method in here rather than try to generate it dynamically.
  def blinkled (state1, state2, duration, count)
    execute('blinkled=' + state1.to_s  + ' ' +  state2.to_s + ' ' + duration.to_s + ' ' +count.to_s)
  end

# Returns the state of the reader's external inputs
  def gpio
    execute('externalinput?')
  end

# Sets reader's external outputs
  def gpio=(newvalue)
    execute('externaloutput='+newvalue.to_s)
  end

# Executes the Alien _UpgradeNow_ command. The _opts_ parameter correspondst to the standard
# values passed to the _UpgradeNow_ command. For example
#  rdr.upgradenow("force http://server.ip/firmware")
  def upgradenow(opts="")
    sendreceive("upgradenow #{opts}", :timeout=>120)
  end

# Implements the Alien _service_ command
  def service(service_name ="all", command="status")
    sendreceive("service #{service_name} #{command}")
  end

  def alien_tag_list
    tl = AlienTagList.new(self.taglist)
    return tl
  end
end
