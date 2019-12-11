using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class AnagLivelli
    {
        public int IdLivello { get; set; }
        public string DescLivello { get; set; }
        public bool? FlgProtected { get; set; }
        public bool? FlgFiltroFiliale { get; set; }
        public bool FlgCambioPwd { get; set; }
        public DateTime DtInsert { get; set; }
        public string HomePage { get; set; }
        public string LoginPage { get; set; }
        public string FlagsProfilo { get; set; }
    }
}
