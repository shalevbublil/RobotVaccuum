using System;
using RobotVacuum.Interfaces; 

namespace RobotVacuum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Starting Robot Vacuum Simulator ===");

            // create instances of the hardware interfaces (mocks for simulation)
            var cleaner = new ConsoleCleaner();
            var movement = new ConsoleMovement();
            var battery = new ConsoleBattery();

            // create the robot instance with the hardware interfaces
            var robot = new Robot(cleaner, movement, battery);

            // --- Simulation Steps ---

            Console.WriteLine("\n--- Step 1: Trying to turn on the robot ---");
            robot.EvOn(); 

            Console.WriteLine("\n--- Step 2: Oh no! The robot gets stuck (evStuck) ---");
            robot.EvStuck(); 

            Console.WriteLine("\n--- Step 3: Trying to change mode while stuck (Protection Check) ---");
            robot.EvMode(); 

            Console.WriteLine("\n--- Step 4: Physical service completed (evService) ---");
            robot.EvService();

            Console.WriteLine("\n=== Simulation Finished Successfully! ===");
        }
    }

    // --- Mock Implementations ---

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

        public int GetChargePercent()
        {
            return _batteryPercent;
        }

        public void Charge()
        {
            Console.WriteLine("[Hardware-Battery] Charger connected. Battery is charging...");
        }

        public void StopCharge()
        {
            Console.WriteLine("[Hardware-Battery] Charger disconnected. Battery charging stopped.");
        }
    }
}