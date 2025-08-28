using FluentValidation;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.DTOs;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Validators
{
    public class AddTimeTableValidator : AbstractValidator<AddTimeTableDto>
    {
        private readonly SchoolDbContext _context;

        public AddTimeTableValidator(SchoolDbContext context)
        {
            _context = context;

            RuleFor(x => x.ClassId)
                .NotEmpty().WithMessage("ClassId is required.")
                .MustAsync(ClassExists).WithMessage("Class does not exist.");

            RuleFor(x => x.ScheduleId)
                .NotEmpty().WithMessage("ScheduleId is required.")
                .MaximumLength(100).WithMessage("ScheduleId cannot exceed 100 characters.")
                .MustAsync(BeUniqueScheduleId).WithMessage("ScheduleId must be unique.");

            RuleFor(x => x.TimetableSlots)
                .NotNull().WithMessage("TimetableSlots cannot be null.");

            RuleForEach(x => x.TimetableSlots).SetValidator(new AddTimetableSlotValidator(_context));

            RuleFor(x => x.TimetableSlots)
                .Must(HaveNoDuplicateSlots).WithMessage("Duplicate time slots are not allowed.");
        }

        private async Task<bool> ClassExists(int classId, CancellationToken cancellationToken)
        {
            return await _context.Classes.AnyAsync(c => c.Id == classId, cancellationToken);
        }

        private async Task<bool> BeUniqueScheduleId(string scheduleId, CancellationToken cancellationToken)
        {
            return !await _context.Timetables.AnyAsync(t => t.ScheduleId == scheduleId, cancellationToken);
        }

        private bool HaveNoDuplicateSlots(List<AddTimetableSlotDto> slots)
        {
            if (slots == null) return true;

            var duplicates = slots
                .GroupBy(s => new { s.Period, s.DayOfWeek })
                .Any(g => g.Count() > 1);

            return !duplicates;
        }
    
    }
}
