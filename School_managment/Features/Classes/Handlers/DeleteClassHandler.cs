using MediatR;
using School_managment.Features.Classes.Commands;
using School_managment.Features.Classes.Models;
using School_managment.Infrastructure.Interface;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Classes.Handlers
{
    public class DeleteClassHandler : IRequestHandler<DeleteClassCommand, bool>
    {
        private readonly IRepository<Class> _repository;

        public DeleteClassHandler(IRepository<Class> repository)
        {
            _repository = repository;
        }

        public async Task<bool> Handle(DeleteClassCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null) return false;

            await _repository.DeleteAsync(entity);
            return true;
        }
    }
}
