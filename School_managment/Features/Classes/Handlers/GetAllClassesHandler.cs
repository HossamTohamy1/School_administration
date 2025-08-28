using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Queries;
using School_managment.Features.Subjects.DTOs;
using School_managment.Infrastructure;

namespace School_managment.Features.Classes.Handlers
{
    public class GetAllClassesHandler : IRequestHandler<GetAllClassesQuery, List<ClassDto>>
    {
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public GetAllClassesHandler(SchoolDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ClassDto>> Handle(GetAllClassesQuery request, CancellationToken cancellationToken)
        {
            var classes = await _context.Classes
                .Where(c => !c.IsDeleted)
                .Include(c => c.ClassSubjects).ThenInclude(cs => cs.Subject)
                .Include(c => c.ClassTeachers).ThenInclude(ct => ct.Teacher)
                .ToListAsync(cancellationToken);

            var result = classes.Select(c => new ClassDto
            {
                Id = c.Id,
                Grade = c.Grade,
                Section = c.Section,
                TotalHours = c.TotalHours,
                Subjects = c.ClassSubjects.Select(cs => new SimpleSubjectDto
                {
                    Id = cs.Subject.Id,
                    Name = cs.Subject.Name,
                    HoursPerWeek = cs.Subject.HoursPerWeek
                }).ToList(),
                TeacherNames = c.ClassTeachers.Select(ct => ct.Teacher.Name).ToList()
            }).ToList();

            return result;
        }
    }
}
