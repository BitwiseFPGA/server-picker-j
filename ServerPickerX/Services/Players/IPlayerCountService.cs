using System.Threading.Tasks;

namespace ServerPickerX.Services.Players
{
    public interface IPlayerCountService
    {
        // How many people are playing the game right now across the whole world, or null
        // when Steam does not answer. Steam has no per region figure
        Task<int?> GetCurrentPlayerCountAsync(string appId);
    }
}
