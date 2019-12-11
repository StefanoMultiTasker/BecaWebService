using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class DbaFunzioniAree
    {
        public DbaFunzioniAree()
        {
            DbaFunzioniGruppi = new HashSet<DbaFunzioniGruppi>();
        }

        public string CodMenuArea { get; set; }
        public string DescMenuArea { get; set; }
        public string Icona { get; set; }
        public short Ordine { get; set; }
        public DateTime? DtInsert { get; set; }

        public virtual ICollection<DbaFunzioniGruppi> DbaFunzioniGruppi { get; set; }
    }
}
