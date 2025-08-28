using Microsoft.AspNetCore.Mvc;
using School_managment.Features.Classes.Orchestrators;
using School_managment.Features.Classes.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Enums;
using School_managment.Common.Models;

namespace School_managment.Features.Classes.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassController : ControllerBase
    {
        private readonly ClassOrchestrator _orchestrator;

        public ClassController(ClassOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }
        [HttpGet]
        public async Task<ActionResult<List<ClassDto>>> GetAll()
        {
            var classes = await _orchestrator.GetAllClassesAsync();
            return Ok(classes);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ClassDto>>> GetPaged(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _orchestrator.GetAllClassesPageNumberAsync(pageNumber, pageSize);
            return Ok(result);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ClassDto>> GetById(int id)
        {
            var cls = await _orchestrator.GetClassByIdAsync(id);
            if (cls == null) return NotFound();
            return Ok(cls);
        }

        [HttpPost]
        public async Task<ActionResult<ClassDto>> Add(AddClassDto dto)
        {
            var response = await _orchestrator.AddClassAsync(dto);

            if (!response.IsSuccess)
            {
                if (response.ErrorCode == ErrorCode.DuplicateClass)
                    return Conflict(new { message = response.Message }); // 409
                else
                    return BadRequest(new { message = response.Message });
            }


            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Data.Id },
                response.Data
            );
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<ClassDto>> Update(int id, UpdateClassDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch");
            var updated = await _orchestrator.UpdateClassAsync(dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _orchestrator.DeleteClassAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
  


    }
}
