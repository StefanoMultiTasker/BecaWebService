using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class VMenu
    {
        public string CodMenuItem { get; set; }
        public string CodMenuMain { get; set; }
        public string Caption { get; set; }
        public int SottoGruppo { get; set; }
        public int? Posizione { get; set; }
        public string DetailsForm { get; set; }
        public string DescLivello { get; set; }
        public int IdLivello { get; set; }
        public bool FlAdd { get; set; }
        public bool FlEdit { get; set; }
        public bool FlDel { get; set; }
        public bool FlDetail { get; set; }
        public bool FlList { get; set; }
        public bool FlExcel { get; set; }
        public int Id { get; set; }
    }
}
