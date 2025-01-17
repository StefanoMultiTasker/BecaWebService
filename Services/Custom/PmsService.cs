using Azure.Core.Serialization;
using BecaWebService.ExtensionsLib;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Contracts.Custom;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Diagnostics;
using System.Dynamic;
using static iText.IO.Image.Jpeg2000ImageData;

namespace BecaWebService.Services.Custom
{
    public class PmsService : IPmsService
    {
        private readonly ISharedService _miscServiceBase;
        private readonly IGenericRepository _gRepository;
        private readonly ILoggerManager _logger;
        public PmsService(ISharedService miscServiceBase, IGenericRepository genRepository, ILoggerManager logger)
        {
            _miscServiceBase = miscServiceBase;
            _gRepository = genRepository;
            _logger = logger;   
        }
        public async Task<bool> pms(pmsJson pmsJson, string json, StreamWriter sw)
        {
            try
            {
                sw.WriteLine($"APL: {_gRepository.domain}");

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: aggiorno PMS_JSON ");
                string sql = "Update PMS_JSON Set JSON = {2} Where idAttivita = {0} And user_process_id = {1}";

                List<object> pars = new List<object>();
                pars.Add(pmsJson.external_user_id);
                pars.Add(pmsJson.user_process_id);
                pars.Add(json);

                _gRepository.ExecuteSqlCommand("DbDati", sql, pars.ToArray());

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo vPMS_Processi_da_Avviare");
                BecaParameters parameters = new BecaParameters();
                parameters.Add("idAttivita", pmsJson.external_user_id);
                List<object> processes = _gRepository.GetDataBySQL("DbDati", "SELECT * From vPMS_Processi_da_Avviare", parameters.parameters);

                if (processes.Count == 0)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non ce ne sono");
                    return true;
                }

                foreach (object process in processes)
                {
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Eseguo spPMS_Avvia_Processo {process.GetPropertyString("template_process_idDaAvviare")}");

                    parameters = new BecaParameters();
                    parameters.Add("idAttivita", pmsJson.external_user_id);
                    parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                    parameters.Add("user_email", process.GetPropertyString("email"));
                    parameters.Add("communication_link", true);
                    parameters.Add("communication_email", true);
                    int res = await _gRepository.ExecuteProcedure("DbDati", "spPMS_Avvia_Processo", parameters.parameters);

                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo spPMS_Parametri_e_Valori_Avvia_Processo {pmsJson.external_user_id} {process.GetPropertyString("template_process_idDaAvviare")}");

                    parameters = new BecaParameters();
                    parameters.Add("idAttivita", pmsJson.external_user_id);
                    parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));

                    List<object> pmsParams = _gRepository.GetDataBySP<object>("DbDati", "spPMS_Parametri_e_Valori_Avvia_Processo", parameters.parameters);

                    string sParamas = "";
                    dynamic content = new ExpandoObject();
                    object param = null;

                    if (pmsParams.Count > 0)
                    {
                        param = pmsParams[0];
                        sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Preparo i parametri");

                        // Converte `param` in JSON
                        string jsonParam = JsonConvert.SerializeObject(param);
                        sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {jsonParam}");

                        // Converte il JSON in un dizionario
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonParam);

                        // Rimuovi il campo indesiderato (ad esempio, "campoDaEscludere")
                        dictionary.Remove("external_user_id");

                        // Converte il dizionario in un ExpandoObject, che può essere assegnato a `content`
                        foreach (var kvp in dictionary)
                        {
                            if (kvp.Key == "id_card_savino")
                            {
                                ((IDictionary<string, object>)content).Add(kvp.Key, await getPMSFile(kvp.Value.ToString(), sw));
                            }
                            else
                            {
                                ((IDictionary<string, object>)content).Add(kvp.Key, kvp.Value);
                            }
                        }

                        parameters = new BecaParameters();
                        parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                        List<object> pmsParamsDict = _gRepository.GetDataBySQL("DbDati", "Select * From PMS_ParamDict", parameters.parameters);
                        if (pmsParamsDict.Count > 0)
                        {
                            List<string> keys = pmsParamsDict[0].GetPropertyString("Keys").Split(",").Skip(5).ToList();
                            foreach (string key in keys)
                            {
                                sParamas += @$"""{param.GetPropertyString(key)}"", ";
                            }
                            sParamas = "[" + sParamas.left(sParamas.Length - 2) + "]";
                        }
                    }

                    pmsPostData pmsPostData = new pmsPostData();
                    pmsPostData.external_user_id = int.Parse(param.GetPropertyString("external_user_id"));
                    pmsPostData.email = process.GetPropertyString("email");
                    pmsPostData.apl = process.GetPropertyString("apl");
                    pmsPostData.communication = process.GetPropertyString("communication").Split(",").Select(c => c.Replace(@"""", "").Replace(" ", "")).ToList();
                    pmsPostData.content = content;

                    pmsPost body = new pmsPost();
                    body.template_process_id = int.Parse(process.GetPropertyString("template_process_idDaAvviare"));
                    body.data = new List<pmsPostData>
                    {
                        pmsPostData
                    };

                    string jsonbody = JsonConvert.SerializeObject(body);

                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Avvio il processo su PMS");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {jsonbody}");
                    pmsPostResponse pmsRes = _miscServiceBase.CallWS_JSON_mode<pmsPostResponse>("https://api.pms.attalgroup.it/pms/processes", "", Method.Post, body);

                    string pmsResJson = JsonConvert.SerializeObject(pmsRes);
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: La risposta è la seguente");
                    sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {pmsResJson}");

                    if (pmsRes != null)
                    {
                        if (pmsRes.response == null || pmsRes.response.Count == 0)
                        {
                            sw.WriteLine("L'array response è vuoto");
                            await SaveError(pmsJson.external_user_id, pmsJson.user_process_id, "Response data vuoto", sw);
                        }
                        else
                        {
                            pmsPostResponseData pmsResData = pmsRes.response[0];
                            parameters = new BecaParameters();
                            parameters.Add("idAttivita", pmsResData.external_user_id);
                            parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                            parameters.Add("user_process_id", pmsResData.user_process_id);
                            parameters.Add("user_steps_id", "[" + string.Join(",", pmsResData.user_step_ids ?? new List<int>()) + "]");
                            parameters.Add("link", pmsResData.link);
                            parameters.Add("parametri_processo", sParamas);
                            parameters.Add("PWDI", 1);

                            sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Eseguo spPMS_Avvia_Processo2");
                            res = await _gRepository.ExecuteProcedure("DbDati", "spPMS_Avvia_Processo2", parameters.parameters);
                        }
                    }
                    else
                    {
                        sw.WriteLine("PMS non ha risposto");
                        await SaveError(pmsJson.external_user_id, pmsJson.user_process_id, "Risposta non valida", sw);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nel processo ( {ex.Message} )");
                sw.Flush();
                await SaveError(pmsJson.external_user_id, pmsJson.user_process_id, ex.Message, sw);
                return false;
            }
        }

        public async Task<GenericResponse> ValidaFase(int idAttivita, int user_process_id) {
            return await AvviaProcessoSuccessivo(idAttivita, user_process_id, null);
        }

        private async Task<GenericResponse> AvviaProcessoSuccessivo(int idAttivita, int user_process_id, StreamWriter sw)
        {
            try
            {
                if(sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo vPMS_Processi_da_Avviare");
                BecaParameters parameters = new BecaParameters();
                parameters.Add("idAttivita", idAttivita);
                parameters.Add("apl", _gRepository.GetActiveCompany().MainFolder);
                List<object> processes = _gRepository.GetDataBySQL("MainDB", "SELECT * From vPMS_Processi_da_Avviare", parameters.parameters);

                if (processes.Count == 0)
                {
                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non ce ne sono");
                    return new GenericResponse(true);
                }

                foreach (object process in processes)
                {
                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Eseguo spPMS_Avvia_Processo {process.GetPropertyString("template_process_idDaAvviare")}");

                    parameters = new BecaParameters();
                    parameters.Add("idAttivita", idAttivita);
                    parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                    parameters.Add("user_email", process.GetPropertyString("email"));
                    parameters.Add("communication_link", true);
                    parameters.Add("communication_email", true);
                    int res = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Avvia_Processo", parameters.parameters);

                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo spPMS_Parametri_e_Valori_Avvia_Processo {idAttivita} {process.GetPropertyString("template_process_idDaAvviare")}");

                    parameters = new BecaParameters();
                    parameters.Add("idAttivita", idAttivita);
                    parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));

                    List<object> pmsParams = _gRepository.GetDataBySP<object>("MainDB", "spPMS_Parametri_e_Valori_Avvia_Processo", parameters.parameters);

                    string sParamas = "";
                    dynamic content = new ExpandoObject();
                    object param = null;

                    if (pmsParams.Count > 0)
                    {
                        param = pmsParams[0];
                        if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Preparo i parametri");

                        // Converte `param` in JSON
                        string jsonParam = JsonConvert.SerializeObject(param);
                        if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {jsonParam}");

                        // Converte il JSON in un dizionario
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonParam);

                        // Rimuovi il campo indesiderato (ad esempio, "campoDaEscludere")
                        dictionary.Remove("external_user_id");

                        // Converte il dizionario in un ExpandoObject, che può essere assegnato a `content`
                        foreach (var kvp in dictionary)
                        {
                            if (kvp.Key == "id_card_savino")
                            {
                                ((IDictionary<string, object>)content).Add(kvp.Key, await getPMSFile(kvp.Value.ToString(), sw));
                            }
                            else
                            {
                                ((IDictionary<string, object>)content).Add(kvp.Key, kvp.Value);
                            }
                        }

                        parameters = new BecaParameters();
                        parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                        List<object> pmsParamsDict = _gRepository.GetDataBySQL("MainDB", "Select * From PMS_ParamDict", parameters.parameters);
                        if (pmsParamsDict.Count > 0)
                        {
                            List<string> keys = pmsParamsDict[0].GetPropertyString("Keys").Split(",").Skip(5).ToList();
                            foreach (string key in keys)
                            {
                                sParamas += @$"""{param.GetPropertyString(key)}"", ";
                            }
                            sParamas = "[" + sParamas.left(sParamas.Length - 2) + "]";
                        }
                    }

                    pmsPostData pmsPostData = new pmsPostData();
                    pmsPostData.external_user_id = int.Parse(param.GetPropertyString("external_user_id"));
                    pmsPostData.email = process.GetPropertyString("email");
                    pmsPostData.apl = process.GetPropertyString("apl");
                    pmsPostData.communication = process.GetPropertyString("communication").Split(",").Select(c => c.Replace(@"""", "").Replace(" ", "")).ToList();
                    pmsPostData.content = content;

                    pmsPost body = new pmsPost();
                    body.template_process_id = int.Parse(process.GetPropertyString("template_process_idDaAvviare"));
                    body.data = new List<pmsPostData>
                    {
                        pmsPostData
                    };

                    string jsonbody = JsonConvert.SerializeObject(body);

                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Avvio il processo su PMS");
                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {jsonbody}");
                    pmsPostResponse pmsRes = _miscServiceBase.CallWS_JSON_mode<pmsPostResponse>("https://api.pms.attalgroup.it/pms/processes", "", Method.Post, body);

                    string pmsResJson = JsonConvert.SerializeObject(pmsRes);
                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: La risposta è la seguente");
                    if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {pmsResJson}");

                    if (pmsRes != null)
                    {
                        if (pmsRes.response == null || pmsRes.response.Count == 0)
                        {
                            if (sw != null) sw.WriteLine("L'array response è vuoto");
                            await SaveError(idAttivita, user_process_id, "Response data vuoto", sw);
                            return "La risposta è PMS vuota".toResponse();
                        }
                        if (pmsRes.response[0].errors != null)
                        {
                            string jsonError = JsonConvert.SerializeObject(pmsRes.response[0].errors);
                            _logger.LogError($"La risposta è un errore: {jsonError}");
                            return $"La risposta è un errore: {jsonError}".toResponse();
                        }
                        else
                        {
                            pmsPostResponseData pmsResData = pmsRes.response[0];
                            parameters = new BecaParameters();
                            parameters.Add("idAttivita", pmsResData.external_user_id);
                            parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                            parameters.Add("user_process_id", pmsResData.user_process_id);
                            parameters.Add("user_steps_id", "[" + string.Join(",", pmsResData.user_step_ids ?? new List<int>()) + "]");
                            parameters.Add("link", pmsResData.link);
                            parameters.Add("parametri_processo", sParamas.Replace("'","''"));
                            parameters.Add("PWDI", 1);

                            if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Eseguo spPMS_Avvia_Processo2");
                            res = await _gRepository.ExecuteProcedure("DbDati", "spPMS_Avvia_Processo2", parameters.parameters);
                        }
                    }
                    else
                    {
                        if (sw != null) sw.WriteLine("PMS non ha risposto");
                        await SaveError(idAttivita, user_process_id, "Risposta non valida", sw);
                        return "La risposta è PMS non è valida".toResponse();
                    }
                }
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nel processo ( {ex.Message} )");
                if (sw != null) sw.Flush();
                await SaveError(idAttivita, user_process_id, ex.Message, sw);
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> AvviaProcesso(pmsAvviaProcesso avvio)
        {
            try
            {
                _logger.LogDebug($"Avvio l'attività per il soggetto {avvio.email}");
                _logger.LogDebug("Eseguo spPMS_Avvia_Attivita");

                BecaParameters parameters = new BecaParameters();
                parameters = new BecaParameters();
                parameters.Add("idAnagAttivita", avvio.idAnagAttivita);
                parameters.Add("apl", avvio.apl);
                parameters.Add("cdff", avvio.cdff);
                parameters.Add("pwdi", _gRepository.GetLoggedUser().idUtenteLoc(_gRepository.GetActiveCompany().idCompany));
                parameters.Add("user_email", avvio.email);
                parameters.Add("communication_link", true);
                parameters.Add("communication_email", true);
                parameters.Add("codiceanagrafica", null);
                parameters.Add("tipoanagrafica", "L");
                List<object> res1 =  _gRepository.GetDataBySP<object>("MainDB", "spPMS_Avvia_Attivita", parameters.parameters);
                if (res1.Count == 0) {
                    return "spPMS_Avvia_Attivita ha dato esito negativo".toResponse();
                }
                int idAttivita = int.Parse(res1[0].GetPropertyString("idAttivita"));

                _logger.LogDebug("Avvio PMS");

                List<object> apl = _gRepository.GetDataBySQL("DbDati", "Select * From ANSDA", null);
                if (apl.Count == 0) { return "Non trovo i dati dell'APL".toResponse(); }

                parameters = new BecaParameters();
                parameters.Add("cdff", avvio.cdff);
                List<object> filiali = _gRepository.GetDataBySQL("DbDati", "Select * From ANTEX", parameters.parameters);
                if (filiali.Count == 0) { return "Non trovo i dati della filiale".toResponse(); }

                dynamic data = new ExpandoObject();

                data.lavoratore_identificativo = avvio.data.lavoratore_identificativo;
                data.cell_provvisorio = avvio.data.cell_provvisorio;
                data.cdff = avvio.cdff;
                data.comune_filiale = filiali[0].GetPropertyString("COFL");
                data.aplP = apl[0].GetPropertyString("RSSL");
                data.piva_apl = apl[0].GetPropertyString("PISL");
                data.comune_sede_legale = apl[0].GetPropertyString("COSL");
                data.indirizzo_sede_legale = apl[0].GetPropertyString("INSL");
                data.data_odierna = DateTime.Today.ToString("dd/MM/yyyy");
                data.anno_corrente = DateTime.Today.ToString("yyyy");

                pmsPostData pmsPostData = new pmsPostData()
                {
                    external_user_id = idAttivita,
                    email = avvio.email,
                    apl = avvio.apl,
                    communication = new List<string>() { "link", "email" },
                    content = data
                };

                pmsPost body = new pmsPost()
                {
                    template_process_id = 5,
                    data = new List<pmsPostData> { pmsPostData }
                };

                string jsonbody = JsonConvert.SerializeObject(body);
                _logger.LogDebug($"{jsonbody}");

                pmsPostResponse pmsRes = _miscServiceBase.CallWS_JSON_mode<pmsPostResponse>("https://api.pms.attalgroup.it/pms/processes", "", Method.Post, body);

                string pmsResJson = JsonConvert.SerializeObject(pmsRes);
                _logger.LogDebug($"Risposta: {pmsResJson}");

                if (pmsRes != null)
                {
                    if (pmsRes.response == null || pmsRes.response.Count == 0)
                    {
                        _logger.LogError($"La risposta è vuota");
                        return "La risposta è PMS vuota".toResponse();
                    }
                    if (pmsRes.response[0].errors != null)
                    {
                        string jsonError = JsonConvert.SerializeObject(pmsRes.response[0].errors);
                        _logger.LogError($"La risposta è un errore: {jsonError}");
                        return $"La risposta è un errore: {jsonError}".toResponse();
                    }
                    else
                    {
                        pmsPostResponseData pmsResData = pmsRes.response[0];

                        //var rowDictionary = data as JObject;
                        //string sParamas = "";

                        //if (rowDictionary != null)
                        //{
                        //    // Seleziona le proprietà a partire dall'ottava in avanti
                        //    var columnValues = rowDictionary
                        //                        .Properties().Where(p => p.Name.ToLower() != "email") // Ottieni le proprietà dell'oggetto JObject
                        //                                      //.Skip(7) // Salta le prime 7 proprietà
                        //                        .Select(prop => @"""" + (prop.Value?.ToString() ?? "NULL") + @""""); // Trasforma i valori in stringa, usando "NULL" se il valore è null

                        //    sParamas = "[" + string.Join(", ", columnValues) + "]";

                        //    // Log o utilizzo del risultato
                        //    Console.WriteLine(sParamas);
                        //}
                        var rowDictionary = (IDictionary<string, object>)data;

                        // Seleziona le colonne partendo dall'ottava in avanti
                        var columnValues = rowDictionary
                                            .Where(p => p.Key.ToLower() != "email")
                                            .Select(kvp => @"""" + (kvp.Value?.ToString() ?? "NULL") + @"""");  // Trasforma i valori in stringa, usando "NULL" se il valore è null

                        string sParamas = "[" + string.Join(", ", columnValues) + "]";

                        _logger.LogDebug("Eseguo spPMS_Avvia_Processo2");

                        parameters = new BecaParameters();
                        parameters.Add("idAttivita", idAttivita);
                        parameters.Add("template_process_id", 5);
                        parameters.Add("user_process_id", pmsResData.user_process_id);
                        parameters.Add("user_steps_id", "[" + string.Join(",", (pmsResData.user_step_ids ?? new List<int>())) + "]");
                        parameters.Add("link", pmsResData.link);
                        parameters.Add("parametri_processo", sParamas);
                        parameters.Add("PWDI", _gRepository.GetLoggedUser().idUtenteLoc(_gRepository.GetActiveCompany().idCompany));
                        int res2 = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Avvia_Processo2", parameters.parameters);
                    }
                }
                else
                {
                    _logger.LogDebug($"La risposta è nulla");
                    return "La risposta è PMS nulla".toResponse();
                }
                return true.toResponse();
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Non ha funzionato: {ex.Message}");
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> InvalidaFasi(pmsInvalidaFasi fasi)
        {
            try {
                pmsInvalidaFasiJson pmsInvalidaFasiJson = new pmsInvalidaFasiJson()
                {
                    data = new List<pmsInvalidaFasi> { fasi }
                };
                var pmsRes = _miscServiceBase.CallWS_JSON_mode<pmsPostResponse>("https://api.pms.attalgroup.it/pms/processes/update-user-processes", "", Method.Post, pmsInvalidaFasiJson);

                BecaParameters parameters = new BecaParameters();
                parameters.Add("user_process_id", fasi.user_process_id);
                parameters.Add("user_process_status", "Reinviato all'utente");
                int res2 = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Processo_aggiorna_stato", parameters.parameters);
          
                return new GenericResponse(true);
            }
            catch (Exception ex) { return ex.Message.toResponse(); }
        }


        private async Task<int> SaveError(int external_user_id, int user_process_id, string err, StreamWriter sw)
        {
            if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Scrivo l'errore: {err} ");
            string sql = "Insert Into PMS_Errori (external_user_id, user_process_id) Values ({0}, {1})";

            List<object> pars = new List<object>();
            pars.Add(external_user_id);
            pars.Add(user_process_id);

            return await _gRepository.ExecuteSqlCommandAsync("MainDB", sql, pars.ToArray());
        }
        private async Task<string> getPMSFile(string fileUrl, StreamWriter sw)
        {
            if(sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: scarico il file ({fileUrl}) ");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Scarica il file
                    byte[] fileBytes = await client.GetByteArrayAsync(fileUrl);
                    // Converte l'array di byte in una stringa Base64
                    string fileBase64 = Convert.ToBase64String(fileBytes);
                    return fileBase64;
                }
            }
            catch (Exception ex)
            {
                if (sw != null) sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: non riesco a scaricare il file ({ex.Message}) ");
                return "";
            }
        }
    }
}
