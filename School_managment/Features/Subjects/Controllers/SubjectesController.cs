using Microsoft.AspNetCore.Mvc;
using School_managment.Infrastructure; // DbContext
using School_managment.Features.Subjects.Models; // Subject model
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly SchoolDbContext _context;

    public SubjectsController(SchoolDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetAllSubjects()
    {
        var subjects = _context.Subjects
                               .Select(s => new { s.Id, s.Name }) 
                               .ToList();

        return Ok(subjects);
    }
}
