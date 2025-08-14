using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Teachers.Queries;
using School_managment.Infrastructure.Interface;
using System.Threading;
using System.Threading.Tasks;

namespace School_managment.Features.Teachers.Handlers
{
    public class GetTeacherByldHandler : IRequestHandler<GetTeacherByldQuery, TeacherDto>
    {
        private readonly IRepository<Teacher> _repository;
        private readonly IMapper _mapper;

        public GetTeacherByldHandler(IRepository<Teacher> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<TeacherDto> Handle(GetTeacherByldQuery request, CancellationToken cancellationToken)
        {
            var dto = await _repository.GetAll()
                .Where(t => t.Id == request.Id)
                .Select(t => new TeacherDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subject = t.Subject,
                    WeeklyQuota = t.WeeklyQuota,
                    RestrictedPeriods = t.RestrictedPeriods,
                    ClassNames = t.ClassTeachers.Select(ct => ct.NameClass).ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            return dto; 
        }


    }
}
