using AutoMapper;
using MediatR;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Teachers.Queries;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using School_managment.Common.Models;
using School_managment.Infrastructure.Interface;

namespace School_managment.Features.Teachers.Handlers
{
    public class GetAllTeachersHandler : IRequestHandler<GetAllTeachersQuery, List<TeacherDto>>
    {
        private readonly IRepository<Teacher> _repository;
        private readonly IMapper _mapper;

        public GetAllTeachersHandler(IRepository<Teacher> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<List<TeacherDto>> Handle(GetAllTeachersQuery request, CancellationToken cancellationToken)
        {
            var teachers = await _repository.GetAll()
                .Select(t => new TeacherDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subject = t.Subject,
                    WeeklyQuota = t.WeeklyQuota,
                    RestrictedPeriods = t.RestrictedPeriods,
                    ClassNames = t.ClassTeachers.Select(ct => ct.NameClass).ToList()
                })
                .ToListAsync(cancellationToken);

            return teachers;
        }
  
        }

    }

