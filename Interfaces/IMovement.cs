namespace RobotVacuum.Interfaces
{
    public interface IMovement
    {
        void GoForward();
        void GoBackward();
        void Stop();
        void Turn(int angle);
    }
}