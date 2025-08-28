using MediatR;
using School_managment.Features.Timetables.DTOs;

namespace School_managment.Features.Timetables.Queries
{
    public class GetAllTimeTableQuery : IRequest<List<TimeTableDto>>
    {
        public int? ClassId { get; set; }
        public bool? IsActive { get; set; }

        public GetAllTimeTableQuery(int? classId = null, bool? isActive = null)
        {
            ClassId = classId;
            IsActive = isActive;
        }

    }
}
