using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models.Custom
{
    public class DossierMail
    {
        public string oggetto { get; set; }
        public string messaggio { get; set; }
        public string cdff { get; set; }
        public string? codRS { get; set; }
        public int? idDossier { get; set; }
        public IList<DossierMailDestinatari> destinatari { get; set; } = [];
        public string azione { get; set; }
    }

    public class DossierMailDestinatari
    {
        public string? ffcl { get; set; }
        public string? codc { get; set; }
        public string? eMail { get; set; }
        public string? esitoAzione { get; set; }
    }
}
