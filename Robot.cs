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
            AtHome,         // in the charging station
            Cleaning,       // actively cleaning
            ReturningHome,  // returning to the charging station
            Stuck           // stuck and waiting for physical service
        }

        private RobotState _currentState;
        private RobotState _previousState; 
        private bool _isWashingMode;       

        // --- Configuration Parameters ---
        private readonly int _lowBatteryThreshold; // percentage below which the robot should return home (default 5%)
        private readonly int _obstacleTurnAngle;   // degrees to turn when avoiding obstacles (default 45°)

        // --- Internal Timers ---
        private int _secondsSinceLastBatteryCheck = 0;
        private int _secondsSinceLastDockSearch = 0;
        private int _secondsSinceLastNavigation = 0;
        private bool _isHomeLocated = false; // whether the charging station has been located (step 1 of the return process)

        // constructor with dependency injection for hardware interfaces and configuration parameters 
        public Robot(ICleaner cleaner, IMovement movement, IBattery battery, 
                     int lowBatteryThreshold = 5, int obstacleTurnAngle = 45)
        {
            _cleaner = cleaner;
            _movement = movement;
            _battery = battery;
            
            _lowBatteryThreshold = lowBatteryThreshold;
            _obstacleTurnAngle = obstacleTurnAngle;

            _currentState = RobotState.AtHome; 
            _isWashingMode = false;            
        }

        // --- Main Simulation Tick ---
        public void Tick(int seconds)
        {
            // if the robot is stuck, we do not perform any actions during the tick
            if (_currentState == RobotState.Stuck)
            {
                Console.WriteLine($"[Simulation] {seconds}s passed, but Robot is STUCK. No actions taken.");
                return;
            }

            for (int i = 0; i < seconds; i++)
            {
                // manage battery checks every 100 seconds
                _secondsSinceLastBatteryCheck++;
                if (_secondsSinceLastBatteryCheck >= 100)
                {
                    _secondsSinceLastBatteryCheck = 0;
                    PerformBatteryCheck();
                }

                // manage the return home process if the robot is in ReturningHome state
                if (_currentState == RobotState.ReturningHome)
                {
                    if (!_isHomeLocated)
                    {
                       // stage 1: searching for the charging station direction - check every second
                        _secondsSinceLastDockSearch++;
                        if (_secondsSinceLastDockSearch >= 1)
                        {
                            _secondsSinceLastDockSearch = 0;
                            TryLocateHome();
                        }
                    }
                    else
                    {
                        // stage 2: active navigation - calculate angle and turn every 7 seconds
                        _secondsSinceLastNavigation++;
                        if (_secondsSinceLastNavigation >= 7)
                        {
                            _secondsSinceLastNavigation = 0;
                            NavigateTowardsHome();
                        }
                    }
                }
            }
        }

        private void PerformBatteryCheck()
        {
            int currentBattery = _battery.GetChargePercent();
            Console.WriteLine($"[Robot] [100s Timer] Checking Battery: {currentBattery}%");

            if (_currentState != RobotState.AtHome)
            {
                // if the robot is not at home, check if battery is below threshold to initiate return home
                if (currentBattery < _lowBatteryThreshold)
                {
                    Console.WriteLine($"[Robot] Battery low (< {_lowBatteryThreshold}%). Starting automatic return home.");
                    StartReturningHomeSequence();
                }
            }
            else // the robot is at home
            {
                if (currentBattery == 100)
                {
                    _battery.StopCharge(); // if fully charged, stop charging
                }
                else if (currentBattery < 80)
                {
                    _battery.Charge(); // recharge if battery is below 80% while at home
                }
            }
        }

        private void StartReturningHomeSequence()
        {
            _currentState = RobotState.ReturningHome;
            _cleaner.Stop(); // stop cleaning systems
            _isHomeLocated = false;
            _secondsSinceLastDockSearch = 0;
            _secondsSinceLastNavigation = 0;
            Console.WriteLine("[Robot] Cleaning stopped. Phase 1: Moving and searching for charging station direction...");
        }

        private void TryLocateHome()
        {
            Console.WriteLine("[Robot] [1s Sensor] Scanning for charging station signal...");
            // simulate successful detection of the charging station
            _isHomeLocated = true; 
            Console.WriteLine("[Robot] Charging station located! Phase 2: Switch to active 7s navigation.");
            _secondsSinceLastNavigation = 0;
        }

        private void NavigateTowardsHome()
        {
            int angle = GetHomeAngle();
            Console.WriteLine($"[Robot] [7s Navigation] Recalculating route. Turning {angle}° towards dock and driving forward.");
            _movement.Turn(angle);
            _movement.GoForward();
        }

        // ==========================================
        // Event Handlers for Robot Events
        // ==========================================

        public void EvOn()
        {
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
            else
            {
                Console.WriteLine($"Event evOn ignored. Current state is: {_currentState}");
            }
        }

        public void EvHome()
        {
            // only allow manual return to dock if the robot is not already at home or stuck
            if (_currentState != RobotState.AtHome && _currentState != RobotState.Stuck)
            {
                Console.WriteLine("[Robot] Event received: evHome. User requested manual return to dock.");
                StartReturningHomeSequence();
            }
            else
            {
                Console.WriteLine($"[Robot] Event evHome ignored. Current state is: {_currentState}");
            }
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
            if (_currentState != RobotState.AtHome && _currentState != RobotState.Stuck)
            {
                Console.WriteLine($"[Robot] Event received: evStuck! Robot is stuck. Current state was: {_currentState}");
                _previousState = _currentState;
                _currentState = RobotState.Stuck;
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
            if (_currentState == RobotState.Stuck)
            {
                Console.WriteLine("[Robot] Event received: evService. Problem resolved! Restoring previous state...");
                _currentState = _previousState;
                Console.WriteLine($"[Robot] State successfully restored to: {_currentState}");

                if (_currentState == RobotState.Cleaning)
                {
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
            if (_currentState == RobotState.ReturningHome)
            {
                Console.WriteLine("[Robot] Event received: evArrivedHome. Robot docked into charging station successfully.");
                _currentState = RobotState.AtHome;
                _movement.Stop();
                
                // start charging if battery is not full
                int batteryPercent = _battery.GetChargePercent();
                if (batteryPercent < 100)
                {
                    _battery.Charge();
                }
            }
            else
            {
                Console.WriteLine($"[Robot] Event evArrivedHome ignored. Current state is: {_currentState}");
            }
        }

        // ==========================================
        // Helper Functions for the Controller
        // ==========================================
        private int GetHomeAngle()
        {
            // In a real implementation, this would calculate the angle to the charging station based on sensor data.
            return 35; 
        }
    }
}
