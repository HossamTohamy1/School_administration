using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Queries;
using School_managment.Features.Teachers.DTOs;
using School_managment.Infrastructure;

namespace School_managment.Features.Classes.Handlers
{
    public class GetAllClassesPageNumber : IRequestHandler<GetAllClassesPageNumberQuery, PagedResult<ClassDto>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetAllClassesPageNumber(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<ClassDto>> Handle(GetAllClassesPageNumberQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Classes.AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Section)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<ClassDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new PagedResult<ClassDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        
    }
}
