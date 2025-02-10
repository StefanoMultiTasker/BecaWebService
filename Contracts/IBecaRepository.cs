using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IBecaRepository
    {
        List<Company> Companies(int? idCompany = null, string? name = null);
        BecaViewAction BecaViewActions(string name);
    }
}
