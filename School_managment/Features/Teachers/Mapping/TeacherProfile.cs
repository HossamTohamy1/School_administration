using AutoMapper;
using School_managment.Features.Teachers.DTOs;
using School_managment.Features.Teachers.Models;

namespace School_managment.Features.Teachers.Mapping
{
    public class TeacherProfile : Profile
    {
        public TeacherProfile()
        {
            CreateMap<AddTeacherDto, Teacher>();
            CreateMap<UpdateTeacherDto, Teacher>();
            CreateMap<Teacher, TeacherDto>()
              .ForMember(
                  dest => dest.ClassNames,
                  opt => opt.MapFrom(src => src.ClassTeachers.Select(ct => ct.NameClass))
              );
        }
    }
}
