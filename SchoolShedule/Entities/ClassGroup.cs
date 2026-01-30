using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Группа учеников
    /// </summary>
    public class ClassGroup
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Буква класса
        /// </summary>
        public char? Letter { get; set; }

        /// <summary>
        /// Номер класса
        /// </summary>
        [Required]
        public int Grade { get; set; }

        /// <summary>
        /// Принадлежащие данному классу ученики
        /// </summary>
        public ICollection<Student>? Students { get; set; } = default!;

        /// <summary>
        /// Сборное название Группы
        /// </summary>
        /// <example>3-А или 1 </example>
        public string Name =>
    Letter.HasValue
        ? $"{Grade}"
        : $"{Grade}-{Letter}";

    }
}
