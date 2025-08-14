using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Queries;
using School_managment.Features.Subjects.DTOs;
using School_managment.Infrastructure.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Classes.Handlers
{
    public class GetAllClassesHandler : IRequestHandler<GetAllClassesQuery, List<ClassDto>>
    {
        private readonly IRepository<Class> _repository;
        private readonly IMapper _mapper;

        public GetAllClassesHandler(IRepository<Class> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        // Handler
        public async Task<List<ClassDto>> Handle(GetAllClassesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAll()
                .ProjectTo<ClassDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }


    }
}
