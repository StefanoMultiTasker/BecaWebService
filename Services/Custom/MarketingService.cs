using BecaWebService.ExtensionsLib;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Contracts.Custom;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using Microsoft.AspNetCore.Identity;
using Org.BouncyCastle.Utilities.Collections;
using System.Net.Mail;

namespace BecaWebService.Services.Custom
{
    public class MarketingService : IMarketingService
    {
        private readonly IGenericRepository _gRepository;
        private readonly ILoggerManager _logger;
        private readonly IMailService _mailService;
        public MarketingService(IGenericRepository genRepository, IMailService mailService, ILoggerManager logger)
        {
            _gRepository = genRepository;
            _mailService = mailService;
            _logger = logger;
        }

        public async Task<GenericResponse> DossierMail(DossierMail invio)
        {
            if (_gRepository.GetLoggedUser() == null) return "Non sei loggato".toResponse();
            if (_gRepository.GetActiveCompany() == null) return "Non hai specificato la company".toResponse();

            try
            {
                if ((invio.destinatari == null || invio.destinatari.Count == 0) && invio.codRS == null)
                {
                    return new GenericResponse("Nessun destinatario o R&S definito");
                }

                string list = "";
                //è un invio da proattiva
                if ((invio.codRS ?? "") == "")
                {
                    BecaParameters aPar = new BecaParameters();
                    foreach (DossierMailDestinatari destinatario in invio.destinatari!.Where(d => d.ffcl != null))
                    {
                        list += (list == "" ? "" : ";").ToString() + destinatario.eMail;
                        int idUser = _gRepository.GetLoggedUser()!.idUtenteLoc(_gRepository.GetActiveCompany()!.idCompany) ?? 1;
                        List<object> azione =
                        [
                            destinatario.ffcl ?? "",
                            destinatario.codc ?? "",
                            invio.cdff,
                            idUser,
                            invio.azione,
                            invio.azione,
                            DateTime.Now,
                            DateTime.Now,
                            invio.oggetto,
                            "E",
                            invio.idDossier,
                            idUser,
                        ];
                        try
                        {
                            string sql = "Insert Into MK_ATTIVITA (FFCL, CODC, CDFF, OPERAT, Azione, Contatto, DataProgrammata, DataEsecuzione, Oggetto, Stato, idDossier, PWDI) " +
                                "Values (" +
                                string.Join(", ", Enumerable.Range(0, 12).Select(i => $"{{{i}}}")) +
                                ")";
                            int res = await _gRepository.ExecuteSqlCommandAsync("DbDati", sql, azione.ToArray());
                            //object azioneNew = await _gRepository.AddDataByForm<object>("Marketing", azione);
                            destinatario.esitoAzione = "";
                        }
                        catch (Exception ex)
                        {
                            destinatario.esitoAzione = ex.Message;
                        }
                    }
                }
                else
                {
                    List<object> parameters =
                    [
                        invio.codRS.left(3),
                        invio.codRS!.Substring(3, 4),
                        invio.codRS.right(4),
                        invio.idDossier,
                        DateTime.Now,
                    ];
                    string sql = "Update CONTR_Selezione_CV " +
                        "Set dtInvioIns = {4} " +
                        "Where CDFF = {0} And AACT = {1} And CDNN = {2} " +
                        "And idDossier = {3}";
                    bool res = await _gRepository.ExecuteSqlCommandAsync("DbDati", sql, parameters.ToArray()) > 0 ? true : false;
                }
                list = "";
                if (invio.destinatari != null)
                {
                    foreach (DossierMailDestinatari destinatario in invio.destinatari)
                    {
                        list += (list == "" ? "" : ";").ToString() + destinatario.eMail;
                    }

                    //int maxLen = 1500;
                    string err = "";
                    //while (list.Length > maxLen)
                    foreach (string email in list.Split(";"))
                    {
                        GenericResponse res1 = this.SendDossier(invio, email);
                        if (!res1.Success) err += (err.Length == 0 ? "" : System.Environment.NewLine) + res1.Message;
                    }
                    if (err != "") return new GenericResponse(err);
                }

                return new GenericResponse(invio);
            }
            catch (Exception ex)
            {
                return ex.Message.toResponse();
            }
        }

        private GenericResponse SendDossier(DossierMail invio, string email)
        {
            try
            {
                SendMailOptions options = new SendMailOptions()
                {
                    Sender = new SendMailOptionsOrigin() { Type = "Filiale", Name = invio.cdff },
                    Subject = new SendMailOptionsOrigin() { Type = "*", Value = invio.oggetto },
                    Text = new SendMailOptionsOrigin() { Type = "*", Value = invio.messaggio },
                };
                if (email.Contains(";"))
                {
                    options.Dest = new SendMailOptionsOrigin() { Type = "Filiale", Name = invio.cdff };
                    options.CCN = new SendMailOptionsOrigin() { Type = "EMail", Value = email };
                }
                else
                {
                    options.Dest = new SendMailOptionsOrigin() { Type = "EMail", Value = email };
                    options.CCN = new SendMailOptionsOrigin() { Type = "Filiale", Name = invio.cdff };
                }
                return _mailService.Send(options);
            }
            catch (Exception ex)
            {
                return new GenericResponse($"{ex.Message}");
            }
        }
    }
}
