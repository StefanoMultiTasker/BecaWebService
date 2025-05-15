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
using System.IO;
using static iText.IO.Image.Jpeg2000ImageData;

namespace BecaWebService.Services.Custom
{
    public class PmsService(ISharedService miscServiceBase, IGenericRepository genRepository, ILoggerManager logger) : IPmsService
    {
        private readonly ISharedService _miscServiceBase = miscServiceBase;
        private readonly IGenericRepository _gRepository = genRepository;
        private readonly ILoggerManager _logger = logger;

        public async Task<bool> pms(pmsJson pmsJson, string json, StreamWriter sw)
        {
            try
            {
                sw.WriteLine($"APL: {_gRepository.domain}");

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: aggiorno PMS_JSON ");
                string sql = "Update PMS_JSON Set JSON = {2} Where idAttivita = {0} And user_process_id = {1}";

                List<object> pars = [pmsJson.external_user_id, pmsJson.user_process_id, json];

                GenericResponse res = await AvviaProcessoSuccessivo(pmsJson.external_user_id, pmsJson.user_process_id, sw);

                _gRepository.ExecuteSqlCommand("DbDati", sql, [.. pars]);

                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo vPMS_Processi_da_Avviare");
                return res.Success;
            }
            catch (Exception ex)
            {
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nel processo ( {ex.Message} )");
                sw.Flush();
                await SaveError(pmsJson.external_user_id, pmsJson.user_process_id, ex.Message, sw);
                return false;
            }
        }

        public async Task<GenericResponse> RiavviaAttivita(int idAttivita, int user_process_id)
        {
            return await AvviaProcessoSuccessivo(idAttivita, user_process_id, null);
        }

        public async Task<GenericResponse> ValidaFase(int idAttivita, int user_process_id)
        {
            return await AvviaProcessoSuccessivo(idAttivita, user_process_id, null);
        }

        private async Task<GenericResponse> AvviaProcessoSuccessivo(int idAttivita, int user_process_id, StreamWriter? sw)
        {
            try
            {
                sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo vPMS_Processi_da_Avviare");
                BecaParameters parameters = new();
                parameters.Add("idAttivita", idAttivita);
                parameters.Add("user_process_id", user_process_id);
                List<object> processes = _gRepository.GetDataBySQL("MainDB", $"SELECT * From dbo.fnPMS_Processi_da_Avviare({idAttivita}, {user_process_id})", parameters.parameters);

                if (processes.Count == 0)
                {
                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Non ce ne sono");
                    return new GenericResponse(true);
                }

                foreach (object process in processes)
                {
                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Eseguo spPMS_Avvia_Processo {process.GetPropertyString("template_process_idDaAvviare")}");

                    parameters = new BecaParameters();
                    parameters.Add("idAttivita", idAttivita);
                    parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                    parameters.Add("user_email", process.GetPropertyString("email"));
                    parameters.Add("communication_link", true);
                    parameters.Add("communication_email", true);
                    int res = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Avvia_Processo", parameters.parameters);

                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Leggo spPMS_Parametri_e_Valori_Avvia_Processo {idAttivita} {process.GetPropertyString("template_process_idDaAvviare")}");

                    parameters = new BecaParameters();
                    parameters.Add("idAttivita", idAttivita);
                    parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));

                    List<object> pmsParams = _gRepository.GetDataBySP<object>("MainDB", "spPMS_Parametri_e_Valori_Avvia_Processo", parameters.parameters);

                    string sParamas = "";
                    dynamic content = new ExpandoObject();
                    object? param = null;

                    if (pmsParams.Count > 0)
                    {
                        param = pmsParams[0];
                        sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Preparo i parametri");

                        // Converte `param` in JSON
                        string jsonParam = JsonConvert.SerializeObject(param);
                        sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {jsonParam}");

                        // Converte il JSON in un dizionario
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object?>>(jsonParam);

                        // Rimuovi il campo indesiderato (ad esempio, "campoDaEscludere")
                        dictionary!.Remove("external_user_id");

                        // Converte il dizionario in un ExpandoObject, che può essere assegnato a `content`
                        foreach (var kvp in dictionary)
                        {
                            if (kvp.Key == "id_card_savino")
                            {
                                ((IDictionary<string, object?>)content).Add(kvp.Key, kvp.Value == null ? null : await GetPMSFile(kvp.Value!.ToString(), sw));
                            }
                            else
                            {
                                ((IDictionary<string, object?>)content).Add(kvp.Key, kvp.Value);
                            }
                        }

                        parameters = new BecaParameters();
                        parameters.Add("template_process_id", process.GetPropertyString("template_process_idDaAvviare"));
                        List<object> pmsParamsDict = _gRepository.GetDataBySQL("MainDB", "Select * From vPMS_ParamDict", parameters.parameters);
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

                    pmsPostData pmsPostData = new()
                    {
                        external_user_id = int.Parse(param.GetPropertyString("external_user_id")),
                        email = process.GetPropertyString("email"),
                        apl = process.GetPropertyString("apl"),
                        communication = process.GetPropertyString("communication").Split(",").Select(c => c.Replace(@"""", "").Replace(" ", "")).ToList(),
                        content = content
                    };

                    pmsPost body = new()
                    {
                        template_process_id = int.Parse(process.GetPropertyString("template_process_idDaAvviare")),
                        data =
                    [
                        pmsPostData
                    ]
                    };

                    string jsonbody = JsonConvert.SerializeObject(body);

                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Avvio il processo su PMS");
                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {jsonbody}");
                    pmsPostResponse pmsRes = _miscServiceBase.CallWS_JSON_mode<pmsPostResponse>("https://api.pms.attalgroup.it/pms/processes", "", Method.Post, body);

                    string pmsResJson = JsonConvert.SerializeObject(pmsRes);
                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: La risposta è la seguente");
                    sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: {pmsResJson}");

                    if (pmsRes != null)
                    {
                        if (pmsRes.response == null || pmsRes.response.Count == 0)
                        {
                            sw?.WriteLine("L'array response è vuoto");
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
                            parameters.Add("user_steps_id", "[" + string.Join(",", pmsResData.user_step_ids ?? []) + "]");
                            parameters.Add("link", pmsResData.link);
                            parameters.Add("parametri_processo", sParamas.Replace("'", "''"));
                            parameters.Add("PWDI", 1);

                            sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Eseguo spPMS_Avvia_Processo2");
                            res = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Avvia_Processo2", parameters.parameters);
                        }
                    }
                    else
                    {
                        sw?.WriteLine("PMS non ha risposto");
                        await SaveError(idAttivita, user_process_id, "Risposta non valida", sw);
                        return "La risposta è PMS non è valida".toResponse();
                    }
                }
                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nel processo ( {ex.Message} )");
                sw?.Flush();
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

                int idUtenteAvvio = avvio.idUtenteAvvio; // _gRepository.GetLoggedUser().idUtenteLoc(_gRepository.GetActiveCompany().idCompany);

                BecaParameters parameters = new();
                parameters = new BecaParameters();
                parameters.Add("idAnagAttivita", avvio.idAnagAttivita);

                List<object> attivita = _gRepository.GetDataBySQL("MainDB", "Select * From PMS_AnagAttivita", parameters.parameters,false,true);
                if (attivita.Count == 0) { return "Non trovo l'attività".toResponse(); }

                string sourcesql = attivita[0].GetPropertyString("sourcesql");
                if (sourcesql != string.Empty)
                {
                    string keys = attivita[0].GetPropertyString("keys").ToLower();
                    if(keys == string.Empty) { return "Non trovo le chiavi dell'attività".toResponse(); }

                    var dataDict = ((IEnumerable<JProperty>)avvio.data.Properties()).ToDictionary(p => p.Name.ToLower(), p => p.Value);

                    parameters = new BecaParameters();
                    foreach(string k in keys.Split(","))
                    {
                        if (dataDict.TryGetValue(k, out var jsonValue))
                        {
                            parameters.Add(k, ((Newtonsoft.Json.Linq.JValue)jsonValue).Value);
                        }
                    }
                    List<object> attivitaData = _gRepository.GetDataBySQL("MainDB", $"Select * From {sourcesql}", parameters.parameters,false,true);
                    if(attivitaData.Count == 0) { return "Non trovo i dati dell'attività".toResponse(); }
                    if(attivitaData.Count > 1) { return "Trovo troppi dati per l'avvio dell'attività".toResponse(); }

                    avvio.data = attivitaData[0];
                }

                parameters = new BecaParameters();
                parameters.Add("idAnagAttivita", avvio.idAnagAttivita);
                parameters.Add("apl", avvio.apl);
                parameters.Add("cdff", avvio.cdff);
                parameters.Add("pwdi", idUtenteAvvio);
                parameters.Add("user_email", avvio.email);
                parameters.Add("communication_link", true);
                parameters.Add("communication_email", true);
                parameters.Add("codiceanagrafica", null);
                parameters.Add("tipoanagrafica", "L");
                List<object> res1 = _gRepository.GetDataBySP<object>("MainDB", "spPMS_Avvia_Attivita", parameters.parameters);
                if (res1.Count == 0)
                {
                    return "spPMS_Avvia_Attivita ha dato esito negativo".toResponse();
                }
                int idAttivita = int.Parse(res1[0].GetPropertyString("idAttivita"));

                _logger.LogDebug("Avvio PMS");

                List<object> apl = _gRepository.GetDataBySQL("DbDati", "Select * From ANSDA", []);
                if (apl.Count == 0) { return "Non trovo i dati dell'APL".toResponse(); }

                parameters = new BecaParameters();
                parameters.Add("CDFF", avvio.cdff);
                List<object> filiali = _gRepository.GetDataBySQL("DbDati", "Select * From ANTEX", parameters.parameters);
                if (filiali.Count == 0) { return "Non trovo i dati della filiale".toResponse(); }

                parameters = new BecaParameters();
                parameters.Add("idAnagAttivita", avvio.idAnagAttivita);
                parameters.Add("OrdineEsecuzione", 1);
                List<object> processi = _gRepository.GetDataBySQL("MainDB", "Select * From PMS_AnagAttivitaProcessi", parameters.parameters);
                if (processi.Count == 0) { return "Non trovo il processo di avvio".toResponse(); }
                int processo = int.Parse(processi[0].GetPropertyString("template_process_id"));
                bool communication_link = (bool)processi[0].GetPropertyValue("communication_link");

                dynamic data = new ExpandoObject();
                var dict = (IDictionary<string, object?>)data;
                parameters = new BecaParameters();
                parameters.Add("template_process_id", processo);
                List<object> parametri = _gRepository.GetDataBySQL("MainDB", "Select * From vPMS_ParamDict", parameters.parameters);
                if (parametri == null || parametri.Count == 0) { return "Non trovo il parametri di avvio".toResponse(); }

                if (avvio.data is JObject)
                    avvio.data = JsonConvert.DeserializeObject<ExpandoObject>(avvio.data.ToString());

                var propsDict = (IDictionary<string, object?>)avvio.data; // ((IEnumerable<JProperty>)avvio.data.Properties()).ToDictionary(p => p.Name, p => p.Value);
                foreach (string k in parametri[0].GetPropertyString("Keys").Split(",").Skip(5))
                {
                    if (propsDict.TryGetValue(k, out var jsonValue))
                    {
                        //dict[k] = ((Newtonsoft.Json.Linq.JValue)jsonValue).Value;
                        dict[k] = jsonValue;
                    }
                    else
                    {
                        switch (k)
                        {
                            case "cdff": dict[k] = avvio.cdff; break;
                            case "comune_filiale": dict[k] = filiali[0].GetPropertyString("COFL"); break;
                            case "aplP": dict[k] = apl[0].GetPropertyString("RSSL"); break;
                            case "piva_apl": dict[k] = apl[0].GetPropertyString("PISL"); break;
                            case "comune_sede_legale": dict[k] = apl[0].GetPropertyString("COSL"); break;
                            case "indirizzo_sede_legale": dict[k] = apl[0].GetPropertyString("INSL"); break;
                            case "ora_adesso": dict[k] = DateTime.Now.ToString("HH:mm:ss"); break;
                            case "data_odierna": dict[k] = DateTime.Today.ToString("dd/MM/yyyy"); break;
                            case "anno_corrente": dict[k] = DateTime.Today.ToString("yyyy"); break;
                            default: break;
                        }
                    }
                }
                if (propsDict.ContainsKey("email")) avvio.email = avvio.data.email ?? "";

                pmsPostData pmsPostData = new()
                {
                    external_user_id = idAttivita,
                    email = avvio.email ?? "",
                    apl = avvio.apl,
                    communication = new List<string>() {
                        (bool)processi[0].GetPropertyValue("communication_link") ? "link" : "",
                        (bool)processi[0].GetPropertyValue("communication_email") ? "email" : ""
                    }.Where(c => c != "").ToList(),
                    content = data
                };

                pmsPost body = new()
                {
                    template_process_id = processo,
                    data = [pmsPostData]
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
                        var rowDictionary = (IDictionary<string, object>)data;

                        // Seleziona le colonne partendo dall'ottava in avanti
                        var columnValues = rowDictionary
                                            //.Where(p => p.Key.ToLower() != "email")
                                            .Select(kvp => @"""" + (kvp.Value?.ToString() ?? "NULL") + @"""");  // Trasforma i valori in stringa, usando "NULL" se il valore è null

                        string sParamas = "[" + string.Join(", ", columnValues) + "]";

                        _logger.LogDebug("Eseguo spPMS_Avvia_Processo2");

                        parameters = new BecaParameters();
                        parameters.Add("idAttivita", idAttivita);
                        parameters.Add("template_process_id", processo);
                        parameters.Add("user_process_id", pmsResData.user_process_id);
                        parameters.Add("user_steps_id", "[" + string.Join(",", (pmsResData.user_step_ids ?? [])) + "]");
                        parameters.Add("link", pmsResData.link);
                        parameters.Add("parametri_processo", sParamas);
                        parameters.Add("PWDI", idUtenteAvvio);
                        int res2 = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Avvia_Processo2", parameters.parameters);
                    }
                }
                else
                {
                    _logger.LogDebug($"La risposta è nulla");
                    return "La risposta è PMS nulla".toResponse();
                }
                return new GenericResponse(new { pmsRes.response[0].link });
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Non ha funzionato: {ex.Message}");
                return ex.Message.toResponse();
            }
        }

        public async Task<GenericResponse> InvalidaFasi(pmsInvalidaFasi fasi)
        {
            try
            {
                pmsInvalidaFasiJson pmsInvalidaFasiJson = new()
                {
                    data = [fasi]
                };
                var pmsRes = _miscServiceBase.CallWS_JSON_mode<pmsPostResponse>("https://api.pms.attalgroup.it/pms/processes/update-user-processes", "", Method.Post, pmsInvalidaFasiJson);

                BecaParameters parameters = new();
                parameters.Add("user_process_id", fasi.user_process_id);
                parameters.Add("user_process_status", "Reinviato all'utente");
                int res2 = await _gRepository.ExecuteProcedure("MainDB", "spPMS_Processo_aggiorna_stato", parameters.parameters);

                return new GenericResponse(true);
            }
            catch (Exception ex) { return ex.Message.toResponse(); }
        }

        public async Task<GenericResponse> getFileFromPMS(string url)
        {
            try
            {
                string base64 = await GetPMSFile(url, null);
                // Converti la stringa Base64 in un array di byte
                byte[] bytes = Convert.FromBase64String(base64);
                // Determina il MIME type dai primi byte del file
                string mimeType = GetMimeType(bytes);

                // Crea un MemoryStream a partire dall'array di byte
                MemoryStream stream = new(bytes);
                // Esempio: leggere il contenuto del MemoryStream e convertirlo in stringa
                using (StreamReader reader = new(stream))
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result); // Output: Hello, World!
                }
                return new GenericResponse(new { pdf = stream, mimeType });
            }
            catch (Exception ex)
            {
                return new GenericResponse($"non riesco ad accedere al file: {ex.Message} "); // - {ex.InnerException.Message}
            }
        }

        // Metodo per determinare il MIME type in base ai magic numbers
        private static string GetMimeType(byte[] fileBytes)
        {
            if (fileBytes.Length > 4)
            {
                if (fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46) // %PDF
                {
                    return "application/pdf";
                }
                else if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8) // JPEG
                {
                    return "image/jpeg";
                }
                else if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47) // PNG
                {
                    return "image/png";
                }
            }
            return "application/octet-stream"; // Fallback per file sconosciuti
        }


        private async Task<int> SaveError(int external_user_id, int user_process_id, string err, StreamWriter? sw)
        {
            sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Scrivo l'errore: {err} ");
            string sql = "Insert Into PMS_Errori (external_user_id, user_process_id) Values ({0}, {1})";

            List<object> pars = [external_user_id, user_process_id];

            return await _gRepository.ExecuteSqlCommandAsync("MainDB", sql, [.. pars]);
        }
        private static async Task<string> GetPMSFile(string? fileUrl, StreamWriter? sw)
        {
            if ((fileUrl ?? "") == "") return "";
            sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: scarico il file ({fileUrl}) ");
            try
            {
                using HttpClient client = new();
                // Scarica il file
                byte[] fileBytes = await client.GetByteArrayAsync(fileUrl);
                // Converte l'array di byte in una stringa Base64
                string fileBase64 = Convert.ToBase64String(fileBytes);
                return fileBase64;
            }
            catch (Exception ex)
            {
                sw?.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: non riesco a scaricare il file ({ex.Message}) ");
                return "";
            }
        }
    }
}
