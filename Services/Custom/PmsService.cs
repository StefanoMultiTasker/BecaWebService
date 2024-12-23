using BecaWebService.ExtensionsLib;
using BecaWebService.Models.Communications;
using Contracts;
using Contracts.Custom;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using Newtonsoft.Json;
using RestSharp;
using System.Dynamic;

namespace BecaWebService.Services.Custom
{
    public class PmsService : IPmsService
    {
        private readonly ISharedService _miscServiceBase;
        private readonly IGenericRepository _gRepository;
        private readonly ILogger<MiscService> _logger;
        public PmsService(ISharedService miscServiceBase, IGenericRepository genRepository, ILogger<MiscService> logger)
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
                            await SaveError(pmsJson, "Response data vuoto", sw);
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
                        await SaveError(pmsJson, "Risposta non valida", sw);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Errore nel processo ( {ex.Message} )");
                sw.Flush();
                await SaveError(pmsJson, ex.Message, sw);
                return false;
            }
        }

        private async Task<int> SaveError(pmsJson pmsJson, string err, StreamWriter sw)
        {
            sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: Scrivo l'errore: {err} ");
            string sql = "Insert Into PMS_Errori (external_user_id, user_process_id) Values ({0}, {1})";

            List<object> pars = new List<object>();
            pars.Add(pmsJson.external_user_id);
            pars.Add(pmsJson.user_process_id);

            return await _gRepository.ExecuteSqlCommandAsync("DbDati", sql, pars.ToArray());
        }
        private async Task<string> getPMSFile(string fileUrl, StreamWriter sw)
        {
            sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: scarico il file ({fileUrl}) ");
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
                sw.WriteLine($"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}: non riesco a scaricare il file ({ex.Message}) ");
                return "";
            }
        }
    }
}
