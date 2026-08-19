using SchoolShedule.Services.Generation;

namespace SchoolShedule.Interfaces
{
    /// <summary>
    /// Сервис генерации расписания на основе шаблонов и правил.
    /// </summary>
    public interface IScheduleGeneratorService
    {
        /// <summary>
        /// Асинхронно генерирует расписание для указанного шаблона.
        /// </summary>
        /// <param name="templateId">Идентификатор шаблона генерации.</param>
        /// <returns>Результат генерации расписания с информацией о конфликтах.</returns>
        Task<ScheduleGenerationResult> GenerateAsync(int templateId);
    }
}
