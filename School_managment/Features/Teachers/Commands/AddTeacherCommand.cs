using MediatR;
using School_managment.Features.Teachers.DTOs;

namespace School_managment.Features.Teachers.Commands
{
    public class AddTeacherCommand : IRequest<TeacherDto>
    {
        public AddTeacherDto Teacher { get; set; }
        public AddTeacherCommand(AddTeacherDto teacher)
        {
            Teacher = teacher;
        }
    }
}
