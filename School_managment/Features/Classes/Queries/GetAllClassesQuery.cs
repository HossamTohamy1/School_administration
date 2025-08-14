using MediatR;
using School_managment.Features.Classes.DTOs;

namespace School_managment.Features.Classes.Queries
{
    public record GetAllClassesQuery() : IRequest<List<ClassDto>>;

}
