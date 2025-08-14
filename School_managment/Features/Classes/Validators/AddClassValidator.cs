using FluentValidation;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Subjects.DTOs;

namespace School_managment.Features.Classes.Validators
{
    public class AddClassValidator : AbstractValidator<AddClassDto>
    {
        public AddClassValidator()
        {
            RuleFor(x => x.Grade).NotEmpty().WithMessage("Grade is required.");
            RuleFor(x => x.Section).NotEmpty().WithMessage("Section is required.");
            RuleFor(x => x.Subjects).NotEmpty().WithMessage("At least one subject is required.");
            //RuleForEach(x => x.Subjects).SetValidator(new SubjectValidator());
        }
    }

    public class SubjectValidator : AbstractValidator<SubjectDto>
    {
        public SubjectValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Subject name is required.");
            RuleFor(x => x.HoursPerWeek).InclusiveBetween(1, 10).WithMessage("Hours per week must be between 1 and 10.");
        }
    }
}
