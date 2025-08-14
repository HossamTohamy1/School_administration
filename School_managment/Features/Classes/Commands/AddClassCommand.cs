using MediatR;
using School_managment.Features.Classes.DTOs;
using School_managment.ViewModels;

namespace School_managment.Features.Classes.Commands
{
    public record AddClassCommand(AddClassDto ClassDto) : IRequest<ResponseViewModel<ClassDto>>;

}
