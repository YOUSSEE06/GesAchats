using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IDeliveryNoteRepository : IRepository<DeliveryNote>
{
    Task<IEnumerable<DeliveryNote>> GetAllWithDetailsAsync();
}
