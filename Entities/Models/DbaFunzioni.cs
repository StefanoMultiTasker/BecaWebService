using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public partial class DbaFunzioni
    {
        public string CodMenuItem { get; set; }
        public string DescMenuItem { get; set; }
        public string Caption { get; set; }
        public string CodMenuMain { get; set; }
        public int SottoGruppo { get; set; }
        public int Posizione { get; set; }
        public string Form { get; set; }
        public string DetailsForm { get; set; }
        public string CustomForm { get; set; }
        public string TableName { get; set; }
        public string ViewName { get; set; }
        public bool GridWait4Param { get; set; }
        public string Parameters { get; set; }
        public DateTime DtInsert { get; set; }

        public virtual DbaFunzioniGruppi CodMenuMainNavigation { get; set; }
    }
}
