using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Teachers.Orchestrators;
using School_managment.Features.Teachers.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace School_managment.Features.Teachers.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly TeacherOrchestrator _orchestrator;

        public TeacherController(TeacherOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [HttpGet]
        public async Task<ActionResult<List<TeacherDto>>> GetAll()
        {
            var teachers = await _orchestrator.GetAllTeachersAsync();
            return Ok(teachers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherDto>> GetById(int id)
        {
            var teacher = await _orchestrator.GetTeacherByIdAsync(id);
            if (teacher == null)
                return NotFound();

            return Ok(teacher);
        }

        [HttpPost]
        public async Task<ActionResult<TeacherDto>> Add([FromBody] AddTeacherDto dto)
        {
            var createdTeacher = await _orchestrator.AddTeacherAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdTeacher.Id }, createdTeacher);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TeacherDto>> Update(int id, [FromBody] UpdateTeacherDto dto)
        {
            if (id != dto.Id)
                return BadRequest("Id mismatch");

            var updatedTeacher = await _orchestrator.UpdateTeacherAsync(dto);
            if (updatedTeacher == null)
                return NotFound();

            return Ok(updatedTeacher);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetTeachers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _orchestrator.GetTeachersAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _orchestrator.DeleteTeacherAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
       
    }

}

