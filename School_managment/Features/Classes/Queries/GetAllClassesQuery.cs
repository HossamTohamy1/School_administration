using MediatR;
using School_managment.Features.Classes.DTOs;

namespace School_managment.Features.Classes.Queries
{
    public class GetAllClassesQuery : IRequest<List<ClassDto>>
    {
    }
}
