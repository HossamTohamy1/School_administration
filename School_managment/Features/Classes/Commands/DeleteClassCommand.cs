using MediatR;

namespace School_managment.Features.Classes.Commands
{
 
    public record DeleteClassCommand(int Id) : IRequest<bool>;

}
