using MediatR;
using School_managment.Features.Teachers.DTOs;

namespace School_managment.Features.Teachers.Queries
{
    public class GetTeacherByldQuery : IRequest<TeacherDto>
    {
        public int Id { get; set; }
        public GetTeacherByldQuery(int id)
        {
            Id = id;
        }
    }
}
