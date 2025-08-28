using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Queries;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Handlers
{
    public class GetAllTimeTableHandler : IRequestHandler<GetAllTimeTableQuery, List<TimeTableDto>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetAllTimeTableHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<TimeTableDto>> Handle(GetAllTimeTableQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Teacher)
                .AsQueryable();

            if (request.ClassId.HasValue)
            {
                query = query.Where(t => t.ClassId == request.ClassId.Value);
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == request.IsActive.Value);
            }

            var timeTables = await query
                .OrderByDescending(t => t.GeneratedAt)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<TimeTableDto>>(timeTables);
        }
    }
    
    
}
