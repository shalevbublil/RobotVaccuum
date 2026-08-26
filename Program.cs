using System;
using RobotVacuum.Interfaces; // Ensure this matches your interface namespace

namespace RobotVacuum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Starting Robot Vacuum Simulator - Grid Map & BFS Pathfinding ===");

            // 1. Create instances of hardware mocks and the 2D grid map
            var cleaner = new ConsoleCleaner();
            var movement = new ConsoleMovement();
            var battery = new ConsoleBattery();
            var map = new GridMap(10, 10); // Create a 10x10 grid room map

            // 2. Create Robot controller and inject dependencies
            var robot = new Robot(cleaner, movement, battery, map, lowBatteryThreshold: 5);

            // --- Simulation Scenario ---

            Console.WriteLine("\n--- Step 1: Turn on and start cleaning ---");
            robot.EvOn(); // Robot starts cleaning and teleports to room location (5,5)

            Console.WriteLine("\n--- Step 2: Manually draining battery to 4% (Low Battery) ---");
            battery.SetBatteryPercent(4);

            Console.WriteLine("\n--- Step 3: Running simulation Tick (100 seconds) to trigger check & start return ---");
            // The Tick triggers the 100s battery check, detects low battery, runs BFS and moves step-by-step
            robot.Tick(100); 

            Console.WriteLine("\n=== Simulation Finished Successfully! ===");

            Console.WriteLine("\n--- Step 4: Giving the robot 10 more seconds to walk all the way Home ---");
            robot.Tick(10); // Runs the simulation for 10 more seconds to let the robot finish its 10-step path
        }
    }

    // --- Hardware Mocks for Console Simulation ---

    public class ConsoleCleaner : ICleaner
    {
        public void StartVacuum() => Console.WriteLine("[Hardware-Cleaner] Vacuum motor started.");
        public void StartWashing() => Console.WriteLine("[Hardware-Cleaner] Water pump started.");
        public void Stop() => Console.WriteLine("[Hardware-Cleaner] Cleaning systems stopped.");
    }

    public class ConsoleMovement : IMovement
    {
        public void GoForward() { }
        public void GoBackward() { }
        public void Turn(int degrees) { }
        public void Stop() => Console.WriteLine("[Hardware-Movement] Wheels locked. Movement stopped.");
    }

    public class ConsoleBattery : IBattery
    {
        private int _batteryPercent = 100;
        public int GetChargePercent() => _batteryPercent;
        public void SetBatteryPercent(int percent) => _batteryPercent = percent;
        public void Charge() => Console.WriteLine("[Hardware-Battery] Charger connected. Charging...");
        public void StopCharge() => Console.WriteLine("[Hardware-Battery] Charger disconnected. Fully charged.");
    }
}