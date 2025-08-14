using MediatR;
using School_managment.Features.Teachers.DTOs;
using System.Collections.Generic;

namespace School_managment.Features.Teachers.Queries
{
    public class GetAllTeachersQuery : IRequest<List<TeacherDto>>
    {
    }
}
