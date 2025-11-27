using Group_2.Models;

namespace Group_2.Services
{
    public interface IEventRepository
    {
        Task<List<EventItem>> GetAllAsync();
        Task<List<EventItem>> GetByDateAsync(DateTime date);
        Task AddAsync(EventItem item);
        Task UpdateAsync(EventItem item);
        Task DeleteAsync(Guid id);
    }
}
