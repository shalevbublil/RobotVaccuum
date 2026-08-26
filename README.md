[README.md](https://github.com/user-attachments/files/31475968/README.md)
# Robotic Vacuum Simulator

A highly professional, modular, and event-driven **Robotic Vacuum Simulator** built in **C#** and **.NET 10.0**. This project demonstrates advanced software engineering principles, robust architectural patterns, and sophisticated spatial pathfinding algorithms. 

Instead of basic random-bounce navigation, this simulator implements a state-of-the-art **Roborock-style coverage algorithm** combined with **Breadth-First Search (BFS)** for intelligent pathfinding, obstacle avoidance, and automatic docking.

---

## 🚀 Key Features

* **Event-Driven Architecture:** Complete decoupling of system control from physical hardware interfaces using C# Interfaces (`ICleaner`, `IMovement`, `IBattery`) and Dependency Injection (DI).
* **Robust State Machine:** Managing transitions between distinct operating states (`AtHome`, `Cleaning`, `ReturningHome`, `Stuck`) with solid safety-recovery flows (`evStuck` / `evService`).
* **Advanced 2D Grid Mapping:** Discretizing the room into a 10x10 coordinate grid with static obstacles representing walls and furniture.
* **Roborock-Style Coverage Algorithm:** 
  1. *Perimeter Phase:* Clean the room boundaries (walls and obstacle edges) first.
  2. *Interior Zigzag Phase:* Clean the inside area using efficient parallel sweeps.
  3. *BFS Recovery:* If the robot hits a dead-end or gets blocked, it runs a BFS to find and navigate to the nearest uncleaned cell, preventing "orphan cells".
* **BFS Pathfinding & Docking:** Finding the absolute shortest path back to the charging station `(0,0)` when the battery drops below the threshold, avoiding all obstacles along the way.
* **Interactive Command Line Interface:** Allows the user to select the robot's starting position dynamically and watch the simulation run tick-by-tick with real-time ASCII map rendering.

---

## 🛠️ Architecture & Design Patterns

### 1. Interface Segregation (SOLID)
Hardware capabilities are split into highly cohesive interfaces:
* `ICleaner`: Controls suction and water pumps.
* `IMovement`: Controls physical movement commands (GoForward, GoBackward, Turn, Stop).
* `IBattery`: Monitors battery levels, triggers charging, and manages power.

This design makes it extremely easy to swap the console-based mock classes with a real physical robot API or a GUI-based Unity rendering engine without modifying the core robot logic.

### 2. State Machine Diagram
```
       [ AtHome ] --- evOn ---> [ Cleaning (Perimeter -> Zigzag) ]
           ^                                |
           |                            evStuck / Low Battery
      evArrivedHome                         |
           |                                v
    [ ReturningHome (BFS) ] <--- evService --- [ Stuck ]
```

---

## 📂 Project Structure

* **`GridMap.cs`**: Handles the 2D grid matrix, boundary detection, and the BFS algorithms (`FindPathToHome`, `FindPathToNearestUncleanedPerimeter`, `FindPathToNearestUncleanedInterior`).
* **`Robot.cs`**: The central controller acting as the brain. Manages the state machine, processes simulation clock ticks, monitors battery levels, and coordinates cleaning phases.
* **`Program.cs`**: The simulation runner. Handles user interactive input, initializes dependencies, and drives the real-time simulation loop.

---

## 🚦 How to Run

1. Clone this repository:
   ```bash
   git clone https://github.com/shalevbublil/RobotVaccuum.git
   cd RobotVacuum
   ```
2. Build the project:
   ```bash
   dotnet build
   ```
3. Run the simulator:
   ```bash
   dotnet run
   ```

---

## 📺 Live Simulation Output Example

```text
--- Current Grid Map ---
H . . . . . . . . . 
. . . . . . . . . . 
. . . X X . . . . . 
. . . . . . . . . . 
. . . . . R . . . . 
. X X . . C . . . . 
. . . . . . . . . . 
. . . . . . X X . . 
. . . . . . . . . . 
. . . . . . . . . . 
------------------------
[Robot] [5s Timer] Checking Battery: 4%
[Robot] Battery low (< 5%). Starting automatic return home.
[Hardware-Cleaner] Suction stopped.
[Robot] Calculating shortest path from (4,5) to Home (0,0) using BFS...
[Robot] Path found! Shortest distance: 9 steps.
```
