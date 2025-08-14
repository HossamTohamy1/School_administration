using AutoMapper;
using MediatR;
using School_managment.Features.Classes.Commands;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Infrastructure.Interface;

public class UpdateClassHandler : IRequestHandler<UpdateClassCommand, ClassDto>
{
    private readonly IClassRepository<Class> _classRepository;
    private readonly IMapper _mapper;

    public UpdateClassHandler(IClassRepository<Class> classRepository, IMapper mapper)
    {
        _classRepository = classRepository;
        _mapper = mapper;
    }

    public async Task<ClassDto> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var dto = request.ClassDto;

        var entity = await _classRepository.GetByIdAsync(dto.Id);
        if (entity == null)
            throw new KeyNotFoundException($"Class with Id {dto.Id} not found.");

        await _classRepository.LoadClassSubjectsAsync(entity);

        entity.Grade = dto.Grade;
        entity.Section = dto.Section;
        entity.TotalHours = dto.TotalHours;

        entity.ClassSubjects.Clear();
        entity.ClassSubjects = await _classRepository.BuildClassSubjectsAsync(dto.Subjects);

        await _classRepository.UpdateAsync(entity);

        return _mapper.Map<ClassDto>(entity);
    }
}
