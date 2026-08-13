namespace RobotVacuum.Interfaces
{
    public interface IBattery
    {
    int GetChargePercent();
    void Charge();
    void StopCharge();
    
    }
}