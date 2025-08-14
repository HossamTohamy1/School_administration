using MediatR;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.Models;
using School_managment.Infrastructure.Interface;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Teachers.Handlers
{
    public class DeleteTeacherHandler : IRequestHandler<DeleteTeacherCommand, bool>
    {
        private readonly IRepository<Teacher> _repository;

        public DeleteTeacherHandler(IRepository<Teacher> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteTeacherCommand request, CancellationToken cancellationToken)
        {
            var existingTeacher = await _repository.GetByIdAsync(request.Id);
            if (existingTeacher == null)
                return false;

            await _repository.DeleteAsync(existingTeacher);
            return true;
        }
    }
}
