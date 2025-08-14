using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Features.Classes.Models;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Infrastructure;
using School_managment.Infrastructure.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Teachers.Handlers
{
    public class UpdateTeacherHandler : IRequestHandler<UpdateTeacherCommand, TeacherDto>
    {
        private readonly IRepository<Teacher> _teacherRepository;
        private readonly IRepository<Class> _classRepository;
        private readonly IMapper _mapper;
        public UpdateTeacherHandler(IRepository<Teacher> teacherRepository, IRepository<Class> classRepository, IMapper mapper)
        {
            _teacherRepository = teacherRepository;
            _classRepository = classRepository;
            _mapper = mapper;
        }

        public async Task<TeacherDto> Handle(UpdateTeacherCommand request, CancellationToken cancellationToken)
        {
            var existingTeacher = await _teacherRepository.GetAll()
                .Include(t => t.ClassTeachers)
                .FirstOrDefaultAsync(t => t.Id == request.Teacher.Id, cancellationToken);

            if (existingTeacher == null)
                return null;

            _mapper.Map(request.Teacher, existingTeacher);

            existingTeacher.ClassTeachers ??= new List<ClassTeacher>();
            existingTeacher.ClassTeachers.Clear();

            if (request.Teacher.ClassIds?.Any() == true)
            {
                var classes = await _classRepository.GetAll()
                    .Where(c => request.Teacher.ClassIds.Contains(c.Id))
                    .ToListAsync(cancellationToken);

                foreach (var cls in classes)
                {
                    existingTeacher.ClassTeachers.Add(new ClassTeacher
                    {
                        ClassId = cls.Id,
                        Teacher = existingTeacher,
                        NameTeacher = existingTeacher.Name,
                        NameClass = $"{cls.Grade} {cls.Section}"
                    });
                }
            }

            await _teacherRepository.UpdateAsync(existingTeacher);

            var dto = _mapper.Map<TeacherDto>(existingTeacher);
            dto.ClassNames = existingTeacher.ClassTeachers
                .Select(ct => ct.NameClass)
                .ToList();

            return dto;
        }
    }
}
