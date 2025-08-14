using FluentValidation;
using School_managment.Features.Classes.DTOs;

namespace School_managment.Features.Classes.Validators
{
    public class UpdateClassValidator : AbstractValidator<UpdateClassDto>
    {
        public UpdateClassValidator() 
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.")
                .GreaterThan(0).WithMessage("Id must be greater than 0.");
        }

    }
}
