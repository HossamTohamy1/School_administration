using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Queries;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Handlers
{
    public class GetByIdTimeTableHandler : IRequestHandler<GetByIdTimeTableQuery, TimeTableDto>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetByIdTimeTableHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TimeTableDto> Handle(GetByIdTimeTableQuery request, CancellationToken cancellationToken)
        {
            var timeTable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Teacher)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (timeTable == null)
                throw new KeyNotFoundException($"TimeTable with ID {request.Id} not found.");

            return _mapper.Map<TimeTableDto>(timeTable);
        }
    
    }
}
