using System;
using RobotVacuum.Interfaces;

namespace RobotVacuum
{
    public class Robot
    {
        private readonly ICleaner _cleaner;
        private readonly IMovement _movement;
        private readonly IBattery _battery;
    public enum RobotState 
        { 
            AtHome,         
            Cleaning,       
            ReturningHome, 
            Stuck          
        }
        private RobotState _currentState;
        private RobotState _previousState;
        private bool _isWashingMode;
        public Robot(ICleaner cleaner, IMovement movement, IBattery battery)
        {
            _cleaner = cleaner;
            _movement = movement;
            _battery = battery;
            
            _currentState = RobotState.AtHome; // The robot starts in the "AtHome" state
            _isWashingMode = false;            // Default mode: Cleaning
        }
       public void EvOn()
{
    // check if the robot is currently at home before starting cleaning
    if (_currentState == RobotState.AtHome)
    {
        Console.WriteLine("Event received: evOn - starting cleaning");
        _currentState = RobotState.Cleaning;

        if (_isWashingMode)
        {
            _cleaner.StartWashing();
        }
        else 
        {
            _cleaner.StartVacuum(); 
      }
        
        _movement.GoBackward();
        _movement.Turn(180);
        _movement.GoForward();

        Console.WriteLine("Robot is now out of the dock and Cleaning."); 
    }
    //  if the robot is already cleaning or returning home, ignore the event
    else
    {
        Console.WriteLine($"Event evOn ignored. Current state is: {_currentState}");
    }
}
        public void EvHome()
        {
            // TODO: לוגיקת חזרה הביתה יזומה
            Console.WriteLine("Event received: evHome");
        }

        public void EvMode()
        {
    if (_currentState == RobotState.Stuck)
    {
        Console.WriteLine("[Robot] Event evMode ignored. Cannot change mode while the robot is stuck!");
        return;
    }

    _isWashingMode = !_isWashingMode;
    Console.WriteLine($"[Robot] Event received: evMode. Mode changed. Washing mode active: {_isWashingMode}");
}

       public void EvStuck()
{
    // you can only transition to the Stuck state if the robot is not already at home or already stuck
    if (_currentState != RobotState.AtHome && _currentState != RobotState.Stuck)
    {
        Console.WriteLine($"[Robot] Event received: evStuck! Robot is stuck. Current state was: {_currentState}");
        
        // save the previous state before transitioning to Stuck
        _previousState = _currentState;
                
        // Transition to Stuck state
        _currentState = RobotState.Stuck;

        // Stop all operations
        _cleaner.Stop();
        _movement.Stop();

        Console.WriteLine("[Robot] All systems stopped safely. Waiting for physical service (evService)...");
    }
    else
    {
        Console.WriteLine($"[Robot] Event evStuck ignored. Robot is in state: {_currentState}");
    }
}


public void EvService()
{
    // we can only transition out of the Stuck state if the robot is currently stuck
    if (_currentState == RobotState.Stuck)
    {
        Console.WriteLine("[Robot] Event received: evService. Problem resolved! Restoring previous state...");

        // restore the previous state
        _currentState = _previousState;

        Console.WriteLine($"[Robot] State successfully restored to: {_currentState}");

        // resume operations based on the restored state
        if (_currentState == RobotState.Cleaning)
        {
            // if we were cleaning before getting stuck, resume cleaning
            if (_isWashingMode)
            {
                _cleaner.StartWashing();
            }
            else
            {
                _cleaner.StartVacuum();
            }
            _movement.GoForward();
        }
        else if (_currentState == RobotState.ReturningHome)
        {
            // if we were returning home before getting stuck, continue the movement towards the charging station (without starting cleaning)
            _movement.GoForward();
        }
    }
    else
    {
        Console.WriteLine($"[Robot] Event evService ignored. Robot is not currently stuck (State: {_currentState}).");
    }
}

        public void EvArrivedHome()
        {
            // TODO: עצירה, הפסקת תנועה ומעבר לטעינה
            Console.WriteLine("Event received: evArrivedHome");
        }
        private int GetHomeAngle()
        {
            // מדמה חישוב זווית לעמדת הטעינה
            return 45; 
        }
    }
}
