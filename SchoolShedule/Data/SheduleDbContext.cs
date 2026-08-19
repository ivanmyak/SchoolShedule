using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Entities;
using SchoolShedule.Entities.Generation;
using System;

namespace SchoolShedule.Data
{
    public class SheduleDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        private readonly ILogger<SheduleDbContext> _logger;

        public DbSet<Student> Students { get; set; } = default!;
        public DbSet<Teacher> Teachers { get; set; } = default!;
        public DbSet<ClassGroup> ClassGroups { get; set; } = default!;
        public DbSet<Subject> Subjects { get; set; } = default!;
        public DbSet<Lesson> Lessons { get; set; } = default!;
        public DbSet<Room> Rooms { get; set; } = default!;
        public DbSet<TimeSlot> TimeSlots { get; set; } = default!;

        public DbSet<ScheduleTemplate> ScheduleTemplates { get; set; }
        public DbSet<LessonRuleContainer> LessonRuleContainers { get; set; }

        public SheduleDbContext(DbContextOptions<SheduleDbContext> options, ILogger<SheduleDbContext> logger) : base(options) { _logger = logger; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Связь один-к-одному: Teacher - User
            builder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Teacher>(t => t.UserId);

            builder.Entity<Teacher>()
                .HasMany(t => t.Subjects)
                .WithMany();

            builder.Entity<Teacher>()
                .HasIndex(t => t.UserId)
                .IsUnique();

            // Связь один-к-одному: Student - User 
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId);

            builder.Entity<Student>()
                .HasIndex(s => s.UserId)
                .IsUnique();

            // Связь многие-ко-многим: Room - Subjects
            builder.Entity<Room>()
                .HasMany(t => t.AssignedSubjects)
                .WithMany();

            // Уникальные индексы для предотвращения конфликтов в расписании 
            builder.Entity<Lesson>()
                .HasIndex(l => new { l.TeacherId, l.TimeSlotId })
                .IsUnique();

            builder.Entity<Lesson>()
                .HasIndex(l => new { l.RoomId, l.TimeSlotId })
                .IsUnique();

            builder.Entity<Lesson>()
                .HasIndex(l => new { l.ClassGroupId, l.TimeSlotId })
                .IsUnique();

            // Пометил, что код предмета тоже будет уникальным
            builder.Entity<Subject>()
                .HasIndex(s => s.Code)
                .IsUnique();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken token = default)
        {
            LogChanges();
            return await base.SaveChangesAsync(token);
        }

        private void LogChanges()
        {
            var modified = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in modified)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified)
                    {
                        _logger.LogInformation(
                            "[UPDATE] {Entity} {Id}: {Prop} {Old} -> {New}",
                            entry.Entity.GetType().Name,
                            entry.Property("Id").CurrentValue,
                            prop.Metadata.Name,
                            prop.OriginalValue,
                            prop.CurrentValue
                        );
                    }
                }
            }
        }

    }
}
