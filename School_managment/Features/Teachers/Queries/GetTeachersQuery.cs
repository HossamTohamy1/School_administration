using MediatR;
using School_managment.Common.Models;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;

namespace School_managment.Features.Teachers.Queries
{
    public class GetTeachersQuery : IRequest<PagedResult<TeacherDto>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public GetTeachersQuery(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
