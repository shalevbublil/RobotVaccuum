using System;
using RobotVacuum.Interfaces;

namespace RobotVacuum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Starting Robot Vacuum Simulator - Roborock Coverage & BFS Recovery ===");

            // 1. Create instances of hardware mocks and the 10x10 grid map
            var cleaner = new ConsoleCleaner();
            var movement = new ConsoleMovement();
            var battery = new ConsoleBattery();
            var map = new GridMap(10, 10); 

            // Initialize battery with 100% charge for a compact and exciting simulation
            battery.SetBatteryPercent(100);

            // 2. Create Robot controller and inject dependencies
            var robot = new Robot(cleaner, movement, battery, map, lowBatteryThreshold: 5);

            // 3. User input for custom starting position
            int startR = 0;
            int startC = 0;

            Console.WriteLine("\n[Setup] Enter starting row index (0-9) [Default = 0]:");
            string? rowInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(rowInput) && int.TryParse(rowInput, out int r))
            {
                startR = Math.Clamp(r, 0, 9);
            }

            Console.WriteLine("[Setup] Enter starting column index (0-9) [Default = 0]:");
            string? colInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(colInput) && int.TryParse(colInput, out int c))
            {
                startC = Math.Clamp(c, 0, 9);
            }

            // 4. Start the cleaning session
            Console.WriteLine($"\n--- Starting simulation from point ({startR}, {startC}) ---");
            robot.EvOn(startR, startC);

            // 5. Run simulation Tick-by-Tick until robot returns to dock and is fully charged (100%)
            int totalSeconds = 0;
            bool isFirstStep = true;

            while (isFirstStep || robot.CurrentState != Robot.RobotState.AtHome || battery.GetChargePercent() < 100)
            {
                isFirstStep = false;
                totalSeconds++;
                robot.Tick(1); // Progress simulation by 1 second

                // If battery is charging, increment battery by 20% per second for fast charging simulation
                if (battery.IsCharging())
                {
                    battery.ChargeStep(20);
                }

                // Small pause in output to make it readable in console
                System.Threading.Thread.Sleep(300);
            }
            // Run a final tick to let the robot check and stop charging at 100%
            robot.Tick(5);

            Console.WriteLine($"\n=== Simulation Finished Successfully in {totalSeconds}s! ===");
        }
    }

    // --- Dynamic Hardware Mocks for Simulation ---

    public class ConsoleCleaner : ICleaner
    {
        public void StartVacuum() => Console.WriteLine("[Hardware-Cleaner] Suction started.");
        public void StartWashing() => Console.WriteLine("[Hardware-Cleaner] Water sweep started.");
        public void Stop() => Console.WriteLine("[Hardware-Cleaner] Suction stopped.");
    }

    public class ConsoleMovement : IMovement
    {
        public void GoForward() { }
        public void GoBackward() { }
        public void Turn(int degrees) { }
        public void Stop() => Console.WriteLine("[Hardware-Movement] Movement stopped.");
    }

    public class ConsoleBattery : IBattery
    {
        private int _batteryPercent = 100;
        private bool _isCharging = false;

        public int GetChargePercent() => _batteryPercent;
        public void SetBatteryPercent(int percent) => _batteryPercent = percent;
        
        public void Drain(int percent)
        {
            _batteryPercent = Math.Max(0, _batteryPercent - percent);
        }

        public void ChargeStep(int amount)
        {
            _batteryPercent = Math.Min(100, _batteryPercent + amount);
            Console.WriteLine($"[Hardware-Battery] Battery charged to: {_batteryPercent}%");
        }

        public bool IsCharging() => _isCharging;

        public void Charge()
        {
            _isCharging = true;
            Console.WriteLine("[Hardware-Battery] Battery charging...");
        }

        public void StopCharge()
        {
            _isCharging = false;
            Console.WriteLine("[Hardware-Battery] Battery fully charged.");
        }
    }
}