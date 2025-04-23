using Contracts;
using iText.StyledXmlParser.Jsoup.Nodes;
using static System.Net.WebRequestMethods;
using System.Net.Mail;
using System.Net;
using Repository;
using Entities.Models;
using ExtensionsLib;
using BecaWebService.ExtensionsLib;
using Entities.Models.Custom;
using BecaWebService.Models.Communications;
using BecaWebService.Helpers;
using Contracts.Custom;

namespace BecaWebService.Services.Custom
{
    public class MailService : IMailService
    {
        private readonly IGenericRepository _gRepository;
        private readonly IBecaRepository _becaRepository;
        private readonly ILoggerManager _logger;
        private IWebHostEnvironment _env;
        public MailService(IGenericRepository genRepository, IBecaRepository becaRepository, IWebHostEnvironment env, ILoggerManager logger)
        {
            _gRepository = genRepository;
            _becaRepository = becaRepository;
            _logger = logger;
            _env = env;
        }

        private string getMail(SendMailOptionsOrigin origin, string what)
        {
            string sender = "";
            switch (origin.Type)
            {
                case "APL":
                    sender = _becaRepository.Companies()!.Where(c => c.idCompany == _gRepository.GetActiveCompany()!.idCompany).FirstOrDefault()!.mail1 ?? ""; break;
                case "Utente":
                    sender = _gRepository.GetLoggedUser()!.email ?? ""; break;
                case "Filiale":
                    sender = getFromFiliale(origin.Name!); break;
                case "Cliente":
                    sender = getFromCliente(origin.Name!); break;
                case "Action":
                    sender = getFromAction(origin.Name!, what); break;
                case "EMail":
                    sender = origin.Value ?? "";break;
                default:
                    break;
            }
            return sender;
        }

        private string getFromFiliale(string cdff)
        {
            try
            {
                BecaParameters _parameters = new BecaParameters();
                _parameters.Add("CDFF", cdff);
                List<object> filiali = _gRepository.GetDataBySQL("DbDati", "Select EMFL From ANTEX", _parameters.parameters);
                if (filiali == null || filiali.Count() == 0) { return $"Non ho trovato la filiale che deve inviare"; }

                return filiali[0].GetPropertyString("EMFL") ?? "";
            }
            catch (Exception ex) { _logger.LogError($"MailService, getMail, getFiliale: {ex.Message}"); return ""; }
        }

        private string getFromCliente(string codice)
        {
            try
            {
                BecaParameters _parameters = new BecaParameters();
                _parameters.Add("FFCL", codice.left(3));
                _parameters.Add("CODC", codice.right(6));
                List<object> clienti = _gRepository.GetDataBySQL("DbDati", "Select EMCL From CLIEN", _parameters.parameters);
                if (clienti == null || clienti.Count() == 0) { return $"Non ho trovato la cliente che deve inviare"; }

                return clienti[0].GetPropertyString("EMCL") ?? "";
            }
            catch (Exception ex) { _logger.LogError($"MailService, getMail, getCliente: {ex.Message}"); return ""; }
        }

        private string getFromAction(string name, string what)
        {
            try
            {
                string attr = what switch
                {
                    "from"      => "sqlEmailFrom",
                    "to"        => "sqlEmailTo",
                    "cc"        => "sqlEmailCC",
                    "ccn"       => "sqlEmailCCN",
                    "subject"   => "sqlEmailSubject",
                    "text"      => "sqlEmailText",
                    _           => ""
                };
                if (attr == "") return "";
                BecaViewAction action = _becaRepository.BecaViewActions(name)!;
                BecaParameters _parameters = new BecaParameters();
                List<object> dati = _gRepository.GetDataBySQL("DbDati", action.GetPropertyString(attr), _parameters.parameters);
                if (dati == null || dati.Count() == 0) { return $"Non ho trovato l'indirizzo che deve inviare"; }

                return dati[0].GetPropertyString("email") ?? "";
            }
            catch (Exception ex) { _logger.LogError($"MailService, getMail, getAction: {ex.Message}"); return ""; }
        }

        public GenericResponse Send(SendMailOptions options)
        {
            try
            {
                string sender = getMail(options.Sender, "from");
                string dest = getMail(options.Dest, "to");
                string cc = options.CC == null ? "" : getMail(options.CC, "cc");
                string ccn = options.CCN == null ? "" : getMail(options.CCN, "ccn");
                string subject = options.Subject.Type switch
                {
                    "Action" => getFromAction(options.Subject.Name!, "subject"),
                    _ => options.Subject.Value ?? ""
                };
                string text = options.Text.Type switch
                {
                    "Action" => getFromAction(options.Text.Name!, "text"),
                    _ => options.Text.Value ?? ""
                };

                if (sender == "") return "Non ho trovato la mail di spedizione, controlla i parametri".toResponse();
                if (dest == "") return "Non ho trovato la mail di destinazione, controlla i parametri".toResponse();
                if (subject == "") return "Non ho trovato l'oggetto della mail, controlla i parametri".toResponse();
                if (text == "") return "Non ho trovato il testo della mail, controlla i parametri".toResponse();

                SendMail(sender, dest, cc, ccn, subject, text);
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        private void SendMail(string sender, string dest, string cc, string ccn, string subject, string text)
        {
            MailMessage objMail = new MailMessage();
            objMail.Sender = new MailAddress(sender, sender);
            objMail.From = new MailAddress(sender, sender);
            objMail.ReplyToList.Add(new MailAddress(sender));

            SmtpClient objSMTP = new SmtpClient
            {
                Host = "pro.eu.turbo-smtp.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential("gruppoedp@abeaform.it", "ddHu39eX")
            };

            objMail.Subject = subject;
            objMail.IsBodyHtml = true;
            objMail.BodyEncoding = System.Text.Encoding.UTF8;
            objMail.Body = text;
            foreach (string mail in dest.Split(";"))
            {
                objMail.To.Add(new MailAddress(mail));
            }
            foreach (string mail in cc.Split(";"))
            {
                if(mail != "") objMail.CC.Add(new MailAddress(mail));
            }
            foreach (string mail in ccn.Split(";"))
            {
                if (mail != "") objMail.Bcc.Add(new MailAddress(mail));
            }

            objSMTP.Send(objMail);
        }
    }
}
