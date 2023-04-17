using Entities.DataTransferObjects;
using Entities.Models;

namespace Contracts
{
    public interface IBecaViewRepository
    {
        //Task<BecaView> GetViewByID(int idView);
        BecaView GetViewByID(int idView);
        UIform GetViewUI(int idView, string tipoUI);
        UIform GetViewUI(string form);
        bool CustomizeColumnsByUser(int idView, List<dtoBecaData> cols);
    }
}
