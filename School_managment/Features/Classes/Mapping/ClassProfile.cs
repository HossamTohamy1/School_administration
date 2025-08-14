using AutoMapper;
using School_managment.Features.Classes.DTOs;
using School_managment.Features.Classes.Models;
using School_managment.Features.Subjects.DTOs;
using School_managment.Features.Subjects.Models;
using School_managment.Common.Models;
using System.Linq;

namespace School_managment.Features.Classes.Mapping
{
    public class ClassProfile : Profile
    {
        public ClassProfile()
        {
            CreateMap<Subject, SubjectDto>();

            CreateMap<AddClassDto, Class>();

            CreateMap<SubjectDto, Subject>();

            CreateMap<UpdateClassDto, Class>();

            CreateMap<Class, ClassDto>()
                .ForMember(dest => dest.TeacherNames,
                    opt => opt.MapFrom(src =>
                        src.ClassTeachers.Select(ct => ct.Teacher.Name).ToList()
                    ))
                .ForMember(dest => dest.Subjects, opt => opt.MapFrom(src =>
                    src.ClassSubjects.Select(cs => cs.Subject)));

            CreateMap<ClassDto, Class>()
                .ForMember(dest => dest.ClassSubjects, opt => opt.Ignore())
                .AfterMap((src, dest) =>
                {
                    dest.ClassSubjects.Clear();

                    foreach (var subDto in src.Subjects)
                    {
                        dest.ClassSubjects.Add(new ClassSubject
                        {
                            Subject = new Subject
                            {
                                Name = subDto.Name,
                                HoursPerWeek = subDto.HoursPerWeek
                            }
                        });
                    }
                });

            CreateMap<Subject, SubjectDto>().ReverseMap();
            CreateMap<SimpleSubjectDto, Subject>().ReverseMap();

        }
    }
}
