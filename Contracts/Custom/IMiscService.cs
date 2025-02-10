using BecaWebService.Models.Communications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Entities.Models.Custom;
using RestSharp;
using Entities.Models;

namespace Contracts.Custom
{
    public interface ISharedService
    {
        T CallWS_JSON_mode<T>(string url, string token, Method method = Method.Get, object body = null);
    }
    public interface IPmsService
    {
        Task<bool> pms(pmsJson pmsJson, string json, StreamWriter sw);
        Task<GenericResponse> AvviaProcesso(pmsAvviaProcesso avvio);
        Task<GenericResponse> ValidaFase(int idAttivita, int user_process_id);
        Task<GenericResponse> InvalidaFasi(pmsInvalidaFasi fasi);
        Task<GenericResponse> getFileFromPMS(string url);
    }
    public interface IPresenzeService
    {
        Task<GenericResponse> UploadPresenze(int idOrologio, string aaco, string mmco, Microsoft.AspNetCore.Http.IFormFile file);
        Task<GenericResponse> ImportaPresenze(int idOrologio);
        GenericResponse PrintPresenze(
            string aaco, string mmco,
            string cdff = null, string aact = null, string cdnn = null, string cdmt = null,
            string ffcl = null, string codc = null, string cdc = null,
            string nome = null);
    }
    public interface ISavinoService
    {
        GenericResponse SavinoOTP(SavinoOTP res, StreamWriter sw);
        Task<bool> SavinoFirma(SavinoFirma res, StreamWriter sw);
    }
    public interface IDocumentiService
    {
        Task<GenericResponse> PreparaDocumenti(string PeriodoInizio, string PeriodoFine, List<string> Matricole, bool IncludeCU, string Folder = null);
        GenericResponse PreparaEbitemp(List<Matricole4Ebitemp> matricole);
    }
    public interface ILavorService
    {
        GenericResponse ListCUByCodFisc();
        GenericResponse LavorSendRequestMail(string subject, string text);
    }

    public interface IPrintService
    {
        GenericResponse PrintModule(string modulo, List<BecaParameter> parameters);
    }
    public interface IMiscService : IDocumentiService, IPresenzeService, ISavinoService, IPmsService, ILavorService, IPrintService
    {
        //Task<GenericResponse> UploadPresenze(int idOrologio, string aaco, string mmco, Microsoft.AspNetCore.Http.IFormFile file);
        //Task<GenericResponse> ImportaPresenze(int idOrologio);
        //GenericResponse PrintPresenze(
        //    string aaco, string mmco,
        //    string cdff = null, string aact = null, string cdnn = null, string cdmt = null,
        //    string ffcl = null, string codc = null, string cdc = null,
        //    string nome = null);
        //GenericResponse ListCUByCodFisc();
        //GenericResponse LavorSendRequestMail(string subject, string text);
        //GenericResponse SavinoOTP(SavinoOTP res, StreamWriter sw);
        //Task<bool> SavinoFirma(SavinoFirma res, StreamWriter sw);
        //Task<bool> pms(pmsJson pmsJson, string json, StreamWriter sw);
    }

}
