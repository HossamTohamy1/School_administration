using MediatR;
using School_managment.Features.Classes.DTOs;

namespace School_managment.Features.Classes.Queries
{
    public record GetClassByldQuery(int Id) : IRequest<ClassDto>;
}
