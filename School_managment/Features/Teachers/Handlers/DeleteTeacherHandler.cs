using MediatR;
using School_managment.Features.Teachers.Commands;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Classes.Models; // لو ClassTeachers هنا
using School_managment.Infrastructure.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using School_managment.Common.Models;

namespace School_managment.Features.Teachers.Handlers
{
    public class DeleteTeacherHandler : IRequestHandler<DeleteTeacherCommand, bool>
    {
        private readonly IRepository<Teacher> _teacherRepository;
        private readonly IRepository<ClassTeacher> _classTeacherRepository;

        public DeleteTeacherHandler(
            IRepository<Teacher> teacherRepository,
            IRepository<ClassTeacher> classTeacherRepository)
        {
            _teacherRepository = teacherRepository;
            _classTeacherRepository = classTeacherRepository;
        }

        public async Task<bool> Handle(DeleteTeacherCommand request, CancellationToken cancellationToken)
        {
            // جلب المدرس
            var existingTeacher = await _teacherRepository.GetByIdAsync(request.Id);
            if (existingTeacher == null)
                return false;

            // soft delete للمدرس
            existingTeacher.IsDeleted = true; // أو أي طريقة soft delete عندك
            await _teacherRepository.UpdateAsync(existingTeacher);

            // حذف الصفوف المرتبطة في ClassTeachers
            var relatedClassTeachers = _classTeacherRepository
                .GetAll() // لو الـ IRepository يدعم IQueryable
                .Where(ct => ct.TeacherId == request.Id)
                .ToList();

            foreach (var ct in relatedClassTeachers)
            {
                await _classTeacherRepository.HardDeleteAsync(ct);
            }

            return true;
        }
    }
}
