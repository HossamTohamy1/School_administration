using MediatR;
using School_managment.Features.Classes.DTOs;

namespace School_managment.Features.Classes.Commands
{
    public record UpdateClassCommand(UpdateClassDto ClassDto) : IRequest<ClassDto>;

}
