using System;
using System.Collections.Generic; // Fix: Import for lists
using RobotVacuum.Interfaces;      // Ensure this matches your interface namespace

namespace RobotVacuum
{
    public class Robot
    {
        private readonly ICleaner _cleaner;
        private readonly IMovement _movement;
        private readonly IBattery _battery;

        // --- Grid Map and Navigation Components ---
        private readonly GridMap _map;
        private int _currentR = 0; // Current grid row index
        private int _currentC = 0; // Current grid column index
        private List<Tuple<int, int>>? _plannedPathToHome; // Path calculated by BFS
        private int _currentPathIndex = 0; // Index of current step in path

        public enum RobotState 
        { 
            AtHome,         // Robot is docked in charging station
            Cleaning,       // Robot is actively cleaning
            ReturningHome,  // Robot is navigating back to charging station
            Stuck           // Robot is stuck and requires hardware service
        }

        private RobotState _currentState;
        private RobotState _previousState; 
        private bool _isWashingMode;       

        // --- Custom User Settings ---
        private readonly int _lowBatteryThreshold; // Battery percentage to trigger return home
        private readonly int _obstacleTurnAngle;   // Angle to turn during obstacle avoidance

        // --- Internal Timers ---
        private int _secondsSinceLastBatteryCheck = 0;

        // Constructor with Dependency Injection for hardware interfaces and GridMap
        public Robot(ICleaner cleaner, IMovement movement, IBattery battery, GridMap map, 
                     int lowBatteryThreshold = 5, int obstacleTurnAngle = 45)
        {
            _cleaner = cleaner;
            _movement = movement;
            _battery = battery;
            _map = map; 

            _lowBatteryThreshold = lowBatteryThreshold;
            _obstacleTurnAngle = obstacleTurnAngle;

            _currentState = RobotState.AtHome; 
            _isWashingMode = false;       

            // Robot starts docked at (0,0)
            _currentR = 0; 
            _currentC = 0;     
        }

        // --- Main Simulation Time Progression Tick ---
        public void Tick(int seconds)
        {
            // If stuck, time passes but no internal physical progress is made
            if (_currentState == RobotState.Stuck)
            {
                Console.WriteLine($"[Simulation] {seconds}s passed, but Robot is STUCK. No actions taken.");
                return;
            }

            for (int i = 0; i < seconds; i++)
            {
                // 1. Perform battery check every 100 seconds
                _secondsSinceLastBatteryCheck++;
                if (_secondsSinceLastBatteryCheck >= 100)
                {
                    _secondsSinceLastBatteryCheck = 0;
                    PerformBatteryCheck();
                }

                // 2. Move step-by-step along the map every 1 second when returning home
                if (_currentState == RobotState.ReturningHome)
                {
                    MoveOneStepOnBFSPath(); 
                }
            }
        }

        // ==========================================
        // Private Helper Functions
        // ==========================================

        private void PerformBatteryCheck()
        {
            int currentBattery = _battery.GetChargePercent();
            Console.WriteLine($"[Robot] [100s Timer] Checking Battery: {currentBattery}%");

            if (_currentState != RobotState.AtHome)
            {
                // If battery drops below threshold, initiate automatic return home sequence
                if (currentBattery < _lowBatteryThreshold)
                {
                    Console.WriteLine($"[Robot] Battery low (< {_lowBatteryThreshold}%). Starting automatic return home.");
                    StartReturningHomeSequence();
                }
            }
            else 
            {
                // Charging station logic while AtHome
                if (currentBattery == 100)
                {
                    _battery.StopCharge(); // Avoid overcharging
                }
                else if (currentBattery < 80)
                {
                    _battery.Charge(); // Restart charging if battery drops below 80%
                }
            }
        }

        // Calculates the shortest path using BFS immediately upon starting return
        private void StartReturningHomeSequence()
        {
            _currentState = RobotState.ReturningHome;
            _cleaner.Stop(); 
            _movement.Stop();
            
            Console.WriteLine($"[Robot] Calculating shortest path from ({_currentR},{_currentC}) to Home (0,0) using BFS...");
            
            // Run BFS on our 2D grid map
            _plannedPathToHome = _map.FindPathToHome(_currentR, _currentC);
            _currentPathIndex = 0;

            if (_plannedPathToHome != null)
            {
                Console.WriteLine($"[Robot] Path found! Shortest distance: {_plannedPathToHome.Count} steps.");
            }
            else
            {
                Console.WriteLine("[Robot] Error: No path found! Robot is blocked by obstacles.");
            }
        }

        // Takes one step on the BFS-calculated path
        private void MoveOneStepOnBFSPath()
        {
            if (_plannedPathToHome == null || _currentPathIndex >= _plannedPathToHome.Count)
            {
                EvArrivedHome(); 
                return;
            }

            var nextStep = _plannedPathToHome[_currentPathIndex];
            _currentPathIndex++;

            _currentR = nextStep.Item1;
            _currentC = nextStep.Item2;

            Console.WriteLine($"[Robot] [1s Move] Robot moved to ({_currentR}, {_currentC})");
            _map.PrintMap(_currentR, _currentC); 

            // Check if docked at Home
            if (_currentR == 0 && _currentC == 0)
            {
                EvArrivedHome();
            }
        }

        // ==========================================
        // Event Handlers
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
                
                // Hardware action simulation for undocking
                _movement.GoBackward();
                _movement.Turn(180);
                _movement.GoForward();

                // Set initial coordinates in room and mark as cleaned
                _currentR = 5; 
                _currentC = 5; 
                _map.MarkCleaned(_currentR, _currentC); 

                Console.WriteLine($"Robot is now out of the dock and Cleaning. Position set to ({_currentR}, {_currentC})"); 
            }
            else
            {
                Console.WriteLine($"Event evOn ignored. Current state is: {_currentState}");
            }
        }

        public void EvHome()
        {
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
                
                // Begin charging if battery is not full
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
    } 
}