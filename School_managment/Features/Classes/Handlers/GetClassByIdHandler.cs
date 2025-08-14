using AutoMapper;
using MediatR;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Queries;
using School_managment.Infrastructure.Interface;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Classes.Handlers
{
    public class GetClassByldHandler : IRequestHandler<GetClassByldQuery, ClassDto>
    {
        private readonly IRepository<Class> _repository;
        private readonly IMapper _mapper;

        public GetClassByldHandler(IRepository<Class> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ClassDto> Handle(GetClassByldQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsyncWithTracking(request.Id);
            return _mapper.Map<ClassDto>(entity);
        }

    }
}
