using MediatR;
using School_managment.Common.Models;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Teachers.DTOs;

namespace School_managment.Features.Classes.Queries
{
    // Features/Classes/GetAll/GetAllClassesQuery.cs
    public class GetAllClassesPageNumberQuery : IRequest<PagedResult<ClassDto>>
    {
        public int PageNumber { get; }
        public int PageSize { get; }

        public GetAllClassesPageNumberQuery(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
