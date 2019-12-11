using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class DbaFunzioniCfg
    {
        public int Id { get; set; }
        public string CodMenuItem { get; set; }
        public int IdLivello { get; set; }
        public string CodModulo { get; set; }
        public string Caption { get; set; }
        public string CodMenuMain { get; set; }
        public int? SottoGruppo { get; set; }
        public int? Posizione { get; set; }
        public string DetailsForm { get; set; }
        public string CustomForm { get; set; }
        public bool FlAdd { get; set; }
        public bool FlEdit { get; set; }
        public bool FlDel { get; set; }
        public bool FlDetail { get; set; }
        public bool FlList { get; set; }
        public bool FlExcel { get; set; }
        public DateTime DtInsert { get; set; }
    }
}
