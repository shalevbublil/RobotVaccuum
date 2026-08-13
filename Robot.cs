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
            // TODO: לוגיקת הפעלה (יציאה אחורה, סיבוב והתחלת ניקוי)
            Console.WriteLine("Event received: evOn");
        }

        public void EvHome()
        {
            // TODO: לוגיקת חזרה הביתה יזומה
            Console.WriteLine("Event received: evHome");
        }

        public void EvMode()
        {
            // TODO: החלפה בין שאיבה לשטיפה
            Console.WriteLine("Event received: evMode");
        }

        public void EvStuck()
        {
            // TODO: מעבר למצב תקוע ושמירת המצב הקודם
            Console.WriteLine("Event received: evStuck");
        }

        public void EvService()
        {
            // TODO: חזרה מהתקלה למצב הקודם
            Console.WriteLine("Event received: evService");
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
