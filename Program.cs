using System;
using RobotVacuum.Interfaces; 

namespace RobotVacuum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Starting Robot Vacuum Simulator - Battery & Navigation ===");

            // create instances of the hardware interfaces (mocks for console simulation)
            var cleaner = new ConsoleCleaner();
            var movement = new ConsoleMovement();
            var battery = new ConsoleBattery();

            // create the robot instance with the hardware interfaces and parameters
            var robot = new Robot(cleaner, movement, battery, lowBatteryThreshold: 5, obstacleTurnAngle: 45);

            // --- Simulation Steps ---

            Console.WriteLine("\n--- Step 1: Turn on and start cleaning ---");
            robot.EvOn(); 

            Console.WriteLine("\n--- Step 2: Simulating 50 seconds of cleaning (Battery is at 100%) ---");
            robot.Tick(50); 

            Console.WriteLine("\n--- Step 3: Simulating battery drain (Manually forcing battery to 4%) ---");
            battery.SetBatteryPercent(4); // actually set battery to 4% to trigger low battery behavior

            Console.WriteLine("\n--- Step 4: Advancing simulation to trigger the 100s Battery Check ---");
            // simulate 60 seconds to trigger the battery check and see if the robot initiates return to dock
            robot.Tick(60); 
            // At this point, the robot should detect low battery and start returning home
            Console.WriteLine("\n--- Step 5: Continuing simulation to see active 7s navigation in action ---");
            // simulate additional time to allow the robot to navigate towards the charging station
            robot.Tick(15);

            Console.WriteLine("\n--- Step 6: Robot arrives physically at the charging station (evArrivedHome) ---");
            robot.EvArrivedHome(); // trigger the event that the robot has arrived at the charging station

            Console.WriteLine("\n--- Step 7: Simulating charge progress up to 100% ---");
            battery.SetBatteryPercent(100); // simulate battery fully charged
            robot.Tick(100); // simulate time passing to allow the robot to recognize full charge and stop charging

            Console.WriteLine("\n=== Simulation Finished Successfully! ===");
        }
    }

    // --- Mock Implementations for Hardware Interfaces ---

    public class ConsoleCleaner : ICleaner
    {
        public void StartVacuum() => Console.WriteLine("[Hardware-Cleaner] Vacuum motor started (SUCTION ON).");
        public void StartWashing() => Console.WriteLine("[Hardware-Cleaner] Water pump started (WASHING ON).");
        public void Stop() => Console.WriteLine("[Hardware-Cleaner] Cleaning systems stopped.");
    }

    public class ConsoleMovement : IMovement
    {
        public void GoForward() => Console.WriteLine("[Hardware-Movement] Driving forward...");
        public void GoBackward() => Console.WriteLine("[Hardware-Movement] Reversing backward...");
        public void Turn(int degrees) => Console.WriteLine($"[Hardware-Movement] Turning {degrees} degrees.");
        public void Stop() => Console.WriteLine("[Hardware-Movement] Wheels locked. Movement stopped.");
    }

    public class ConsoleBattery : IBattery
    {
        private int _batteryPercent = 100;

        public int GetChargePercent() => _batteryPercent;
        
        public void SetBatteryPercent(int percent)
        {
            _batteryPercent = percent;
            Console.WriteLine($"[Simulation-Battery] Battery level manually set to: {_batteryPercent}%");
        }

        public void Charge() => Console.WriteLine("[Hardware-Battery] Charger connected. Battery is charging...");
        public void StopCharge() => Console.WriteLine("[Hardware-Battery] Charger disconnected. Battery charging stopped.");
    }
}