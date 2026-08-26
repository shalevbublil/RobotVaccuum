using System;
using System.Collections.Generic;
using RobotVacuum.Interfaces;

namespace RobotVacuum
{
    public class Robot
    {
        private readonly ICleaner _cleaner;
        private readonly IMovement _movement;
        private readonly IBattery _battery;

        // --- Grid Map and Navigation Components ---
        private readonly GridMap _map;
        private int _currentR = 0; 
        private int _currentC = 0; 
        private List<Tuple<int, int>>? _plannedPathToHome; 
        private int _currentPathIndex = 0; 

        // --- Smart Roborock-style Cleaning State Variables ---
        private enum CleaningPhase
        {
            Perimeter,      // Phase 1: Clean perimeter of walls and obstacles
            InteriorZigzag, // Phase 2: Clean remaining interior cells with zigzag sweeps
            Done            // Completed cleaning
        }

        private CleaningPhase _currentCleaningPhase = CleaningPhase.Perimeter;
        private int _dirC = 1; // 1 for moving right, -1 for moving left
        private List<Tuple<int, int>>? _recoveryPathToUncleaned;
        private int _recoveryPathIndex = 0;

        public enum RobotState 
        { 
            AtHome,         
            Cleaning,       
            ReturningHome,  
            Stuck           
        }
        public RobotState CurrentState => _currentState;

        private RobotState _currentState;
        private RobotState _previousState; 
        private bool _isWashingMode;       

        // --- Custom User Settings ---
        private readonly int _lowBatteryThreshold; 
        private readonly int _obstacleTurnAngle;   

        // --- Internal Timers ---
        private int _secondsSinceLastBatteryCheck = 0;

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

            _currentR = 0; 
            _currentC = 0;     
        }

        // --- Main Simulation Time Progression Tick ---
        public void Tick(int seconds)
        {
            if (_currentState == RobotState.Stuck)
            {
                Console.WriteLine($"[Simulation] {seconds}s passed, but Robot is STUCK. No actions taken.");
                return;
            }

            for (int i = 0; i < seconds; i++)
            {
                // Perform battery check every 5 seconds for responsiveness
                _secondsSinceLastBatteryCheck++;
                if (_secondsSinceLastBatteryCheck >= 5)
                {
                    _secondsSinceLastBatteryCheck = 0;
                    PerformBatteryCheck();
                }

                // If battery check forced returning home, stop cleaning immediately
                if (_currentState == RobotState.ReturningHome)
                {
                    MoveOneStepOnBFSPath();
                    continue;
                }

                if (_currentState == RobotState.Cleaning)
                {
                    MoveOneStepCleaning();
                    
                    // Simulate battery drain: 1% per second of cleaning
                    (_battery as ConsoleBattery)?.Drain(1);
                }
            }
        }

        // ==========================================
        // Private Helper Functions
        // ==========================================

        private void PerformBatteryCheck()
        {
            int currentBattery = _battery.GetChargePercent();
            Console.WriteLine($"[Robot] [5s Timer] Checking Battery: {currentBattery}%");

            if (_currentState != RobotState.AtHome)
            {
                if (currentBattery < _lowBatteryThreshold)
                {
                    Console.WriteLine($"[Robot] Battery low (< {_lowBatteryThreshold}%). Starting automatic return home.");
                    StartReturningHomeSequence();
                }
            }
            else 
            {
                if (currentBattery == 100)
                {
                    _battery.StopCharge(); 
                }
                else if (currentBattery < 80)
                {
                    _battery.Charge(); 
                }
            }
        }

        private void StartReturningHomeSequence()
        {
            _currentState = RobotState.ReturningHome;
            _cleaner.Stop(); 
            _movement.Stop();
            _recoveryPathToUncleaned = null; // Clear active recovery path
            
            Console.WriteLine($"[Robot] Calculating shortest path from ({_currentR},{_currentC}) to Home (0,0) using BFS...");
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

        private void MoveOneStepCleaning()
        {
            // Mark the current position as cleaned
            _map.MarkCleaned(_currentR, _currentC);

            // PHASE 1: Perimeter Cleaning (Wall and Obstacle Following)
            if (_currentCleaningPhase == CleaningPhase.Perimeter)
            {
                if (_recoveryPathToUncleaned != null && _recoveryPathIndex < _recoveryPathToUncleaned.Count)
                {
                    var nextStep = _recoveryPathToUncleaned[_recoveryPathIndex];
                    _recoveryPathIndex++;
                    _currentR = nextStep.Item1;
                    _currentC = nextStep.Item2;
                    Console.WriteLine($"[Robot] [Cleaning - Perimeter Phase] Moving to ({_currentR}, {_currentC})");
                    _map.PrintMap(_currentR, _currentC);
                    return;
                }

                // Find the nearest uncleaned perimeter cell using BFS
                _recoveryPathToUncleaned = _map.FindPathToNearestUncleanedPerimeter(_currentR, _currentC);
                _recoveryPathIndex = 0;

                if (_recoveryPathToUncleaned != null && _recoveryPathToUncleaned.Count > 0)
                {
                    var nextStep = _recoveryPathToUncleaned[_recoveryPathIndex];
                    _recoveryPathIndex++;
                    _currentR = nextStep.Item1;
                    _currentC = nextStep.Item2;
                    Console.WriteLine($"[Robot] [Cleaning - Perimeter Phase] Moving to ({_currentR}, {_currentC})");
                    _map.PrintMap(_currentR, _currentC);
                }
                else
                {
                    // No uncleaned perimeter cells left! Transition to Interior Zigzag Phase
                    Console.WriteLine("[Robot] >>> Perimeter cleaning complete! Transitioning to Interior Zigzag Sweep. <<<");
                    _currentCleaningPhase = CleaningPhase.InteriorZigzag;
                    _recoveryPathToUncleaned = null;
                    
                    // Call cleaning again to immediately start the next phase
                    MoveOneStepCleaning();
                }
            }
            // PHASE 2: Interior Zigzag Sweeping & BFS Recovery
            else if (_currentCleaningPhase == CleaningPhase.InteriorZigzag)
            {
                if (_recoveryPathToUncleaned != null && _recoveryPathIndex < _recoveryPathToUncleaned.Count)
                {
                    var nextStep = _recoveryPathToUncleaned[_recoveryPathIndex];
                    _recoveryPathIndex++;
                    _currentR = nextStep.Item1;
                    _currentC = nextStep.Item2;
                    Console.WriteLine($"[Robot] [Cleaning - BFS Interior Recovery] Moving to ({_currentR}, {_currentC})");
                    _map.PrintMap(_currentR, _currentC);

                    if (_recoveryPathIndex >= _recoveryPathToUncleaned.Count)
                    {
                        _recoveryPathToUncleaned = null; // Reached recovery destination
                        Console.WriteLine("[Robot] Reached target interior area. Resuming interior zigzag sweep.");
                    }
                    return;
                }

                // Normal horizontal movement inside the interior bounds (skipping perimeter)
                int nextR = _currentR;
                int nextC = _currentC + _dirC;

                if (nextC >= 0 && nextC < 10 && !_map.IsObstacle(nextR, nextC) && !_map.IsPerimeter(nextR, nextC))
                {
                    _currentR = nextR;
                    _currentC = nextC;
                    Console.WriteLine($"[Robot] [Cleaning - Interior Zigzag] Sweeping to ({_currentR}, {_currentC})");
                    _map.PrintMap(_currentR, _currentC);
                }
                else
                {
                    // Boundary/perimeter hit. Move down to the next row
                    nextR = _currentR + 1;
                    _dirC = -_dirC; // Reverse sweeping direction
                    nextC = _currentC;

                    if (nextR < 10 && !_map.IsObstacle(nextR, nextC) && !_map.IsPerimeter(nextR, nextC))
                    {
                        _currentR = nextR;
                        _currentC = nextC;
                        Console.WriteLine($"[Robot] [Cleaning - Interior Zigzag] Moving down to row ({_currentR}, {_currentC})");
                        _map.PrintMap(_currentR, _currentC);
                    }
                    else
                    {
                        // Dead end in interior! Run BFS to find the closest uncleaned interior cell (resolves the orphan-cell issue)
                        Console.WriteLine($"[Robot] Interior zigzag hit dead end at ({_currentR}, {_currentC}). Finding nearest uncleaned interior cell...");
                        _recoveryPathToUncleaned = _map.FindPathToNearestUncleanedInterior(_currentR, _currentC);
                        _recoveryPathIndex = 0;

                        if (_recoveryPathToUncleaned != null && _recoveryPathToUncleaned.Count > 0)
                        {
                            Console.WriteLine($"[Robot] Path to uncleaned interior area found ({_recoveryPathToUncleaned.Count} steps). Navigating...");
                            var nextStep = _recoveryPathToUncleaned[_recoveryPathIndex];
                            _recoveryPathIndex++;
                            _currentR = nextStep.Item1;
                            _currentC = nextStep.Item2;
                            Console.WriteLine($"[Robot] [Cleaning - BFS Interior Recovery] Moving to ({_currentR}, {_currentC})");
                            _map.PrintMap(_currentR, _currentC);
                        }
                        else
                        {
                            // Absolutely no uncleaned reachable interior cells left! Cleaning complete!
                            Console.WriteLine("[Robot] >>> All reachable perimeter and interior cells are clean! Returning to dock. <<<");
                            _currentCleaningPhase = CleaningPhase.Done;
                            StartReturningHomeSequence();
                        }
                    }
                }
            }
        }

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

            Console.WriteLine($"[Robot] [1s Move] Robot returning home at ({_currentR}, {_currentC})");
            _map.PrintMap(_currentR, _currentC); 

            if (_currentR == 0 && _currentC == 0)
            {
                EvArrivedHome();
            }
        }

        // ==========================================
        // Event Handlers
        // ==========================================

        public void EvOn(int startR = 0, int startC = 0)
        {
            if (_currentState == RobotState.AtHome)
            {
                if (_map.IsObstacle(startR, startC))
                {
                    Console.WriteLine($"[Robot] Error: Start position ({startR},{startC}) is an obstacle! Cannot start.");
                    return;
                }

                Console.WriteLine($"Event received: evOn - starting cleaning at position ({startR}, {startC})");
                _currentState = RobotState.Cleaning;
                _currentCleaningPhase = CleaningPhase.Perimeter; // Reset to perimeter phase on startup

                if (_isWashingMode)
                {
                    _cleaner.StartWashing();
                }
                else 
                {
                    _cleaner.StartVacuum();
                }
                
                _currentR = startR;
                _currentC = startC;
                _map.MarkCleaned(_currentR, _currentC);

                Console.WriteLine($"Robot is now out of the dock and Cleaning."); 
                _map.PrintMap(_currentR, _currentC);
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
