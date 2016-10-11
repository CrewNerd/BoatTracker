=begin rdoc
=Alien Ruby RFID Library 
==alienblocktimer.rb	

A tiny little class to help me time code blocks. Ruby has a 'profiler' library that is
much more powerful, but I've found this handy for quick timing checks.

Returns elapsed time for an operation and an optional description.


Example 1):

  You can just create the timer in place and use it.
  
  puts AlienBlockTimer.new("Time for this block: "){
    ... Some lengthy operation in the block ...
  }
  
  Results in:

  Time for this block: 0.123


Example 2):

  Alternatively, create an instance and use it later.
  
  t=AlienBlockTimer.new("")
  
  puts t.measure("Time for this block: "){
    ... Some lengthy operation in the block ...
  }
  
  ...or...
  
  t.measure("Time for this other block: "){
  ... Some lengthy operation in the block ...
  }
  
  puts t

Example 3):
  
  You can also use the class to make a series of elapsed time measurements.
  
  puts "Starting measurment..."
  
  t.start
  
  (code here...)
  
  puts "So far: #{t.elapsed}"
  
  (more code...)
  
  puts "Done!: #{t.elapsed}"
  


Copyright 2007 Alien Technology Corporation. All rights reserved.
=end

#A class to measure the execution time for blocks of code.
class AlienBlockTimer


  #Setup the timer and pass in an optional description. If there is an associated block with the
  #call, execute a timing measurment.
  def initialize (description="",&block)
    @t1=Time.now
    @measurement = "Timer initialized."
    unless block.nil?
      measure(description){yield} 
    end
  end


  #Measure the execution time for an asscociated code block.
  def measure(description="", &block)

    start

    unless block.nil?
      yield
      @measurement =  description + elapsed.to_s
    else
      #Error message Haiku. :)
      @measurement = "No block to measure. Your timer waits patiently. Give him one to use."
    end

  end
  
  #Grab a start time
  def start
    @t1=Time.now
  end
  
  #The time since we called start or since the class was initialized.
  def elapsed
    Time.now - @t1
  end

  def inspect 
    @measurement
  end

  def to_s
    @measurement
  end

end
