using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Queries;

namespace School_managment.Features.Teachers.Orchestrators
{
    public class TeacherOrchestrator
    {
        private readonly IMediator _mediator;

        public TeacherOrchestrator(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<TeacherDto> AddTeacherAsync(AddTeacherDto dto)
        {
            return await _mediator.Send(new AddTeacherCommand(dto));
        }

        public async Task<TeacherDto> UpdateTeacherAsync(UpdateTeacherDto dto)
        {
            return await _mediator.Send(new UpdateTeacherCommand(dto));
        }

        public async Task<bool> DeleteTeacherAsync(int id)
        {
            return await _mediator.Send(new DeleteTeacherCommand(id));
        }

        public async Task<List<TeacherDto>> GetAllTeachersAsync()
        {
            return await _mediator.Send(new GetAllTeachersQuery());
        }

        public async Task<TeacherDto> GetTeacherByIdAsync(int id)
        {
            return await _mediator.Send(new GetTeacherByldQuery(id));
        }
    }
}
