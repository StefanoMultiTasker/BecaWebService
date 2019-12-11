using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class DbaFunzioniGruppi
    {
        public DbaFunzioniGruppi()
        {
            DbaFunzioni = new HashSet<DbaFunzioni>();
        }

        public string CodMenuMain { get; set; }
        public string CodMenuArea { get; set; }
        public string DescMenuMain { get; set; }
        public string Icona { get; set; }
        public short Ordine { get; set; }
        public DateTime? DtInsert { get; set; }

        public virtual DbaFunzioniAree CodMenuAreaNavigation { get; set; }
        public virtual ICollection<DbaFunzioni> DbaFunzioni { get; set; }
    }
}
