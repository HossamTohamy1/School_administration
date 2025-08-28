using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.Commands;
using School_managment.Features.Timetables.DTOs;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Handlers
{
    public class UpdateTimeTableHandler : IRequestHandler<UpdateTimeTableCommand, TimeTableDto>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public UpdateTimeTableHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TimeTableDto> Handle(UpdateTimeTableCommand request, CancellationToken cancellationToken)
        {
            var timeTable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                .FirstOrDefaultAsync(t => t.Id == request.TimeTable.Id, cancellationToken);

            if (timeTable == null)
                throw new KeyNotFoundException($"TimeTable with ID {request.TimeTable.Id} not found.");

            // Validate that class exists
            var classExists = await _context.Classes.AnyAsync(c => c.Id == request.TimeTable.ClassId, cancellationToken);
            if (!classExists)
                throw new ArgumentException($"Class with ID {request.TimeTable.ClassId} does not exist.");

            // Update timetable properties
            timeTable.ClassId = request.TimeTable.ClassId;
            timeTable.ScheduleId = request.TimeTable.ScheduleId;
            timeTable.IsActive = request.TimeTable.IsActive;
            timeTable.Constraints = request.TimeTable.Constraints ?? "{}";

            // Remove existing slots
            _context.TimetableSlots.RemoveRange(timeTable.TimetableSlots);

            // Add updated slots
            foreach (var slotDto in request.TimeTable.TimetableSlots)
            {
                var slot = new Models.TimetableSlot
                {
                    TimetableId = timeTable.Id,
                    Period = slotDto.Period,
                    DayOfWeek = slotDto.DayOfWeek,
                    SubjectId = slotDto.SubjectId,
                    TeacherId = slotDto.TeacherId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimetableSlots.Add(slot);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Reload with related data
            var updatedTimeTable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Teacher)
                .FirstOrDefaultAsync(t => t.Id == timeTable.Id, cancellationToken);

            return _mapper.Map<TimeTableDto>(updatedTimeTable);
        }
    
    }
}
