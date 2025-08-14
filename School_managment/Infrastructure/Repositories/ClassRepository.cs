using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Features.Classes.Models;
using School_managment.Features.Subjects.DTOs;
using School_managment.Features.Subjects.Models;
using School_managment.Infrastructure.Interface;

namespace School_managment.Infrastructure.Repositories
{
    public class ClassRepository : GeneralRepository<Class>, IClassRepository<Class>
    {
        private readonly SchoolDbContext _context;
        private readonly IRepository<Subject> _subjectRepository;

        public ClassRepository(SchoolDbContext context, IRepository<Subject> subjectRepository)
            : base(context)
        {
            _context = context;
            _subjectRepository = subjectRepository;
        }

        public async Task RemoveClassSubjectsAsync(int classId)
        {
            var oldRelations = await _context.Set<ClassSubject>()
                .Where(cs => cs.ClassId == classId)
                .ToListAsync();

            if (oldRelations.Any())
            {
                _context.Set<ClassSubject>().RemoveRange(oldRelations);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ClassSubject>> BuildClassSubjectsAsync(IEnumerable<SimpleSubjectDto> subjects)
        {
            var classSubjects = new List<ClassSubject>();

            var subjectIds = subjects.Where(s => s.Id > 0).Select(s => s.Id).ToList();
            var existingSubjects = await _subjectRepository
                .GetAll()
                .Where(s => subjectIds.Contains(s.Id))
                .ToListAsync();

            foreach (var s in subjects)
            {
                Subject subjectEntity;

                if (s.Id > 0) 
                {
                    subjectEntity = existingSubjects.FirstOrDefault(es => es.Id == s.Id)
                        ?? throw new KeyNotFoundException($"Subject with Id {s.Id} not found.");
                }
                else 
                {
                    subjectEntity = new Subject
                    {
                        Name = s.Name,
                        HoursPerWeek = s.HoursPerWeek
                    };
                }

                classSubjects.Add(new ClassSubject { Subject = subjectEntity });
            }

            return classSubjects;
        }
        public async Task LoadClassSubjectsAsync(Class entity)
        {
            await _context.Entry(entity)
                .Collection(c => c.ClassSubjects)
                .LoadAsync();
        }
        public async Task<bool> ClassExistsAsync(string grade, string section)
        {
            return await _context.Classes
                .AnyAsync(c => c.Grade == grade && c.Section == section);
        }

    }
}
