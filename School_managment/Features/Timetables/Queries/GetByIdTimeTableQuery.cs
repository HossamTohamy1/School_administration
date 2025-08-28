using MediatR;
using School_managment.Features.Timetables.DTOs;

namespace School_managment.Features.Timetables.Queries
{
    public class GetByIdTimeTableQuery : IRequest<TimeTableDto>
    {
        public int Id { get; set; }

        public GetByIdTimeTableQuery(int id)
        {
            Id = id;
        }
    }
}
