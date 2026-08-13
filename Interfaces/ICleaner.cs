namespace RobotVacuum.Interfaces
{
    public interface ICleaner
    {
        void startVacuum();
        void startWashing();
        void stop();

    }
}