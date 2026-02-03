using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolShedule.Entities;
using System;

namespace SchoolShedule.Data
{
    public class SheduleDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public DbSet<Student> Students { get; set; } = default!;
        public DbSet<Teacher> Teachers { get; set; } = default!;
        public DbSet<ClassGroup> ClassGroups { get; set; } = default!;
        public DbSet<Subject> Subjects { get; set; } = default!;
        public DbSet<Lesson> Lessons { get; set; } = default!;
        public DbSet<Room> Rooms { get; set; } = default!; 
        public DbSet<TimeSlot> TimeSlots { get; set; } = default!;

        public SheduleDbContext(DbContextOptions<SheduleDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Связь один-к-одному: Teacher ↔ User
            builder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<Teacher>(t => t.UserId);

            builder.Entity<Teacher>()
                .HasIndex(t => t.UserId)
                .IsUnique();

            // Связь один-к-одному: Student ↔ User 
            builder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId);

            builder.Entity<Student>()
                .HasIndex(s => s.UserId)
                .IsUnique();

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
        }

    }
}
