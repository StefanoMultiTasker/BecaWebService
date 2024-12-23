using BecaWebService.Models.Communications;
using Contracts;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using RestSharp;
using System.Net.Mail;
using System.Net;
using Contracts.Custom;

namespace BecaWebService.Services.Custom
{
    public class SavinoService : ISavinoService
    {
        private readonly ISharedService _miscServiceBase;
        private readonly IGenericRepository _gRepository;
        private readonly ILogger<MiscService> _logger;
        public SavinoService(ISharedService miscServiceBase, IGenericRepository genRepository, ILogger<MiscService> logger)
        {
            _miscServiceBase = miscServiceBase;
            _gRepository = genRepository;
            _logger = logger;
        }
        public GenericResponse SavinoOTP(SavinoOTP res, StreamWriter sw)
        {
            //sw.WriteLine(""); 
            try
            {
                sw.WriteLine($"APL: {_gRepository.domain}");

                List<object> user = _gRepository.GetDataBySQL("DbDati", "SELECT * From vFRM_DatiWS", null);
                string url = user[0].GetPropertyString("urlBase");
                string token = user[0].GetPropertyString("token");

                LinkResponse linkResponse = _miscServiceBase.CallWS_JSON_mode<LinkResponse>(url + "/signatory/link/" + res.id.ToString(), token, Method.Get);
                if (!linkResponse.success)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non riesco ad accedere al WebService. Token: {token}, errorCode: {linkResponse.errorCode}, error: {linkResponse.error}");
                    sw.Flush();
                    return new GenericResponse("Erorre nel reperimento del link");
                }
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Richiesto link per firmatario {res.id}");
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Risposta: otp {linkResponse.data.otp}");

                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > res.dueDate)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: OTP Scaduto: otp {res.otp}, scadenza: {res.dueDate}");
                    linkResponse = _miscServiceBase.CallWS_JSON_mode<LinkResponse>(url + "/signatory/link/" + res.id.ToString(), token, Method.Get);
                    if (!linkResponse.success) return new GenericResponse("Erorre nel reperimento del link");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Nuovo OTP: otp {linkResponse.data.otp}");
                }

                sw.Flush();

                string otp = linkResponse.data.otp;
                string email = user[0].GetPropertyString("sender");
                string nome = "";

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Cerco il firmatario {res.id} (vFRM_Firmatari)");
                BecaParameters parameters = new BecaParameters();
                parameters.Add("idFirmatarioWS", res.id);
                List<object> firme = _gRepository.GetDataBySQL("DbDati", "SELECT * From vFRM_Firmatari", parameters.parameters);
                if (firme.Count == 0)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non l'ho trovato");
                }
                else
                {
                    nome = firme[0].GetPropertyString("nomeFirmatario");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: si chiama {nome}");
                }
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Invio la mail da parte di {_gRepository.domain}");

                //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                //System.Net.Mail.SmtpClient objSMTP = new System.Net.Mail.SmtpClient
                //{
                //    Host = "192.168.0.5",
                //    Port = 25,
                //};
                //if (email == "documentazione.bw@tempor.it") email = "documentazione.bw@attalgroup.it";
                //email = email.Replace("documentazione.bw", "postmaster");
                SmtpClient objSMTP = new SmtpClient
                {
                    Host = "pro.eu.turbo-smtp.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential("gruppoedp@abeaform.it", "ddHu39eX")
                };
                MailMessage objMail = new MailMessage();
                objMail.Sender = new MailAddress(email, email);
                objMail.From = new MailAddress(email, email);
                objMail.ReplyToList.Add(new MailAddress(email));

                objMail.Subject = $"OTP per la firma - {_gRepository.domain} Spa";
                objMail.IsBodyHtml = true;
                objMail.BodyEncoding = System.Text.Encoding.UTF8;
                objMail.Body = $"Gentilissima/o {nome},<br>inserisca il seguente OTP per completare il processo di firma:<br>{otp}<br>Cordiali Saluti<br>{_gRepository.domain} Spa ";
                objMail.To.Add(new MailAddress($"{res.email}"));

                objSMTP.Send(objMail);
                sw.Flush();
                return new GenericResponse(true);

            }
            catch (Exception ex)
            {
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: errore: {ex.Message}");
                sw.Flush();
                return new GenericResponse(ex.Message);
            }
        }

        public async Task<bool> SavinoFirma(SavinoFirma res, StreamWriter sw)
        {
            try
            {
                sw.WriteLine($"APL: {_gRepository.domain}");

                sw.WriteLine("Giro a PMS la callback");
                try
                {
                    _miscServiceBase.CallWS_JSON_mode<object>("https://api.pms.attalgroup.it/pms/user-processes/signed", "", Method.Post, res);
                }
                catch (Exception ex)
                {
                    sw.WriteLine($"ha dato errore {ex.Message}");
                }

                List<object> user = _gRepository.GetDataBySQL("DbDati", "SELECT * From vFRM_DatiWS", null);
                string url = user[0].GetPropertyString("urlBase");
                string token = user[0].GetPropertyString("token");

                var linkResponse = _miscServiceBase.CallWS_JSON_mode<LinkResponse>(url + "/complete/" + res.root.ToString(), token, Method.Post);
                if (!linkResponse.success)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nella chiusura dell'ordine {linkResponse.data}");
                    sw.Flush();
                    return false;
                }
                //_logger.LogInformation($"Completato l'ordine {res.root}");
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Completato l'ordine {res.root}");

                BecaParameters parameters = new BecaParameters();
                parameters.Add("idOrdineWS", res.root);
                int sp = await _gRepository.ExecuteProcedure("DbDati", "spFRM_DocsRiceviFirma", parameters.parameters);
                //_logger.LogInformation($"Aggiornato lo stato: {sp} record aggiornato");
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Aggiornato lo stato: {sp} record aggiornato");
                if (sp != 0)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Cerco i dati del firmatario dell'ordine {res.root} (vFRM_Firmatari)");
                    List<object> firmatari = _gRepository.GetDataBySQL("DbDati", "SELECT * From vFRM_Firmatari", parameters.parameters);
                    if (firmatari == null) { sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non lo trovo!!!"); sw.Flush(); return false; }
                    if (firmatari.Count == 0) return false;
                    string Dest = firmatari[0].GetPropertyString("email");
                    string nome = firmatari[0].GetPropertyString("nomeFirmatario");

                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Cerco i documenti dell'ordine {res.root} (vFRM_InvioOrdiniCliList)");
                    List<object> documenti = _gRepository.GetDataBySQL("DbDati", "SELECT * From vFRM_InvioOrdiniCliList", parameters.parameters);
                    if (documenti.Count == 0) { sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non lo trovo!!!"); sw.Flush(); return false; }
                    string docs = documenti[0].GetPropertyString("Docs");

                    //_logger.LogInformation($"leggo l'ordine {res.root} da WS");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: leggo l'ordine " + res.root.ToString());
                    GetOrderResponse order = _miscServiceBase.CallWS_JSON_mode<GetOrderResponse>(url + "/" + res.root.ToString(), token, Method.Get);
                    if (order == null) return false;
                    if (!order.success) return false;
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: trovato");

                    OrderFileResponse zip = order.data.files.FirstOrDefault(f => f.mimetype == "application/zip");
                    if (zip == null) return false;
                    //_logger.LogInformation($"Trovato lo zip nei file collegati: {zip.id}");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Trovato lo zip nei file collegati: {zip.id}");

                    OrderFileGetResponse zipFile = _miscServiceBase.CallWS_JSON_mode<OrderFileGetResponse>(url + "/file/" + zip.id.ToString(), token, Method.Get);
                    if (zipFile == null) return false;
                    if (!zipFile.success) return false;
                    //_logger.LogInformation($"scaricato il file zip {zip.name}");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: scaricato il file zip {zip.name}");

                    string email = user[0].GetPropertyString("sender");

                    //System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                    //System.Net.Mail.SmtpClient objSMTP = new System.Net.Mail.SmtpClient
                    //{
                    //    Host = "192.168.0.5",
                    //    Port = 25,
                    //};
                    //if (email == "documentazione.bw@tempor.it") email = "documentazione.bw@attalgroup.it";
                    //email = email.Replace("documentazione.bw", "postmaster");
                    SmtpClient objSMTP = new SmtpClient
                    {
                        Host = "pro.eu.turbo-smtp.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Credentials = new NetworkCredential("gruppoedp@abeaform.it", "ddHu39eX")
                    };
                    MailMessage objMail = new MailMessage();
                    objMail.Sender = new MailAddress(email, email);
                    objMail.From = new MailAddress(email, email);
                    objMail.ReplyToList.Add(new MailAddress(email));

                    objMail.Subject = $"Documenti firmati per {_gRepository.domain} Spa";
                    objMail.IsBodyHtml = true;
                    objMail.BodyEncoding = System.Text.Encoding.UTF8;
                    objMail.Body = $"Gentilissima/o {nome},<br>in allegato a questa mail trova i seguenti documenti sottoscritti da conservare:<br>" +
                        $"<ul>{docs}</ul><br><br>" +
                        $"Cordiali saluti<br>{_gRepository.domain} Spa";
                    objMail.To.Add(new MailAddress(Dest));

                    // File in formato Base64
                    string fileBase64 = zipFile.data.contentToBase64;
                    // Decodifica della stringa Base64 in byte array
                    byte[] fileBytes = Convert.FromBase64String(fileBase64);
                    // Creazione dell'allegato
                    Attachment allegato = new Attachment(new MemoryStream(fileBytes), zip.filename);
                    objMail.Attachments.Add(allegato);

                    //_logger.LogInformation($"inviata la mail a {Dest}");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: inviata la mail a {Dest}");
                    sw.Flush();

                    objSMTP.Send(objMail);
                    return true;
                }
                else
                {
                    //_logger.LogInformation($"Qualcosa + andato storto con spFRM_DocsRiceviFirma");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: DOH!!!! ");
                    sw.Flush();
                    return false;
                }
            }
            catch (Exception ex)
            {
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nel processo ( {ex.Message} )");
                sw.Flush();
                return false;
            }
        }
    }
}
