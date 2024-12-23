using BecaWebService.ExtensionsLib;
using BecaWebService.Models.Communications;
using Entities.Models;
using ExtensionsLib;
using iText.Forms.Fields;
using iText.Forms;
using iText.IO.Source;
using iText.Kernel.Pdf;
using RestSharp;
using Contracts;
using Entities.Models.Custom;
using Contracts.Custom;

namespace BecaWebService.Services.Custom
{
    public class PresenzeService : IPresenzeService
    {
        private readonly IGenericRepository _gRepository;
        private IWebHostEnvironment _env;
        private readonly ILogger<MiscService> _logger;
        public PresenzeService(IGenericRepository genRepository, IWebHostEnvironment env, ILogger<MiscService> logger)
        {
            _gRepository = genRepository;
            _env = env;
            _logger = logger;
        }
        public async Task<GenericResponse> UploadPresenze(int idOrologio, string aaco, string mmco, IFormFile file)
        {
            try
            {
                BecaParameters parameters = new BecaParameters();
                parameters.Add("idOrologio", idOrologio);
                List<object> orologi = _gRepository.GetDataBySQL("DbDati", "SELECT * From AnagOrologi_Test", parameters.parameters);
                if (orologi.Count == 0) return new GenericResponse("Orologio non trovato");

                object orologio = orologi[0];
                string tipo = orologio.GetPropertyValue("Tipo").ToString();
                string folderName = "";
                string fileName = "";
                switch (tipo)
                {
                    case "1":
                        folderName = orologio.GetPropertyValue("PathFile").ToString();
                        fileName = orologio.GetPropertyValue("FileName").ToString();
                        if (fileName.Contains(";"))
                        {
                            fileName = fileName.ToLower().Split(";").FirstOrDefault(n => file.FileName.ToLower().Contains(n) || n.Contains(file.FileName.ToLower()));
                            break;
                        }
                        break;
                    case "2":
                        folderName = orologio.GetPropertyValue("PathFile").ToString();
                        fileName = orologio.GetPropertyValue("FFCL").ToString() +
                            orologio.GetPropertyValue("CODC").ToString() +
                            orologio.GetPropertyValue("idOrologio").ToString() +
                            "." + orologio.GetPropertyValue("FileExtension").ToString();
                        break;
                }
                string fullName = Path.Combine(folderName, fileName);

                if (!Directory.Exists(folderName)) Directory.CreateDirectory(folderName);
                if (File.Exists(fullName)) File.Delete(fullName);

                using (FileStream writer = File.Create(fullName))
                {
                    await file.CopyToAsync(writer);
                    writer.Close();
                }

                if (tipo == "1") return new GenericResponse(true);

                parameters.Add("AACO", aaco);
                parameters.Add("MMCO", mmco);
                int res = 0;
                List<object> _status = _gRepository.GetDataBySQL("DbDati", "SELECT * From PresenzeImportStatus", parameters.parameters);

                List<object> pars = new List<object>();
                pars.Add(idOrologio);
                pars.Add(aaco);
                pars.Add(mmco);
                if (_status.Count == 0)
                {
                    res = _gRepository.ExecuteSqlCommand("DbDati", "Insert Into PresenzeImportStatus (AACO, MMCO, idOrologio, Status) " +
                        "Values ({1}, {2}, {0}, 'Uploaded')", pars.ToArray());
                }
                else
                {
                    res = _gRepository.ExecuteSqlCommand("DbDati", "Update PresenzeImportStatus Set Status = 'Uploaded' " +
                        "Where AACO = {1} And MMCO = {2} And idOrologio = {0}", pars.ToArray());
                }
                await _gRepository.CompleteAsync();

                return new GenericResponse(true);
            }
            catch (Exception ex)
            {
                return new GenericResponse($"{ex.InnerException.Message}");
            }
        }

        public async Task<GenericResponse> ImportaPresenze(int idOrologio)
        {
            try
            {
                BecaParameters parameters = new BecaParameters();
                parameters.Add("idOrologio", idOrologio);
                List<object> orologi = _gRepository.GetDataBySQL("DbDati", "SELECT * From AnagOrologi_Test", parameters.parameters);
                if (orologi.Count == 0) return new GenericResponse("Orologio non trovato");

                object orologio = orologi[0];
                string fileName = orologio.GetPropertyValue("QWFileName").ToString();
                string apl = _gRepository.GetActiveCompany().MainFolder;

                string url = $@"http://192.168.0.146/RunServerApp/api/qv?apl={apl}&name={fileName}.qvw";
                RestClient client = new RestClient(url);
                //client.Timeout = -1;
                RestRequest request = new RestRequest("", Method.Get);
                request.Timeout = TimeSpan.FromMinutes(10);
                //request.AddHeader("Content-Type", "application/json");
                //request.AddHeader("Accept", "application/json");
                //string body = $"'timestamp':'1635344504','token':'eac70bffb22d9c2aeb82b4544e7bb8f8','candidate':'{TalentumEmail}'";
                //request.AddJsonBody("{" + body.Replace("'", @"""") + "}");
                RestResponse response = await client.ExecuteAsync(request);
                string res = response.Content;
                if (res.left(3) == "\"ok")
                    return new GenericResponse(true);
                else
                    return new GenericResponse(response.Content);
            }
            catch (Exception ex)
            {
                return new GenericResponse(ex.Message);
            }
        }

        public GenericResponse PrintPresenze(
                string aaco, string mmco,
                string cdff = null, string aact = null, string cdnn = null, string cdmt = null,
                string ffcl = null, string codc = null, string cdc = null,
                string nome = null)
        {
            string step = "avvio";
            //Log.Information($"PrintPresenze avvio");
            //Log.Information($"PrintPresenze parameters: " +
            //    "AACO = " + aaco + ", MMCO = " +mmco +
            //    (cdff == null ? "" : "CDFF = " + cdff) +
            //    (aact == null ? "" : "AACT = " + aact) +
            //    (cdnn == null ? "" : "CDNN = " + cdnn) +
            //    (cdmt == null ? "" : "CDMT = " + cdmt) +
            //    (ffcl == null ? "" : "FFCL = " + ffcl) +
            //    (codc == null ? "" : "CODC = " + codc) +
            //    (cdc == null ? "" : "CDC = " + cdc) +
            //    (nome == null ? "" : "Nome = " + nome) 
            //    );
            try
            {
                step = "clear";
                //ClearTemp();

                step = "get data";
                List<BecaParameter> par = new List<BecaParameter>();
                par.Add(new BecaParameter("idUtente", _gRepository.GetLoggedUser().idUtente));
                par.Add(new BecaParameter("AACO", aaco));
                par.Add(new BecaParameter("MMCO", mmco));
                if (cdff != null) par.Add(new BecaParameter("CDFF", cdff));
                if (aact != null) par.Add(new BecaParameter("AACT", aact));
                if (cdnn != null) par.Add(new BecaParameter("CDNN", cdnn));
                if (cdmt != null) par.Add(new BecaParameter("CDMT", cdmt));
                if (ffcl != null) par.Add(new BecaParameter("FFCL", ffcl));
                if (codc != null) par.Add(new BecaParameter("CODC", codc));
                if (cdc != null) par.Add(new BecaParameter("CDC", cdc));
                if (nome != null) par.Add(new BecaParameter("Nome", nome));
                List<object> data = _gRepository.GetDataBySP<object>("DbDati", "spPreMeseStampaAngular", par);
                //Log.Information($"PrintPresenze trovati {data.Count} cartellini");

                if (data == null || data.Count == 0) return new GenericResponse("Nessuna cartellino stampabile trovato");

                string baseFolder = _gRepository.GetActiveCompany().MainFolder != "localhost" ?
                    Path.Combine(_env.ContentRootPath) :
                    Path.Combine("E:", "BecaWeb");
                baseFolder = Path.Combine("E:", "BecaWeb");
                string folderName = Path.Combine(baseFolder, "Web", "Download", _gRepository.GetActiveCompany().MainFolder);
                string sourceName = Path.Combine(folderName, "Cartellino.pdf");
                string tempFolder = Path.Combine(baseFolder, "Web", "Download", "_TEMP");
                string tempName = Path.Combine(tempFolder, "Cartellino_" +
                    _gRepository.GetActiveCompany().MainFolder +
                    _gRepository.GetLoggedUser().idUtente.ToString() +
                    DateTime.Now.Ticks.ToString()) + ".pdf";

                step = "get module " + sourceName;
                //Log.Information($"PrintPresenze open pdf module {sourceName}");
                MemoryStream stream = new MemoryStream();
                PdfDocument destPdfDocumentSmartMode = new PdfDocument(new PdfWriter(stream).SetSmartMode(true));

                int count = 0;
                foreach (object o in data)
                {
                    ByteArrayOutputStream baos = new ByteArrayOutputStream();
                    PdfDocument sourcePdfDocument = new PdfDocument(new PdfReader(sourceName), new PdfWriter(baos));
                    //Read fields
                    PdfAcroForm form = PdfAcroForm.GetAcroForm(sourcePdfDocument, true);
                    IDictionary<string, PdfFormField> fields = form.GetAllFormFields();
                    //Fill out fields
                    //PdfFormField toSet;
                    foreach (PdfFormField field in fields.Values)
                    {
                        object val = o.GetPropertyValue(field.GetFieldName().ToString());
                        if (val != null)
                            field.SetValue(val.ToString());
                        //fields.TryGetValue(field.GetFieldName().ToString(), out toSet);
                        //toSet.SetValue(tokenizer.NextToken());
                    }
                    //Flatten fields
                    form.FlattenFields();
                    sourcePdfDocument.Close();
                    sourcePdfDocument = new PdfDocument(new PdfReader(new MemoryStream(baos.ToArray())));
                    //Copy pages
                    sourcePdfDocument.CopyPagesTo(1, sourcePdfDocument.GetNumberOfPages(), destPdfDocumentSmartMode, null);
                    sourcePdfDocument.Close();
                    count++;
                    //Log.Information($"PrintPresenze compiled {count}° form");
                }
                destPdfDocumentSmartMode.Close();

                step = "return pdf";
                //Log.Information($"PrintPresenze return pdf");
                return new GenericResponse(new { pdf = stream });
            }
            catch (Exception ex)
            {
                return new GenericResponse($"{step}: {ex.Message}"); // - {ex.InnerException.Message}
            }
        }

        private void ClearTemp()
        {
            string baseFolder = _gRepository.GetActiveCompany().MainFolder != "localhost" ?
                    Path.Combine(_env.ContentRootPath) :
                    Path.Combine("E:", "BecaWeb");
            string tempFolder = Path.Combine(baseFolder, "Web", "Download", "_TEMP");
            foreach (string sFile in Directory.GetFiles(tempFolder))
            {
                if (File.GetCreationTime(sFile) < DateTime.Today) File.Delete(sFile);
            }
        }
    }
}
