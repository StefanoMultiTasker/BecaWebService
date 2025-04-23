using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using System.Net.Mail;
using System.Net;
using Contracts.Custom;
using BecaWebService.Helpers;

namespace BecaWebService.Services.Custom
{
    public class LavorService : ILavorService
    {
        private readonly IGenericRepository _gRepository;
        private readonly ILogger<MiscService> _logger;
        public LavorService(IGenericRepository genRepository, ILogger<MiscService> logger)
        {
            _gRepository = genRepository;
            _logger = logger;
        }
        public GenericResponse ListCUByCodFisc()
        {
            if (_gRepository.GetLoggedUser() == null) return "Non sei loggato".toResponse();
            if (_gRepository.GetActiveCompany() == null) return "Non hai specificato la company".toResponse();

            string path = $@"E:\BecaWeb\Web\Upload\{_gRepository.GetActiveCompany()!.MainFolder!}\CUD";

            BecaParameters parameters = new BecaParameters();
            parameters.Add("idUtente", _gRepository.GetLoggedUser()!.idUtenteLoc(_gRepository.GetActiveCompany()!.idCompany));
            List<object> lavor = _gRepository.GetDataBySQL("DbDati", "SELECT * From LAVOR", parameters.parameters);
            if (lavor.Count == 0) return new GenericResponse("Anagrafica non trovata");

            string CF = lavor[0].GetPropertyValue("CFIL").ToString() ?? "";
            if (CF == null || CF == "") return new GenericResponse("Anagrafica senza Codice Fiscale");

            return new GenericResponse(
                Directory.EnumerateFiles($@"{path}", $"{CF}*.pdf")
                .Select(file => file.Substring(file.LastIndexOf(@"\") + 1))
                );
        }

        public GenericResponse LavorSendRequestMail(string subject, string text)
        {
            try
            {
                if (_gRepository.GetLoggedUser() == null) return "Non sei loggato".toResponse();
                if (_gRepository.GetActiveCompany() == null) return "Non hai specificato la company".toResponse();

                BecaParameters parameters = new BecaParameters();
                parameters.Add("idUtente", _gRepository.GetLoggedUser()!.idUtenteLoc(_gRepository.GetActiveCompany()!.idCompany));
                List<object> lavor = _gRepository.GetDataBySQL("DbDati", "SELECT * From LAVOR", parameters.parameters);
                if (lavor.Count == 0) return new GenericResponse("Anagrafica non trovata");

                string cognome = lavor[0].GetPropertyValue("CGNL").ToString() ?? "";
                string nome = lavor[0].GetPropertyValue("NOML").ToString() ?? "";
                string nomeC = cognome + " " + nome;
                string CF = lavor[0].GetPropertyValue("CFIL").ToString() ?? "";
                List<MatricolaLav> Matricole = GetMatricolaUtente();
                string matricole = string.Join(", ", Matricole.Select(m => m.Matricola));
                string clienti = string.Join(", ", Matricole.Select(m => m.Cliente));
                string filiale = Matricole[0].Filiale ?? "";
                string filiali = string.Join(", ", GetFilialiUtente().Where(F => F != filiale));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                SmtpClient objSMTP = new SmtpClient
                {
                    Host = "192.168.0.5",
                    Port = 25
                };
                string owner = ((_gRepository.GetLoggedUser()!.email ?? "").ToString() ?? "").Split(";")[0];
                string sender = $"info.bw@{_gRepository.GetActiveCompany()!.MainFolder}.it";
                MailMessage objMail = new MailMessage();
                objMail.Sender = new MailAddress(sender, owner);
                objMail.From = new MailAddress(sender, owner);
                objMail.ReplyToList.Add(new MailAddress(owner));

                objMail.Subject = $"({nomeC}): {subject}";
                objMail.IsBodyHtml = true;
                objMail.BodyEncoding = System.Text.Encoding.UTF8;
                objMail.Body = $"Cognome: {cognome}<br>" +
                    $"Nome: {nome}<br>" +
                    $"Codice Fiscale: {CF}<br>" +
                    $"E-Mail: {owner}<br>" +
                    $"Tel.: {_gRepository.GetLoggedUser()!.Phone}<br>" +
                    $"Filiale: {filiale}<br>" +
                    $"Matricola/e: {matricole}<br>" +
                    $"Cliente/i: {clienti}<br>" +
                    $"Altre filiali: ({filiali})<br>" +
                    $"Data richiesta: {DateTime.Today.ToLongDateString()} {DateTime.Now.ToLongTimeString()}<br><br><br>" +
                    $"Richiesta: {text}";
                objMail.To.Add(new MailAddress($"lavoratori@{_gRepository.GetActiveCompany()!.MainFolder}.it"));
                if (subject == "Test Sviluppo")
                {
                    objMail.Body = "Questa è una prova<br><br>" + objMail.Body;
                    objMail.CC.Add("g.musolino@abeaform.it");
                    objMail.CC.Add("s.bettica@abeaform.it");
                }
                objSMTP.Send(objMail);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex.Message);
            }
        }

        private List<MatricolaLav> GetMatricolaUtente()
        {
            DateTime dt1 = DateTime.Today.AddDays(-DateTime.Today.Day + 1).AddMonths(-1);
            DateTime dt2 = dt1.AddMonths(1).AddDays(-1);

            BecaParameters parameters = new BecaParameters();
            parameters.Add("idUtente", _gRepository.GetLoggedUser()!.idUtenteLoc(_gRepository.GetActiveCompany()!.idCompany));
            List<object> lavor = _gRepository.GetDataBySQL("DbDati", "SELECT * From vCOLPE_UT Order By Fine DESC", parameters.parameters);

            if (lavor.Count == 0)
                return new List<MatricolaLav>(new MatricolaLav[] { new MatricolaLav { Matricola = "", Cliente = "", Filiale = "" } });

            List<object> matricoleAtt = lavor
                .Where(l => (DateTime)l.GetPropertyValue("DTII") <= dt2 && (DateTime)l.GetPropertyValue("DTFI") >= dt1)
                .ToList();
            List<MatricolaLav> matricole = matricoleAtt
                .Select(l => new MatricolaLav
                {
                    Matricola = l.GetPropertyValue("MATR").ToString() ?? "",
                    Cliente = l.GetPropertyValue("RSCL").ToString() ?? "",
                    Filiale = l.GetPropertyValue("CDFF").ToString() ?? ""
                })
                .ToList();

            if (matricole.Count == 0)
                return new List<MatricolaLav>(new MatricolaLav[] {
                    new MatricolaLav {
                        Matricola = lavor[0].GetPropertyValue("MATR").ToString() ?? "",
                        Cliente = lavor[0].GetPropertyValue("RSCL").ToString() ?? "",
                        Filiale = lavor[0].GetPropertyValue("CDFF").ToString() ?? ""
                    }
                });
            else
                return matricole;
        }

        private List<string> GetFilialiUtente()
        {
            BecaParameters parameters = new BecaParameters();
            parameters.Add("idUtente", _gRepository.GetLoggedUser()!.idUtenteLoc(_gRepository.GetActiveCompany()!.idCompany));
            return _gRepository.GetDataBySQL("DbDati", "SELECT * From vCOLPE_UT_Fil", parameters!.parameters!)
                .Select(F => F.GetPropertyValue("CDFF").ToString() ?? "")
                .ToList();
        }

        private class MatricolaLav
        {
            public string? Matricola { get; set; }
            public string? Cliente { get; set; }
            public string? Filiale { get; set; }
        }
    }
}
