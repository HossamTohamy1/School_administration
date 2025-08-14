using School_managment.Common.Models;
using School_managment.Features.Classes.Models;
using School_managment.Features.Subjects.DTOs;
using School_managment.Infrastructure.Repositories;

namespace School_managment.Infrastructure.Interface
{
    public interface IClassRepository<T> : IRepository<T> where T : BaseEntity
    {
        Task RemoveClassSubjectsAsync(int classId);
        Task<List<ClassSubject>> BuildClassSubjectsAsync(IEnumerable<SimpleSubjectDto> subjects);
        Task LoadClassSubjectsAsync(Class entity);
        Task<bool> ClassExistsAsync(string grade, string section);


    }
}
