using MediatR;

namespace School_managment.Features.Teachers.Commands
{
    public class DeleteTeacherCommand : IRequest<bool>
    {
        public int Id { get; set; }
        public DeleteTeacherCommand(int id)
        {
            Id = id;
        }
    }
}
