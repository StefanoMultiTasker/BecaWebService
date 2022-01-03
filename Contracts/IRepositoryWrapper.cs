using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IRepositoryWrapper
    {
        IBecaViewRepository BecaView { get;  }
        void ReadToken(string token);
        Task SaveAsync();
    }
}
