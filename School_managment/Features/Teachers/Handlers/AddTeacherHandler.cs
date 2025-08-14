using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Features.Classes.Models;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Infrastructure.Interface;

public class AddTeacherHandler : IRequestHandler<AddTeacherCommand, TeacherDto>
{
    private readonly IRepository<Teacher> _teacherRepository;
    private readonly IRepository<Class> _classRepository;
    private readonly IMapper _mapper;

    public AddTeacherHandler(IRepository<Teacher> teacherRepository, IRepository<Class> classRepository, IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _classRepository = classRepository;
        _mapper = mapper;
    }

    public async Task<TeacherDto> Handle(AddTeacherCommand request, CancellationToken cancellationToken)
    {
        var teacher = _mapper.Map<Teacher>(request.Teacher);

        teacher.ClassTeachers ??= new List<ClassTeacher>();

        if (request.Teacher.ClassIds != null && request.Teacher.ClassIds.Any())
        {
            var classes = await _classRepository.GetAll()
                .Where(c => request.Teacher.ClassIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            foreach (var cls in classes)
            {
                teacher.ClassTeachers.Add(new ClassTeacher
                {
                    ClassId = cls.Id,
                    Teacher = teacher,
                    NameTeacher = teacher.Name,
                    NameClass = $"{cls.Grade} {cls.Section}"
                });
            }
        }

        await _teacherRepository.AddAsync(teacher);

        var dto = _mapper.Map<TeacherDto>(teacher);
        dto.ClassNames = teacher.ClassTeachers.Select(ct => ct.NameClass).ToList();

        return dto;
    }
}
