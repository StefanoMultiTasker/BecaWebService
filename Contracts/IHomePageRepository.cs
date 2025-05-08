using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IHomePageRepository
    {
        public List<BecaHomePage> GetHomePageByUser();
        List<BecaHomeBuild> GetHomeBuildByUser(int[] idProfiles);
        BecaHomeBuild? GetHomeBrick(int idHomeBrick, int[] idProfiles);
    }
}
