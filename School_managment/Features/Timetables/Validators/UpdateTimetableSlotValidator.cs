using FluentValidation;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.DTOs;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Validators
{
    public class UpdateTimetableSlotValidator : AbstractValidator<UpdateTimetableSlotDto>
    {
        private readonly SchoolDbContext _context;

        public UpdateTimetableSlotValidator(SchoolDbContext context)
        {
            _context = context;

            RuleFor(x => x.Period)
                .InclusiveBetween(1, 8).WithMessage("Period must be between 1 and 8.");

            RuleFor(x => x.DayOfWeek)
                .IsInEnum().WithMessage("Invalid DayOfWeek.");

            RuleFor(x => x.SubjectId)
                .MustAsync(SubjectExistsWhenProvided).WithMessage("Subject does not exist.")
                .When(x => x.SubjectId.HasValue);

            RuleFor(x => x.TeacherId)
                .MustAsync(TeacherExistsWhenProvided).WithMessage("Teacher does not exist.")
                .When(x => x.TeacherId.HasValue);
        }

        private async Task<bool> SubjectExistsWhenProvided(int? subjectId, CancellationToken cancellationToken)
        {
            if (!subjectId.HasValue) return true;
            return await _context.Subjects.AnyAsync(s => s.Id == subjectId.Value, cancellationToken);
        }

        private async Task<bool> TeacherExistsWhenProvided(int? teacherId, CancellationToken cancellationToken)
        {
            if (!teacherId.HasValue) return true;
            return await _context.Teachers.AnyAsync(t => t.Id == teacherId.Value, cancellationToken);
        }
    }
    
    
}
