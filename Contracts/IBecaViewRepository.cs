using Entities.DataTransferObjects;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IBecaViewRepository
    {
        //Task<BecaView> GetViewByID(int idView);
        BecaView GetViewByID(int idView);
        UIform GetViewUI(int idView, string tipoUI);
        bool CustomizeColumnsByUser(int idView, List<dtoBecaData> cols);
    }
}
