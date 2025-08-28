using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.Commands;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Handlers
{
    public class DeleteTimeTableHandler : IRequestHandler<DeleteTimeTableCommand, bool>
    {
        private readonly SchoolDbContext _context;

        public DeleteTimeTableHandler(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteTimeTableCommand request, CancellationToken cancellationToken)
        {
            var timeTable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (timeTable == null)
                return false;

            // Remove all related slots first (cascade delete should handle this, but being explicit)
            _context.TimetableSlots.RemoveRange(timeTable.TimetableSlots);
            _context.Timetables.Remove(timeTable);

            var deletedRows = await _context.SaveChangesAsync(cancellationToken);
            return deletedRows > 0;
        }
    }
      
}
