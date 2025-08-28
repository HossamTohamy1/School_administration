using MediatR;
using School_managment.Common.Models;
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

        public Task<TeacherDto> AddTeacherAsync(AddTeacherDto dto) =>
            _mediator.Send(new AddTeacherCommand(dto));

        public Task<TeacherDto> UpdateTeacherAsync(UpdateTeacherDto dto) =>
            _mediator.Send(new UpdateTeacherCommand(dto));

        public Task<bool> DeleteTeacherAsync(int id) =>
            _mediator.Send(new DeleteTeacherCommand(id));

        public Task<List<TeacherDto>> GetAllTeachersAsync() =>
            _mediator.Send(new GetAllTeachersQuery());

        public Task<TeacherDto> GetTeacherByIdAsync(int id) =>
            _mediator.Send(new GetTeacherByldQuery(id));

        public Task<PagedResult<TeacherDto>> GetTeachersAsync(int pageNumber, int pageSize) =>
            _mediator.Send(new GetTeachersQuery(pageNumber, pageSize));
    }
}
