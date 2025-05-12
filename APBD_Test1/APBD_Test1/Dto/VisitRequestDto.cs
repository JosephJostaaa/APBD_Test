namespace APBD_Test1.Models;

public class VisitRequestDto
{
    public int VisitId { get; set; }
    public int ClientId { get; set; }
    public string MechanicLicenceNumber { get; set; }
    public List<ServiceRequestDto> ServiceRequests { get; set; } = new List<ServiceRequestDto>();
}