using AutoMapper;
using global::School_managment.Features.Timetables.Commands;
using global::School_managment.Features.Timetables.DTOs;
using global::School_managment.Features.Timetables.Models;
using global::School_managment.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Teachers.Models;
using System.Linq;
using System.Text.Json;
using System.Xml;

namespace School_managment.Features.Timetables.Orchestrators
{
    public interface ITimeTableOrchestrator
    {
        Task<SmartTimetableGenerationResult> GenerateSmartTimetableAsync(int classId, SmartTimetableRequest request, CancellationToken cancellationToken = default);
        Task<List<ConflictDto>> ValidateTimetableAsync(int timetableId, CancellationToken cancellationToken = default);
        Task<TimetableStatisticsDto> GetTimetableStatisticsAsync(int timetableId, CancellationToken cancellationToken = default);
        Task<bool> SwapTimetableSlotsAsync(int timetableId, SwapSlotsRequest request, CancellationToken cancellationToken = default);
        Task<TeacherConflictResult> CheckTeacherConflictAsync(int teacherId, DayOfWeek dayOfWeek, int period, int classId, CancellationToken cancellationToken = default);
        Task<ConflictResolutionResult> ResolveTeacherConflictsAsync(int timetableId, List<ConflictDto> conflicts, CancellationToken cancellationToken = default);
        Task<List<AvailableTeacherDto>> GetAvailableTeachersForSlotAsync(int classId, DayOfWeek dayOfWeek, int period, CancellationToken cancellationToken = default);
        Task<SlotAssignmentResult> AssignTeacherToSlotAsync(SlotAssignmentRequest request, CancellationToken cancellationToken = default);
        Task<List<ConflictPreventionSuggestion>> GetConflictPreventionSuggestionsAsync(int classId, CancellationToken cancellationToken = default);
    }

    public class TimeTableOrchestrator : ITimeTableOrchestrator
    {
        private readonly IMediator _mediator;
        private readonly SchoolDbContext _context;
        private readonly IMapper _mapper;

        public TimeTableOrchestrator(IMediator mediator, SchoolDbContext context, IMapper mapper)
        {
            _mediator = mediator;
            _context = context;
            _mapper = mapper;
        }

        #region Smart Timetable Generation with Conflict Prevention

        /// <summary>
        /// Enhanced smart timetable generation with built-in conflict prevention
        /// </summary>
        public async Task<SmartTimetableGenerationResult> GenerateSmartTimetableAsync(
            int classId,
            SmartTimetableRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new SmartTimetableGenerationResult();

            try
            {
                // Pre-validation: Check if class has teacher assignments
                var classTeacherAssignments = await GetClassTeacherAssignmentsAsync(classId, cancellationToken);
                if (!classTeacherAssignments.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = $"No teacher assignments found for class {classId}";
                    return result;
                }

                // Load all necessary data upfront
                var generationContext = await PrepareGenerationContextAsync(classId, classTeacherAssignments, cancellationToken);

                // Generate timetable with conflict prevention
                var (slots, warnings, unassignedHours) = await GenerateConflictFreeSlots(
                    generationContext,
                    request,
                    cancellationToken);

                // Create and save timetable
                var timetableId = await CreateTimetableAsync(classId, slots, cancellationToken);

                // Get the created timetable for response
                var timetable = await GetTimetableWithDetailsAsync(timetableId, cancellationToken);

                result.Success = true;
                result.TimetableDto = _mapper.Map<TimeTableDto>(timetable);
                result.GenerationWarnings = warnings;
                result.UnassignedSubjectHours = unassignedHours;
                result.ConflictsGenerated = 0; // Should be minimal with our approach
                result.TotalSlotsGenerated = slots.Count;
                result.OptimizationSuggestions = GenerateOptimizationSuggestions(generationContext, slots);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to generate smart timetable: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Generate slots with comprehensive conflict prevention
        /// </summary>
        private async Task<(List<AddTimetableSlotDto> slots, List<string> warnings, Dictionary<string, int> unassignedHours)>
            GenerateConflictFreeSlots(
                TimetableGenerationContext context,
                SmartTimetableRequest request,
                CancellationToken cancellationToken)
        {
            var slots = new List<AddTimetableSlotDto>();
            var warnings = new List<string>();
            var unassignedHours = new Dictionary<string, int>();

            // Create subject pool based on hours per week
            var subjectPool = CreateSubjectPool(context.ClassTeacherAssignments);
            var shuffledPool = ShuffleSubjectPool(subjectPool);

            var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };

            // Track assignments to prevent over-scheduling
            var dailySubjectCount = new Dictionary<(DayOfWeek day, int subjectId), int>();

            foreach (var day in daysOfWeek)
            {
                for (int period = 1; period <= request.MaxPeriodsPerDay; period++)
                {
                    var availableAssignments = await GetAvailableAssignmentsForSlot(
                        shuffledPool,
                        context,
                        day,
                        period,
                        slots,
                        dailySubjectCount,
                        request.Constraints,
                        cancellationToken
                    );

                    if (availableAssignments.Any())
                    {
                        // Apply intelligent selection logic
                        var selectedAssignment = SelectBestAssignment(
                            availableAssignments,
                            day,
                            period,
                            dailySubjectCount,
                            request.Constraints
                        );

                        slots.Add(new AddTimetableSlotDto
                        {
                            Period = period,
                            DayOfWeek = day,
                            SubjectId = selectedAssignment.SubjectId,
                            TeacherId = selectedAssignment.TeacherId
                        });

                        // Update tracking
                        shuffledPool.Remove(selectedAssignment);
                        UpdateDailySubjectCount(dailySubjectCount, day, selectedAssignment.SubjectId);
                    }
                    else
                    {
                        // Log why no assignment was possible
                        warnings.Add($"No available teacher for {day} Period {period}");
                    }
                }
            }

            // Calculate unassigned hours
            unassignedHours = CalculateUnassignedHours(shuffledPool);

            return (slots, warnings, unassignedHours);
        }

        /// <summary>
        /// Get available assignments for a specific slot with all conflict checks
        /// </summary>
        private async Task<List<School_managment.Common.Models.ClassTeacher>> GetAvailableAssignmentsForSlot(
            List<School_managment.Common.Models.ClassTeacher> subjectPool,
            TimetableGenerationContext context,
            DayOfWeek day,
            int period,
            List<AddTimetableSlotDto> currentSlots,
            Dictionary<(DayOfWeek day, int subjectId), int> dailySubjectCount,
            TimetableConstraints constraints,
            CancellationToken cancellationToken)
        {
            var availableAssignments = new List<School_managment.Common.Models.ClassTeacher>();

            foreach (var assignment in subjectPool)
            {
                if (assignment.TeacherId == 0 || assignment.Teacher == null)
                    continue;

                // 1. Check teacher restricted periods
                if (constraints.RespectRestrictedPeriods && !IsTeacherAvailable(assignment.Teacher, day, period))
                    continue;

                // 2. Check teacher availability from database
                if (!IsTeacherAvailableFromDb(assignment.TeacherId, day, period, context.TeacherAvailabilities))
                    continue;

                // 3. Check current timetable conflicts (same teacher, same time)
                if (constraints.AvoidDoubleBooking && HasTeacherConflictInCurrentTimetable(currentSlots, assignment.TeacherId, day, period))
                    continue;

                // 4. Check cross-class conflicts (teacher teaching other classes)
                if (constraints.AvoidDoubleBooking && await HasTeacherConflictAcrossClassesAsync(assignment.TeacherId, day, period, context.CurrentClassId, cancellationToken))
                    continue;

                // 5. Check if we should spread subjects evenly
                if (constraints.SpreadSubjectsEvenly && ShouldSkipForEvenDistribution(dailySubjectCount, day, assignment.SubjectId))
                    continue;

                // 6. Check consecutive classes constraint
                if (!constraints.AllowConsecutiveClasses && HasConsecutiveConflict(currentSlots, assignment.SubjectId, day, period))
                    continue;

                availableAssignments.Add(assignment);
            }

            return availableAssignments;
        }

        /// <summary>
        /// Check if teacher has conflict across other classes with pre-loaded data
        /// </summary>
        private async Task<bool> HasTeacherConflictAcrossClassesAsync(
            int teacherId,
            DayOfWeek day,
            int period,
            int currentClassId,
            CancellationToken cancellationToken)
        {
            if (teacherId == 0) return false;

            var conflict = await _context.TimetableSlots
                .AnyAsync(ts =>
                    ts.TeacherId == teacherId &&
                    ts.DayOfWeek == day &&
                    ts.Period == period &&
                    ts.ClassId != currentClassId &&
                    ts.Timetable.IsActive,
                    cancellationToken);

            return conflict;
        }

        /// <summary>
        /// Intelligent assignment selection based on constraints and optimization
        /// </summary>
        private School_managment.Common.Models.ClassTeacher SelectBestAssignment(
            List<School_managment.Common.Models.ClassTeacher> availableAssignments,
            DayOfWeek day,
            int period,
            Dictionary<(DayOfWeek day, int subjectId), int> dailySubjectCount,
            TimetableConstraints constraints)
        {
            if (availableAssignments.Count == 1)
                return availableAssignments.First();

            // Priority 1: Balance workload if enabled
            if (constraints.BalanceWorkload)
            {
                // Choose teacher with least assignments so far
                var teacherWorkload = availableAssignments
                    .GroupBy(a => a.TeacherId)
                    .OrderBy(g => g.Count())
                    .First();

                if (teacherWorkload.Count() == 1)
                    return teacherWorkload.First();
            }

            // Priority 2: Spread subjects evenly
            if (constraints.SpreadSubjectsEvenly)
            {
                var leastScheduledSubject = availableAssignments
                    .OrderBy(a => dailySubjectCount.GetValueOrDefault((day, a.SubjectId), 0))
                    .First();

                return leastScheduledSubject;
            }

            // Default: Random selection for variety
            var random = new Random();
            return availableAssignments[random.Next(availableAssignments.Count)];
        }

        #endregion

        #region Slot Assignment and Availability Checking

        /// <summary>
        /// Get available teachers for a specific slot with detailed information
        /// </summary>
        public async Task<List<AvailableTeacherDto>> GetAvailableTeachersForSlotAsync(
            int classId,
            DayOfWeek dayOfWeek,
            int period,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all teachers assigned to this class
                var classTeachers = await _context.ClassTeachers
                    .Include(ct => ct.Teacher)
                    .Include(ct => ct.Subject)
                    .Where(ct => ct.ClassId == classId && ct.TeacherId != 0)
                    .ToListAsync(cancellationToken);

                var availableTeachers = new List<AvailableTeacherDto>();

                foreach (var ct in classTeachers)
                {
                    var teacher = ct.Teacher;
                    if (teacher == null) continue;

                    var availability = new AvailableTeacherDto
                    {
                        TeacherId = ct.TeacherId,
                        TeacherName = teacher.Name,
                        SubjectId = ct.SubjectId,
                        SubjectName = ct.Subject?.Name ?? "Unknown",
                        IsAvailable = true,
                        UnavailableReasons = new List<string>()
                    };

                    // Check all constraints
                    await CheckTeacherAvailabilityConstraints(availability, teacher, dayOfWeek, period, classId, cancellationToken);

                    availableTeachers.Add(availability);
                }

                return availableTeachers.OrderByDescending(t => t.IsAvailable).ThenBy(t => t.TeacherName).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error getting available teachers: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Comprehensive availability checking for a teacher
        /// </summary>
        private async Task CheckTeacherAvailabilityConstraints(
            AvailableTeacherDto availability,
            Teacher teacher,
            DayOfWeek dayOfWeek,
            int period,
            int classId,
            CancellationToken cancellationToken)
        {
            // Check restricted periods
            if (!IsTeacherAvailable(teacher, dayOfWeek, period))
            {
                availability.IsAvailable = false;
                availability.UnavailableReasons.Add("Teacher has restricted period");
            }

            // Check database availability
            var dbAvailability = await _context.TeacherAvailabilities
                .FirstOrDefaultAsync(ta =>
                    ta.TeacherId == availability.TeacherId &&
                    ta.DayOfWeek == dayOfWeek &&
                    ta.Period == period,
                    cancellationToken);

            if (dbAvailability?.IsAvailable == false)
            {
                availability.IsAvailable = false;
                availability.UnavailableReasons.Add("Teacher marked as unavailable");
            }

            // Check cross-class conflicts
            var hasConflict = await _context.TimetableSlots
                .AnyAsync(ts =>
                    ts.TeacherId == availability.TeacherId &&
                    ts.DayOfWeek == dayOfWeek &&
                    ts.Period == period &&
                    ts.ClassId != classId &&
                    ts.Timetable.IsActive,
                    cancellationToken);

            if (hasConflict)
            {
                availability.IsAvailable = false;
                availability.UnavailableReasons.Add("Teacher is assigned to another class at this time");
            }
        }

        /// <summary>
        /// Assign teacher to a specific slot with validation
        /// </summary>
        public async Task<SlotAssignmentResult> AssignTeacherToSlotAsync(
            SlotAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new SlotAssignmentResult();

            try
            {
                // Validate teacher can teach this subject in this class
                var isValidAssignment = await _context.ClassTeachers
                    .AnyAsync(ct =>
                        ct.ClassId == request.ClassId &&
                        ct.TeacherId == request.TeacherId &&
                        ct.SubjectId == request.SubjectId,
                        cancellationToken);

                if (!isValidAssignment)
                {
                    result.Success = false;
                    result.ErrorMessage = "Teacher is not assigned to teach this subject in this class";
                    return result;
                }

                // Check for conflicts
                var conflictCheck = await CheckTeacherConflictAsync(
                    request.TeacherId,
                    request.DayOfWeek,
                    request.Period,
                    request.ClassId,
                    cancellationToken);

                if (!conflictCheck.IsAvailable)
                {
                    result.Success = false;
                    result.ErrorMessage = conflictCheck.ConflictMessage;
                    result.ConflictDetails = conflictCheck;
                    return result;
                }

                // Check if slot already exists and update or create
                var existingSlot = await _context.TimetableSlots
                    .FirstOrDefaultAsync(ts =>
                        ts.TimetableId == request.TimetableId &&
                        ts.DayOfWeek == request.DayOfWeek &&
                        ts.Period == request.Period,
                        cancellationToken);

                if (existingSlot != null)
                {
                    existingSlot.TeacherId = request.TeacherId;
                    existingSlot.SubjectId = request.SubjectId;
                }
                else
                {
                    var newSlot = new TimetableSlot
                    {
                        TimetableId = request.TimetableId,
                        ClassId = request.ClassId,
                        Period = request.Period,
                        DayOfWeek = request.DayOfWeek,
                        SubjectId = request.SubjectId,
                        TeacherId = request.TeacherId
                    };
                    _context.TimetableSlots.Add(newSlot);
                }

                await _context.SaveChangesAsync(cancellationToken);

                result.Success = true;
                result.Message = "Teacher assigned successfully";

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Error assigning teacher: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region Conflict Prevention Suggestions

        /// <summary>
        /// Get suggestions for preventing conflicts during manual assignment
        /// </summary>
        public async Task<List<ConflictPreventionSuggestion>> GetConflictPreventionSuggestionsAsync(
            int classId,
            CancellationToken cancellationToken = default)
        {
            var suggestions = new List<ConflictPreventionSuggestion>();

            try
            {
                // Get class assignments
                var classTeachers = await _context.ClassTeachers
                    .Include(ct => ct.Teacher)
                    .Include(ct => ct.Subject)
                    .Where(ct => ct.ClassId == classId)
                    .ToListAsync(cancellationToken);

                // Check for unassigned subjects
                var unassignedSubjects = classTeachers.Where(ct => ct.TeacherId == 0).ToList();
                if (unassignedSubjects.Any())
                {
                    suggestions.Add(new ConflictPreventionSuggestion
                    {
                        Type = "UnassignedSubjects",
                        Priority = "High",
                        Description = $"You have {unassignedSubjects.Count} subjects without assigned teachers",
                        AffectedSubjects = unassignedSubjects.Select(us => us.Subject?.Name ?? "Unknown").ToList(),
                        Recommendation = "Assign teachers to these subjects before generating timetable"
                    });
                }

                // Check for teachers with restricted periods
                var teachersWithRestrictions = classTeachers
                    .Where(ct => ct.Teacher?.RestrictedPeriods?.Any() == true)
                    .ToList();

                if (teachersWithRestrictions.Any())
                {
                    suggestions.Add(new ConflictPreventionSuggestion
                    {
                        Type = "RestrictedPeriods",
                        Priority = "Medium",
                        Description = $"{teachersWithRestrictions.Count} teachers have restricted periods",
                        AffectedTeachers = teachersWithRestrictions.Select(t => t.Teacher?.Name ?? "Unknown").ToList(),
                        Recommendation = "Review teacher availabilities to maximize scheduling flexibility"
                    });
                }

                // Check for overloaded teachers (teaching too many subjects)
                var overloadedTeachers = classTeachers
                    .Where(ct => ct.TeacherId != 0)
                    .GroupBy(ct => ct.TeacherId)
                    .Where(g => g.Sum(ct => ct.Subject?.HoursPerWeek ?? 0) > 20) // More than 20 hours per week
                    .ToList();

                if (overloadedTeachers.Any())
                {
                    suggestions.Add(new ConflictPreventionSuggestion
                    {
                        Type = "TeacherOverload",
                        Priority = "Medium",
                        Description = $"{overloadedTeachers.Count} teachers may be overloaded",
                        Recommendation = "Consider redistributing subjects to balance teacher workload"
                    });
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating suggestions: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Prepare all necessary data for timetable generation
        /// </summary>
        private async Task<TimetableGenerationContext> PrepareGenerationContextAsync(
            int classId,
            List<School_managment.Common.Models.ClassTeacher> classTeacherAssignments,
            CancellationToken cancellationToken)
        {
            var teacherIds = classTeacherAssignments
                .Where(ct => ct.TeacherId != 0)
                .Select(ct => ct.TeacherId)
                .Distinct()
                .ToList();

            var teacherAvailabilities = await _context.TeacherAvailabilities
                .Where(ta => teacherIds.Contains(ta.TeacherId))
                .ToListAsync(cancellationToken);

            var existingSlots = await _context.TimetableSlots
                .Include(ts => ts.Timetable)
                .Where(ts => ts.Timetable.IsActive &&
                             ts.TeacherId != 0 &&
                             teacherIds.Contains(ts.TeacherId ?? 0))
                .ToListAsync(cancellationToken);

            return new TimetableGenerationContext
            {
                CurrentClassId = classId,
                ClassTeacherAssignments = classTeacherAssignments,
                TeacherAvailabilities = teacherAvailabilities,
                ExistingActiveSlots = existingSlots
            };
        }

        private async Task<List<School_managment.Common.Models.ClassTeacher>> GetClassTeacherAssignmentsAsync(
            int classId,
            CancellationToken cancellationToken)
        {
            return await _context.ClassTeachers
                .Include(ct => ct.Teacher)
                .Include(ct => ct.Subject)
                .Where(ct => ct.ClassId == classId)
                .ToListAsync(cancellationToken);
        }

        private List<School_managment.Common.Models.ClassTeacher> CreateSubjectPool(
            List<School_managment.Common.Models.ClassTeacher> assignments)
        {
            var pool = new List<School_managment.Common.Models.ClassTeacher>();

            foreach (var assignment in assignments.Where(a => a.TeacherId != 0 && a.Teacher != null))
            {
                if (assignment.Subject != null)
                {
                    for (int i = 0; i < assignment.Subject.HoursPerWeek; i++)
                    {
                        pool.Add(assignment);
                    }
                }
            }

            return pool;
        }

        private List<School_managment.Common.Models.ClassTeacher> ShuffleSubjectPool(
            List<School_managment.Common.Models.ClassTeacher> pool)
        {
            var random = new Random();
            return pool.OrderBy(x => random.Next()).ToList();
        }

        private void UpdateDailySubjectCount(
            Dictionary<(DayOfWeek day, int subjectId), int> dailyCount,
            DayOfWeek day,
            int subjectId)
        {
            var key = (day, subjectId);
            dailyCount[key] = dailyCount.GetValueOrDefault(key, 0) + 1;
        }

        private bool ShouldSkipForEvenDistribution(
            Dictionary<(DayOfWeek day, int subjectId), int> dailyCount,
            DayOfWeek day,
            int subjectId)
        {
            // Skip if this subject already has 2+ periods today
            return dailyCount.GetValueOrDefault((day, subjectId), 0) >= 2;
        }

        private bool HasConsecutiveConflict(
            List<AddTimetableSlotDto> slots,
            int subjectId,
            DayOfWeek day,
            int period)
        {
            return slots.Any(s =>
                s.SubjectId == subjectId &&
                s.DayOfWeek == day &&
                Math.Abs(s.Period - period) == 1);
        }

        private Dictionary<string, int> CalculateUnassignedHours(
            List<School_managment.Common.Models.ClassTeacher> remainingPool)
        {
            return remainingPool
                .Where(r => r.Subject != null)
                .GroupBy(r => r.Subject.Name)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private List<string> GenerateOptimizationSuggestions(
            TimetableGenerationContext context,
            List<AddTimetableSlotDto> slots)
        {
            var suggestions = new List<string>();

            // Check for unbalanced days
            var dailyDistribution = slots.GroupBy(s => s.DayOfWeek).ToDictionary(g => g.Key, g => g.Count());
            var maxDaily = dailyDistribution.Values.Max();
            var minDaily = dailyDistribution.Values.Min();

            if (maxDaily - minDaily > 2)
            {
                suggestions.Add("Consider redistributing subjects for more balanced daily schedules");
            }

            // Check for teacher workload balance
            var teacherWorkload = slots.GroupBy(s => s.TeacherId).ToDictionary(g => g.Key, g => g.Count());
            if (teacherWorkload.Values.Any() && teacherWorkload.Values.Max() - teacherWorkload.Values.Min() > 3)
            {
                suggestions.Add("Some teachers have significantly more periods than others");
            }

            return suggestions;
        }

        private async Task<int> CreateTimetableAsync(
            int classId,
            List<AddTimetableSlotDto> slots,
            CancellationToken cancellationToken)
        {
            var createCommand = new CreateTimetableCommand
            {
                ClassId = classId,
                Name = $"Smart Timetable for Class {classId} - {DateTime.Now:yyyy-MM-dd}",
                Slots = slots
            };

            return await _mediator.Send(createCommand, cancellationToken);
        }

        private async Task<TimeTable> GetTimetableWithDetailsAsync(
            int timetableId,
            CancellationToken cancellationToken)
        {
            return await _context.Timetables
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Teacher)
                .Include(t => t.Class)
                .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);
        }

        #endregion

        #region Original Methods (Updated for consistency)

        private bool IsTeacherAvailable(Teacher teacher, DayOfWeek day, int period)
        {
            if (teacher?.RestrictedPeriods == null || !teacher.RestrictedPeriods.Any())
                return true;

            string currentDayKey = day switch
            {
                DayOfWeek.Sunday => "Sun",
                DayOfWeek.Monday => "Mon",
                DayOfWeek.Tuesday => "Tue",
                DayOfWeek.Wednesday => "Wed",
                DayOfWeek.Thursday => "Thu",
                DayOfWeek.Friday => "Fri",
                DayOfWeek.Saturday => "Sat",
                _ => day.ToString().Substring(0, 3)
            };

            foreach (var restrictedPeriod in teacher.RestrictedPeriods)
            {
                var parts = restrictedPeriod.Trim().Split('-');
                if (parts.Length != 2) continue;

                var dayKey = parts[0].Trim();
                if (!int.TryParse(parts[1].Trim(), out int restrictedPeriodNumber)) continue;

                if (string.Equals(dayKey, currentDayKey, StringComparison.OrdinalIgnoreCase) &&
                    restrictedPeriodNumber == period)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsTeacherAvailableFromDb(
            int teacherId,
            DayOfWeek day,
            int period,
            List<TeacherAvailability> availabilities)
        {
            if (teacherId == 0) return false;

            var availability = availabilities.FirstOrDefault(a =>
                a.TeacherId == teacherId &&
                a.DayOfWeek == day &&
                a.Period == period);

            return availability?.IsAvailable ?? true;
        }

        private bool HasTeacherConflictInCurrentTimetable(
            List<AddTimetableSlotDto> existingSlots,
            int teacherId,
            DayOfWeek day,
            int period)
        {
            if (teacherId == 0) return false;

            return existingSlots.Any(s =>
                s.TeacherId == teacherId &&
                s.DayOfWeek == day &&
                s.Period == period);
        }

        public async Task<TeacherConflictResult> CheckTeacherConflictAsync(
            int teacherId,
            DayOfWeek dayOfWeek,
            int period,
            int classId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(teacherId);
                if (teacher == null)
                {
                    return new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictMessage = "Teacher not found"
                    };
                }

                // Check restricted periods
                if (!IsTeacherAvailable(teacher, dayOfWeek, period))
                {
                    return new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictMessage = $"Teacher has restricted period on {dayOfWeek} Period {period}"
                    };
                }

                // Check database availability
                var availability = await _context.TeacherAvailabilities
                    .FirstOrDefaultAsync(ta =>
                        ta.TeacherId == teacherId &&
                        ta.DayOfWeek == dayOfWeek &&
                        ta.Period == period,
                        cancellationToken);

                if (availability?.IsAvailable == false)
                {
                    return new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictMessage = $"Teacher is marked as unavailable on {dayOfWeek} Period {period}"
                    };
                }

                // Check cross-class conflicts
                var conflictingSlots = await _context.TimetableSlots
                    .Include(ts => ts.Class)
                    .Include(ts => ts.Subject)
                    .Include(ts => ts.Timetable)
                    .Where(ts => ts.TeacherId == teacherId &&
                                 ts.DayOfWeek == dayOfWeek &&
                                 ts.Period == period &&
                                 ts.ClassId != classId &&
                                 ts.Timetable.IsActive)
                    .ToListAsync(cancellationToken);

                if (conflictingSlots.Any())
                {
                    var conflictingSlot = conflictingSlots.First();
                    return new TeacherConflictResult
                    {
                        IsAvailable = false,
                        ConflictingClassId = conflictingSlot.ClassId,
                        ConflictingClassName = $"{conflictingSlot.Class?.Grade}/{conflictingSlot.Class?.Section}",
                        ConflictingSubjectName = conflictingSlot.Subject?.Name,
                        ConflictMessage = $"Teacher is already assigned to {conflictingSlot.Class?.Grade}/{conflictingSlot.Class?.Section} " +
                                         $"for {conflictingSlot.Subject?.Name} on {dayOfWeek} Period {period}"
                    };
                }

                return new TeacherConflictResult { IsAvailable = true };
            }
            catch (Exception ex)
            {
                return new TeacherConflictResult
                {
                    IsAvailable = false,
                    ConflictMessage = $"Error checking teacher availability: {ex.Message}"
                };
            }
        }

        public async Task<List<ConflictDto>> ValidateTimetableAsync(int timetableId, CancellationToken cancellationToken = default)
        {
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Teacher)
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Class)
                .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

            if (timetable == null)
                throw new KeyNotFoundException($"Timetable with ID {timetableId} not found.");

            var conflicts = new List<ConflictDto>();

            // 1. Check internal teacher conflicts
            var internalTeacherConflicts = timetable.TimetableSlots
                .Where(ts => ts.TeacherId != 0)
                .GroupBy(ts => new { ts.TeacherId, ts.DayOfWeek, ts.Period })
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.Select(ts => new ConflictDto
                {
                    Type = "TeacherDoubleBooking",
                    Description = $"Teacher {ts.Teacher?.Name} has multiple classes at {ts.DayOfWeek} Period {ts.Period} in same timetable",
                    SlotId = ts.Id,
                    DayOfWeek = ts.DayOfWeek,
                    Period = ts.Period
                }));

            conflicts.AddRange(internalTeacherConflicts);

            // 2. Check cross-class teacher conflicts
            foreach (var slot in timetable.TimetableSlots.Where(ts => ts.TeacherId != 0))
            {
                var conflictingSlots = await _context.TimetableSlots
                    .Include(ts => ts.Class)
                    .Include(ts => ts.Subject)
                    .Include(ts => ts.Timetable)
                    .Where(ts => ts.TeacherId == slot.TeacherId &&
                                ts.DayOfWeek == slot.DayOfWeek &&
                                ts.Period == slot.Period &&
                                ts.ClassId != slot.ClassId &&
                                ts.Id != slot.Id &&
                                ts.Timetable.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var conflictSlot in conflictingSlots)
                {
                    conflicts.Add(new ConflictDto
                    {
                        Type = "TeacherCrossClassDoubleBooking",
                        Description = $"Teacher {slot.Teacher?.Name} is assigned to both {slot.Class?.Grade}/{slot.Class?.Section} and {conflictSlot.Class?.Grade}/{conflictSlot.Class?.Section} at {slot.DayOfWeek} Period {slot.Period}",
                        SlotId = slot.Id,
                        DayOfWeek = slot.DayOfWeek,
                        Period = slot.Period
                    });
                }
            }

            // 3. Check restricted periods
            var restrictedConflicts = timetable.TimetableSlots
                .Where(ts => ts.Teacher != null && !IsTeacherAvailable(ts.Teacher, ts.DayOfWeek, ts.Period))
                .Select(ts => new ConflictDto
                {
                    Type = "RestrictedPeriod",
                    Description = $"Teacher {ts.Teacher?.Name} cannot teach at {ts.DayOfWeek} Period {ts.Period} (restricted period).",
                    SlotId = ts.Id,
                    DayOfWeek = ts.DayOfWeek,
                    Period = ts.Period
                });

            conflicts.AddRange(restrictedConflicts);

            // 4. Check teacher-subject assignments
            var classId = timetable.ClassId;
            var assignedCombinations = await _context.ClassTeachers
                .Where(cs => cs.ClassId == classId)
                .Select(cs => new { cs.SubjectId, cs.TeacherId })
                .ToListAsync(cancellationToken);

            var invalidAssignments = timetable.TimetableSlots
                .Where(ts => ts.SubjectId != null && ts.TeacherId != 0)
                .Where(ts => !assignedCombinations.Any(ac =>
                    ac.SubjectId == ts.SubjectId && ac.TeacherId == ts.TeacherId))
                .Select(ts => new ConflictDto
                {
                    Type = "InvalidTeacherSubjectAssignment",
                    Description = $"Teacher {ts.Teacher?.Name} is not assigned to teach {ts.Subject?.Name} in this class",
                    SlotId = ts.Id,
                    DayOfWeek = ts.DayOfWeek,
                    Period = ts.Period
                });

            conflicts.AddRange(invalidAssignments);

            return conflicts;
        }

        public async Task<TimetableStatisticsDto> GetTimetableStatisticsAsync(int timetableId, CancellationToken cancellationToken = default)
        {
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Subject)
                .Include(t => t.TimetableSlots).ThenInclude(ts => ts.Teacher)
                .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

            if (timetable == null)
                throw new KeyNotFoundException($"Timetable with ID {timetableId} not found.");

            var stats = new TimetableStatisticsDto
            {
                TimetableId = timetableId,
                TotalSlots = timetable.TimetableSlots.Count,
                FilledSlots = timetable.TimetableSlots.Count(ts => ts.SubjectId != null),
                EmptySlots = timetable.TimetableSlots.Count(ts => ts.SubjectId == null),
                SubjectDistribution = timetable.TimetableSlots
                    .Where(ts => ts.Subject != null)
                    .GroupBy(ts => ts.Subject.Name)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TeacherWorkload = timetable.TimetableSlots
                    .Where(ts => ts.Teacher != null && ts.TeacherId != 0)
                    .GroupBy(ts => ts.Teacher.Name)
                    .ToDictionary(g => g.Key, g => g.Count()),
                DailyDistribution = timetable.TimetableSlots
                    .Where(ts => ts.SubjectId != null)
                    .GroupBy(ts => ts.DayOfWeek)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };

            return stats;
        }

        public async Task<bool> SwapTimetableSlotsAsync(int timetableId, SwapSlotsRequest request, CancellationToken cancellationToken = default)
        {
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

            if (timetable == null)
                throw new KeyNotFoundException($"Timetable with ID {timetableId} not found.");

            var slot1 = timetable.TimetableSlots.FirstOrDefault(ts =>
                ts.DayOfWeek == request.Slot1.DayOfWeek && ts.Period == request.Slot1.Period);
            var slot2 = timetable.TimetableSlots.FirstOrDefault(ts =>
                ts.DayOfWeek == request.Slot2.DayOfWeek && ts.Period == request.Slot2.Period);

            if (slot1 == null || slot2 == null)
                return false;

            (slot1.SubjectId, slot2.SubjectId) = (slot2.SubjectId, slot1.SubjectId);
            (slot1.TeacherId, slot2.TeacherId) = (slot2.TeacherId, slot1.TeacherId);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ConflictResolutionResult> ResolveTeacherConflictsAsync(
            int timetableId,
            List<ConflictDto> conflicts,
            CancellationToken cancellationToken = default)
        {
            var timetable = await _context.Timetables
                .Include(t => t.TimetableSlots)
                .ThenInclude(ts => ts.Teacher)
                .FirstOrDefaultAsync(t => t.Id == timetableId, cancellationToken);

            if (timetable == null)
                throw new KeyNotFoundException($"Timetable with ID {timetableId} not found");

            int resolvedCount = 0;
            int unresolvedCount = 0;
            var resolutionMessages = new List<string>();

            var doubleBookingConflicts = conflicts.Where(c =>
                c.Type == "TeacherDoubleBooking" ||
                c.Type == "TeacherCrossClassDoubleBooking").ToList();

            foreach (var conflict in doubleBookingConflicts)
            {
                try
                {
                    var conflictingSlot = timetable.TimetableSlots
                        .FirstOrDefault(ts => ts.Id == conflict.SlotId);

                    if (conflictingSlot == null)
                    {
                        unresolvedCount++;
                        continue;
                    }

                    var alternativeSlot = await FindAlternativeSlotAsync(
                        conflictingSlot.TeacherId ?? 0,
                        conflictingSlot.SubjectId,
                        conflictingSlot.ClassId,
                        conflictingSlot.DayOfWeek,
                        conflictingSlot.Period,
                        timetable.TimetableSlots.ToList(),
                        cancellationToken
                    );

                    if (alternativeSlot != null)
                    {
                        conflictingSlot.DayOfWeek = alternativeSlot.Value.DayOfWeek;
                        conflictingSlot.Period = alternativeSlot.Value.Period;

                        resolvedCount++;
                        resolutionMessages.Add(
                            $"Moved {conflictingSlot.Subject?.Name} to {alternativeSlot.Value.DayOfWeek} Period {alternativeSlot.Value.Period}"
                        );
                    }
                    else
                    {
                        _context.TimetableSlots.Remove(conflictingSlot);
                        resolvedCount++;
                        resolutionMessages.Add(
                            $"Removed conflicting slot: {conflictingSlot.Subject?.Name} on {conflictingSlot.DayOfWeek} Period {conflictingSlot.Period}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    unresolvedCount++;
                    resolutionMessages.Add($"Error resolving conflict: {ex.Message}");
                }
            }

            if (resolvedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new ConflictResolutionResult
            {
                Success = resolvedCount > 0,
                Message = resolvedCount > 0
                    ? $"Resolved {resolvedCount} conflict(s), {unresolvedCount} unresolved"
                    : $"Could not resolve any conflicts",
                ResolvedCount = resolvedCount,
                UnresolvedCount = unresolvedCount,
                ResolutionDetails = resolutionMessages
            };
        }

        private async Task<(DayOfWeek DayOfWeek, int Period)?> FindAlternativeSlotAsync(
            int teacherId,
            int? subjectId,
            int classId,
            DayOfWeek originalDay,
            int originalPeriod,
            List<TimetableSlot> existingSlots,
            CancellationToken cancellationToken = default)
        {
            if (teacherId == 0) return null;

            var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };
            var teacher = await _context.Teachers.FindAsync(teacherId);

            if (teacher == null) return null;

            foreach (var day in daysOfWeek)
            {
                for (int period = 1; period <= 8; period++)
                {
                    if (day == originalDay && period == originalPeriod)
                        continue;

                    if (existingSlots.Any(ts => ts.DayOfWeek == day && ts.Period == period))
                        continue;

                    if (!IsTeacherAvailable(teacher, day, period))
                        continue;

                    var conflictingSlots = await _context.TimetableSlots
                        .Include(ts => ts.Timetable)
                        .Where(ts => ts.TeacherId == teacherId &&
                                     ts.DayOfWeek == day &&
                                     ts.Period == period &&
                                     ts.ClassId != classId &&
                                     ts.Timetable.IsActive)
                        .AnyAsync(cancellationToken);

                    if (conflictingSlots)
                        continue;

                    return (day, period);
                }
            }

            return null;
        }

        #endregion
    }

    #region Supporting Classes and DTOs

    public class TimetableGenerationContext
    {
        public int CurrentClassId { get; set; }
        public List<School_managment.Common.Models.ClassTeacher> ClassTeacherAssignments { get; set; } = new();
        public List<TeacherAvailability> TeacherAvailabilities { get; set; } = new();
        public List<TimetableSlot> ExistingActiveSlots { get; set; } = new();
    }

    public class SmartTimetableGenerationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeTableDto? TimetableDto { get; set; }
        public List<string> GenerationWarnings { get; set; } = new();
        public Dictionary<string, int> UnassignedSubjectHours { get; set; } = new();
        public int ConflictsGenerated { get; set; }
        public int TotalSlotsGenerated { get; set; }
        public List<string> OptimizationSuggestions { get; set; } = new();
    }

    public class AvailableTeacherDto
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public List<string> UnavailableReasons { get; set; } = new();
    }

    public class SlotAssignmentRequest
    {
        public int TimetableId { get; set; }
        public int ClassId { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class SlotAssignmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public TeacherConflictResult? ConflictDetails { get; set; }
    }

    public class ConflictPreventionSuggestion
    {
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public List<string> AffectedSubjects { get; set; } = new();
        public List<string> AffectedTeachers { get; set; } = new();
    }

    public class ConflictResolutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ResolvedCount { get; set; }
        public int UnresolvedCount { get; set; }
        public List<string> ResolutionDetails { get; set; } = new();
    }

    public class SmartTimetableRequest
    {
        public int MaxPeriodsPerDay { get; set; } = 8;
        public TimetableConstraints Constraints { get; set; } = new();
    }

    public class TimetableConstraints
    {
        public bool AvoidDoubleBooking { get; set; } = true;
        public bool SpreadSubjectsEvenly { get; set; } = true;
        public bool RespectRestrictedPeriods { get; set; } = true;
        public bool BalanceWorkload { get; set; } = false;
        public bool AllowConsecutiveClasses { get; set; } = false;
    }

    public class ConflictDto
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public int SlotId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class TimetableStatisticsDto
    {
        public int TimetableId { get; set; }
        public int TotalSlots { get; set; }
        public int FilledSlots { get; set; }
        public int EmptySlots { get; set; }
        public Dictionary<string, int> SubjectDistribution { get; set; } = new();
        public Dictionary<string, int> TeacherWorkload { get; set; } = new();
        public Dictionary<string, int> DailyDistribution { get; set; } = new();
    }

    public class SwapSlotsRequest
    {
        public SlotPosition Slot1 { get; set; }
        public SlotPosition Slot2 { get; set; }
    }

    public class SlotPosition
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
    }

    public class TeacherConflictResult
    {
        public bool IsAvailable { get; set; }
        public int? ConflictingClassId { get; set; }
        public string? ConflictingClassName { get; set; }
        public string? ConflictingSubjectName { get; set; }
        public string? ConflictMessage { get; set; }
    }

    #endregion
}