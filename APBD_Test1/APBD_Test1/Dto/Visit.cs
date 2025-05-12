namespace APBD_Test1.Models;

public class Visit
{
    public int VisitId { get; set; }
    public DateTime VisitDate { get; set; }
    public Mechanic Mechanic { get; set; }
    public Client Client { get; set; }
    public List<Service> VisitServices { get; set; }
}