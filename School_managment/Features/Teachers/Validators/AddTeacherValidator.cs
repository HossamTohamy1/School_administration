using FluentValidation;
using School_managment.Features.Teachers.DTOs;

namespace School_managment.Features.Teachers.Validators
{
    public class AddTeacherValidator : AbstractValidator<AddTeacherDto>
    {
        public AddTeacherValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Subject).NotEmpty().WithMessage("Subject is required.");
            RuleFor(x => x.WeeklyQuota).GreaterThan(0).WithMessage("WeeklyQuota must be greater than 0.");
        }
    }
}
