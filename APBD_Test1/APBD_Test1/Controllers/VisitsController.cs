using APBD_Test1.Models;
using APBD_Test1.Repositories;
using APBD_Test1.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_Test1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VisitsController : ControllerBase
{
    private readonly IDbService _dbService;

    public VisitsController(IDbService visitRepository)
    {
        _dbService = visitRepository;
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetVisit(int id, CancellationToken cancellationToken)
    {
        return Ok(await _dbService.GetVisitByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddVisit([FromBody] VisitRequestDto visit, CancellationToken cancellationToken)
    {
        try
        {
            var res = await _dbService.AddVisitAsync(visit, cancellationToken);
            return CreatedAtAction(nameof(GetVisit), new { id = res }, res);
        }
        catch (Exception e)
        {
            return NotFound(e.Message);
        }
        
        
    }
}