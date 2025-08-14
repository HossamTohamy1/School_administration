using MediatR;
using School_managment.Features.Teachers.DTOs;

namespace School_managment.Features.Teachers.Commands
{
    public class UpdateTeacherCommand : IRequest<TeacherDto>
    {
        public UpdateTeacherDto Teacher { get; set; }
        public UpdateTeacherCommand(UpdateTeacherDto teacher)
        {
            Teacher = teacher;
        }
    }
}
