using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class VMenuLivello
    {
        public int IdLivello { get; set; }
        public string AreaCod { get; set; }
        public string AreaDesc { get; set; }
        public string AreaIcona { get; set; }
        public short AreaOrdine { get; set; }
        public string GruppoCod { get; set; }
        public string GruppoDesc { get; set; }
        public string GruppoIcona { get; set; }
        public short GruppoOrdine { get; set; }
        public string MenuCod { get; set; }
        public int SottoGruppo { get; set; }
        public int Posizione { get; set; }
        public string Caption { get; set; }
        public string DetailsForm { get; set; }
        public bool FlAdd { get; set; }
        public bool FlEdit { get; set; }
        public bool FlDel { get; set; }
        public bool FlDetail { get; set; }
        public bool FlList { get; set; }
        public bool FlExcel { get; set; }
        public int Id { get; set; }
    }
}
