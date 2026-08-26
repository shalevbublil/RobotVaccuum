namespace RobotVacuum.Interfaces
{
    public interface ICleaner
    {
        void StartVacuum();
        void StartWashing();
        void Stop();

    }
}