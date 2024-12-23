using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models.Custom
{
    public class PreparaDocs
    {
        public string? folder { get; set; }
        public bool IncludeCU { get; set; }
        public string? AnnoInizio { get; set; }
        public string? MeseInizio { get; set; }
        public string? AnnoFine { get; set; }
        public string? MeseFine { get; set; }
        public List<string>? Matricole { get; set; }
    }

    public class PreparaEbitemp
    {
        public List<Matricole4Ebitemp> matricole4Ebitemp { get; set; }
    }

    public class Matricole4Ebitemp
    {
        public string anno { get; set; }
        public string mese { get; set; }
        public string matricola { get; set; }
    }

}
