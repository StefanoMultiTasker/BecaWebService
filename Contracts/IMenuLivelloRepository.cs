using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IMenuLivelloRepository
    {
        Task<IEnumerable<VMenuLivello>> GetAllByLivello(int idLivello);
    }
}
