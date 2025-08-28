using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Queries;
using School_managment.Infrastructure;

namespace School_managment.Features.Teachers.Handlers
{
    public class GetTeachersQueryHandler : IRequestHandler<GetTeachersQuery, PagedResult<TeacherDto>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetTeachersQueryHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PagedResult<TeacherDto>> Handle(GetTeachersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Teachers.AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Where(c => !c.IsDeleted)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<TeacherDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return new PagedResult<TeacherDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }
    }
}
