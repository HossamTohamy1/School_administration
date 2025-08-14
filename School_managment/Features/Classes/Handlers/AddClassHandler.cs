using AutoMapper;
using MediatR;
using School_managment.Common.Enums;
using School_managment.Features.Classes.Commands;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Infrastructure.Interface;
using School_managment.ViewModels; 

public class AddClassHandler : IRequestHandler<AddClassCommand, ResponseViewModel<ClassDto>>
{
    private readonly IClassRepository<Class> _classRepository;
    private readonly IMapper _mapper;

    public AddClassHandler(IClassRepository<Class> classRepository, IMapper mapper)
    {
        _classRepository = classRepository;
        _mapper = mapper;
    }

    public async Task<ResponseViewModel<ClassDto>> Handle(AddClassCommand request, CancellationToken cancellationToken)
    {
        if (await _classRepository.ClassExistsAsync(request.ClassDto.Grade, request.ClassDto.Section))
        {
            return new ErrorResponseViewModel<ClassDto>(
                $"Class {request.ClassDto.Grade}/{request.ClassDto.Section} already exists.",
                ErrorCode.DuplicateClass
            );
        }

        var entity = _mapper.Map<Class>(request.ClassDto);

        entity.ClassSubjects = await _classRepository.BuildClassSubjectsAsync(request.ClassDto.Subjects);

        await _classRepository.AddAsync(entity);

        var classDto = _mapper.Map<ClassDto>(entity);

        return new SuccessResponseViewModel<ClassDto>(classDto, "Class added successfully.");
    }
}
