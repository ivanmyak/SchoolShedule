using System.ComponentModel.DataAnnotations;

namespace SchoolShedule.Entities
{
    /// <summary>
    /// Предмет
    /// </summary>
    public class Subject
    {
        public int Id { get; set; }

        /// <summary>
        /// Наименование предмета
        /// </summary>
        [Required]
        public string Name { get; set; } = default!;

        /// <summary>
        /// Короткий код предмета
        /// </summary>
        /// <example>MATH = Математика</example>
        [MaxLength(10)]
        [Required]
        public string Code { get; set; } = default!;

    }
}
