using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using School_managment.Common.Models;
using School_managment.Features.Classes.Models;
using School_managment.Features.Subjects.Models;
using School_managment.Features.Teachers.Models;
using School_managment.Features.Timetables.Models;
using School_managment.Features.Users.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Xml;

namespace School_managment.Infrastructure
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // === User ===
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Name)
                      .IsRequired()
                      .HasMaxLength(200);
            });

            // === Teacher ===
            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.ToTable("Teachers");
                entity.Property(t => t.Subject)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(t => t.WeeklyQuota)
                      .IsRequired();

                // تحويل List<string> RestrictedPeriods إلى JSON
                var converter = new ValueConverter<List<string>, string>(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>()
                );

                entity.Property(t => t.RestrictedPeriods)
                      .HasConversion(converter)
                      .HasColumnType("nvarchar(max)");

                // العلاقة مع ClassTeachers
                entity.HasMany(t => t.ClassTeachers)
                      .WithOne(ct => ct.Teacher)
                      .HasForeignKey(ct => ct.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === Subject ===
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.ToTable("Subjects");
                entity.HasKey(s => s.Id);

                entity.Property(s => s.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(s => s.HoursPerWeek)
                      .IsRequired();

                entity.HasMany(s => s.ClassSubjects)
                      .WithOne(cs => cs.Subject)
                      .HasForeignKey(cs => cs.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === Class ===
            modelBuilder.Entity<Class>(entity =>
            {
                entity.ToTable("Classes");
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Grade)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(c => c.Section)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(c => c.TotalHours)
                      .IsRequired();

                entity.HasMany(c => c.ClassSubjects)
                      .WithOne(cs => cs.Class)
                      .HasForeignKey(cs => cs.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.ClassTeachers)
                      .WithOne(ct => ct.Class)
                      .HasForeignKey(ct => ct.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === ClassSubject ===
            modelBuilder.Entity<ClassSubject>(entity =>
            {
                entity.ToTable("ClassSubjects");

                entity.HasKey(cs => new { cs.ClassId, cs.SubjectId });

                entity.HasOne(cs => cs.Class)
                      .WithMany(c => c.ClassSubjects)
                      .HasForeignKey(cs => cs.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Subject)
                      .WithMany(s => s.ClassSubjects)
                      .HasForeignKey(cs => cs.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Teacher)
                      .WithMany() 
                      .HasForeignKey(cs => cs.TeacherId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // === ClassTeacher ===
            modelBuilder.Entity<ClassTeacher>(entity =>
            {
                entity.ToTable("ClassTeachers");

                entity.HasKey(ct => new { ct.ClassId, ct.TeacherId });

                entity.HasOne(ct => ct.Class)
                      .WithMany(c => c.ClassTeachers)
                      .HasForeignKey(ct => ct.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ct => ct.Teacher)
                      .WithMany(t => t.ClassTeachers)
                      .HasForeignKey(ct => ct.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(ct => ct.NameTeacher)
                      .HasMaxLength(200)
                      .IsRequired(false);

                entity.Property(ct => ct.NameClass)
                      .HasMaxLength(200)
                      .IsRequired(false);
            });

            // === Timetable ===
            modelBuilder.Entity<TimeTable>(entity =>
            {
                entity.ToTable("Timetables");
                entity.HasKey(t => t.Id);

                entity.Property(t => t.ScheduleId)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(t => t.GeneratedAt)
                      .IsRequired();

                entity.Property(t => t.IsActive)
                      .IsRequired();

                entity.Property(t => t.Constraints)
                      .HasColumnType("nvarchar(max)");

                entity.HasOne(t => t.Class)
                      .WithMany()
                      .HasForeignKey(t => t.ClassId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(t => t.TimetableSlots)
                      .WithOne(ts => ts.Timetable)
                      .HasForeignKey(ts => ts.TimetableId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === TimetableSlot ===
            modelBuilder.Entity<TimetableSlot>(entity =>
            {
                entity.ToTable("TimetableSlots");
                entity.HasKey(ts => ts.Id);

                entity.Property(ts => ts.Period).IsRequired();
                entity.Property(ts => ts.DayOfWeek).IsRequired();
                entity.Property(ts => ts.CreatedAt).IsRequired();

                entity.HasOne(ts => ts.Subject)
                      .WithMany()
                      .HasForeignKey(ts => ts.SubjectId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(ts => ts.Teacher)
                      .WithMany()
                      .HasForeignKey(ts => ts.TeacherId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(ts => ts.Timetable)
                      .WithMany(t => t.TimetableSlots)
                      .HasForeignKey(ts => ts.TimetableId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === TeacherAvailability ===
            modelBuilder.Entity<TeacherAvailability>(entity =>
            {
                entity.ToTable("TeacherAvailabilities");
                entity.HasKey(ta => ta.Id);

                entity.Property(ta => ta.DayOfWeek).IsRequired();
                entity.Property(ta => ta.Period).IsRequired();
                entity.Property(ta => ta.IsAvailable).IsRequired();

                entity.HasOne(ta => ta.Teacher)
                      .WithMany()
                      .HasForeignKey(ta => ta.TeacherId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ClassTeacher> ClassTeachers { get; set; }
        public DbSet<ClassSubject> ClassSubjects { get; set; }
        public DbSet<TimeTable> Timetables { get; set; }
        public DbSet<TimetableSlot> TimetableSlots { get; set; }
        public DbSet<TeacherAvailability> TeacherAvailabilities { get; set; }
    }
}
