using MediatR;

namespace School_managment.Features.Timetables.Commands
{
     public class DeleteTimeTableCommand : IRequest<bool>
    {
        public int Id { get; set; }

        public DeleteTimeTableCommand(int id)
        {
            Id = id;
        }
    }
    
}
