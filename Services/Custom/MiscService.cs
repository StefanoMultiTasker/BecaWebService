using Newtonsoft.Json;
using System.Net;
using BecaWebService.Models.Communications;
using RestSharp;
using Entities.Models.Custom;
using Contracts.Custom;
using Contracts;
using Entities.Models;

namespace BecaWebService.Services.Custom
{
    public class MiscService : IMiscService
    {
        //private readonly Dictionary<string, object> _subServices;
        IPresenzeService presenzeService;
        ISavinoService savinoService;
        IPmsService pmsService;
        ILavorService lavorService;
        IDocumentiService documentiService;
        IMarketingService marketingService;
        IPrintService printService;
        IMailService mailService;

        private readonly ILoggerManager _logger;
        public MiscService(IPresenzeService _presenzeService, ISavinoService _savinoService,
                IPmsService _pmsService, ILavorService _lavorService,
                IDocumentiService _documentiService, IMarketingService _marketingService,
                IPrintService _printService, IMailService _mailService,
                ILoggerManager logger)
        {
            presenzeService = _presenzeService;
            savinoService = _savinoService;
            pmsService = _pmsService;
            lavorService = _lavorService;
            documentiService = _documentiService;
            marketingService = _marketingService;
            printService = _printService;
            mailService = _mailService;

            _logger = logger;
        }

        #region "Presenze#
        public async Task<GenericResponse> UploadPresenze(int idOrologio, string aaco, string mmco, IFormFile file) => 
            await presenzeService.UploadPresenze(idOrologio, aaco, mmco, file);

        public async Task<GenericResponse> ImportaPresenze(int idOrologio) => await presenzeService.ImportaPresenze(idOrologio);

        public GenericResponse PrintPresenze(
                string aaco, string mmco,
                string cdff = null, string aact = null, string cdnn = null, string cdmt = null,
                string ffcl = null, string codc = null, string cdc = null,
                string nome = null) => presenzeService.PrintPresenze(aaco, mmco, cdff,aact,cdnn,cdmt,ffcl,codc,cdc,nome);

        #endregion

        #region "Lavor"
        public GenericResponse ListCUByCodFisc()
        {
            return lavorService.ListCUByCodFisc();
        }

        public GenericResponse LavorSendRequestMail(string subject, string text)
        {
            return lavorService.LavorSendRequestMail(subject, text);
        }

        #endregion

        #region "Firme"
        public GenericResponse SavinoOTP(SavinoOTP res, StreamWriter sw)
        {
            return savinoService.SavinoOTP(res, sw);
        }

        public async Task<bool> SavinoFirma(SavinoFirma res, StreamWriter sw)
        {
            return await savinoService.SavinoFirma(res, sw);
        }

        #endregion

        #region "PMS"
        public async Task<bool> pms(pmsJson pmsJson, string json, StreamWriter sw) => await pmsService.pms(pmsJson, json, sw);
        public async Task<GenericResponse> RiavviaAttivita(int idAttivita, int user_process_id) => await pmsService.RiavviaAttivita(idAttivita, user_process_id);
        public async Task<GenericResponse> AvviaProcesso(pmsAvviaProcesso avvio) => await pmsService.AvviaProcesso(avvio);
        public async Task<GenericResponse> ValidaFase(int idAttivita, int user_process_id) => await pmsService.ValidaFase(idAttivita, user_process_id);
        public async Task<GenericResponse> InvalidaFasi(pmsInvalidaFasi fasi) => await pmsService.InvalidaFasi(fasi);
        public async Task<GenericResponse> getFileFromPMS(string url)=> await pmsService.getFileFromPMS(url);

        #endregion

        #region "PreparaDocumenti"

        public GenericResponse PreparaEbitemp(List<Matricole4Ebitemp> matricole) => documentiService.PreparaEbitemp(matricole);

        public async Task<GenericResponse> PreparaDocumenti(string PeriodoInizio, string PeriodoFine, List<string> Matricole, bool IncludeCU, string? Folder = null) =>
            await documentiService.PreparaDocumenti(PeriodoInizio, PeriodoFine, Matricole, IncludeCU, Folder);

        #endregion

        #region "Marketing"
        public async Task<GenericResponse> DossierMail(DossierMail invio) => await marketingService.DossierMail(invio);
        #endregion

        public GenericResponse PrintModule(string modulo, List<BecaParameter> parameters) => printService.PrintModule(modulo, parameters);

        public GenericResponse Send(SendMailOptions options) => mailService.Send(options);
    }
}
