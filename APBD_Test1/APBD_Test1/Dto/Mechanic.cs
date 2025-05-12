namespace APBD_Test1.Models;

public class Mechanic
{
    public int MechanicId { get; set; }
    public string LicenceNumber { get; set; }

    public Mechanic(int mechanicId, string licenceNumber)
    {
        MechanicId = mechanicId;
        LicenceNumber = licenceNumber;
    }

    public Mechanic()
    {
    }
}