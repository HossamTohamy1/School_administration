using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.Commands;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Handlers
{
    public class AddTimeTableHandler : IRequestHandler<AddTimeTableCommand, TimeTableDto>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public AddTimeTableHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<TimeTableDto> Handle(AddTimeTableCommand request, CancellationToken cancellationToken)
        {
            var classId = request.TimeTable.ClassId;

            // ✅ Validate that class exists before proceeding
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId, cancellationToken);
            if (!classExists)
                throw new InvalidOperationException($"Class with ID {classId} does not exist.");

            // Deactivate existing active timetables for this class
            var existingActiveTimetables = await _context.Timetables
                .Where(t => t.ClassId == classId && t.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var timetable in existingActiveTimetables)
            {
                timetable.IsActive = false;
            }

            var timeTable = new TimeTable
            {
                ClassId = classId,
                ScheduleId = request.TimeTable.ScheduleId,
                GeneratedAt = DateTime.UtcNow,
                IsActive = true,
                Constraints = request.TimeTable.Constraints ?? "{}"
            };

            _context.Timetables.Add(timeTable);
            await _context.SaveChangesAsync(cancellationToken);

            // Add timetable slots
            foreach (var slotDto in request.TimeTable.TimetableSlots)
            {
                var slot = new TimetableSlot
                {
                    TimetableId = timeTable.Id,
                    ClassId = timeTable.ClassId, // ✅ أضف ده

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
            var createdTimeTable = await _context.Timetables
                .Include(t => t.Class)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots)
                    .ThenInclude(ts => ts.Teacher)
                .FirstOrDefaultAsync(t => t.Id == timeTable.Id, cancellationToken);

            return _mapper.Map<TimeTableDto>(createdTimeTable);
        }
    }
}
