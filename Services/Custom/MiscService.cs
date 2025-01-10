using Newtonsoft.Json;
using System.Net;
using BecaWebService.Models.Communications;
using RestSharp;
using Entities.Models.Custom;
using Contracts.Custom;

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

        private readonly ILogger<MiscService> _logger;
        public MiscService(IPresenzeService _presenzeService, ISavinoService _savinoService,
                IPmsService _pmsService, ILavorService _lavorService,
                IDocumentiService _documentiService,
                ILogger<MiscService> logger)
        {
            presenzeService = _presenzeService;
            savinoService = _savinoService;
            pmsService = _pmsService;
            lavorService = _lavorService;
            documentiService = _documentiService;

            _logger = logger;
        }

        private IEnumerable<Type> GetSubServiceTypes()
        {
            // Puoi personalizzare il filtro per includere solo i sottoservizi desiderati
            return new[] { typeof(IPresenzeService), typeof(ISavinoService), typeof(IPmsService), typeof(ILavorService), typeof(IDocumentiService) };
        }

        #region "Presenze#
        public async Task<GenericResponse> UploadPresenze(int idOrologio, string aaco, string mmco, IFormFile file)
        {
            return await presenzeService.UploadPresenze(idOrologio, aaco, mmco, file);
        }

        public async Task<GenericResponse> ImportaPresenze(int idOrologio)
        {
            return await presenzeService.ImportaPresenze(idOrologio);
        }

        public GenericResponse PrintPresenze(
                string aaco, string mmco,
                string cdff = null, string aact = null, string cdnn = null, string cdmt = null,
                string ffcl = null, string codc = null, string cdc = null,
                string nome = null)
        {
            return presenzeService.PrintPresenze(aaco, mmco, cdff,aact,cdnn,cdmt,ffcl,codc,cdc,nome);
        }

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
        public async Task<GenericResponse> AvviaProcesso(pmsAvviaProcesso avvio) => await pmsService.AvviaProcesso(avvio);
        public async Task<GenericResponse> InvalidaFasi(pmsInvalidaFasi fasi) => await pmsService.InvalidaFasi(fasi);

        #endregion

        #region "PreparaDocumenti"

        public GenericResponse PreparaEbitemp(List<Matricole4Ebitemp> matricole)
        {
            return documentiService.PreparaEbitemp(matricole);
        }

        public async Task<GenericResponse> PreparaDocumenti(string PeriodoInizio, string PeriodoFine, List<string> Matricole, bool IncludeCU, string? Folder = null)
        {
            return await documentiService.PreparaDocumenti(PeriodoInizio, PeriodoFine, Matricole, IncludeCU, Folder);
        }
            #endregion

        }
    }
