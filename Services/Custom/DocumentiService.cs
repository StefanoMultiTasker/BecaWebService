using BecaWebService.ExtensionsLib;
using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Contracts.Custom;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System.Globalization;
using System.IO;
using System.IO.Compression;

namespace BecaWebService.Services.Custom
{
    public class DocumentiService : IDocumentiService
    {
        private readonly IGenericRepository _gRepository;
        private IWebHostEnvironment _env;
        private IConfiguration _cfg;
        private readonly ILogger<MiscService> _logger;
        public DocumentiService(IGenericRepository genRepository, IWebHostEnvironment env, IConfiguration Configuration, ILogger<MiscService> logger)
        {
            _gRepository = genRepository;
            _env = env;
            _cfg = Configuration;
            _logger = logger;
        }

        public GenericResponse PreparaEbitemp(List<Matricole4Ebitemp> matricole)
        {
            if (matricole == null || matricole.Count == 0) { return "nessuna matricola fornita".toResponse();  }
            try
            {
                string section = _env.IsDevelopment() ? "AppSettingsCustomLocal" : "AppSettingsCustom";
                string _pathSource = _cfg.GetSection($"{section}:IspezioniPathCedolini").Value ?? "";
                string _pathDest = (_cfg.GetSection($"{section}:PathEbitemp").Value ?? "")
                        .Replace("#soc#", _gRepository.GetActiveCompany().MainFolder);
                List<Matricole4Ebitemp> errori = new List<Matricole4Ebitemp>();
                List<string> files = new List<string>();

                foreach (Matricole4Ebitemp matricola in matricole)
                {
                    string pathSource = Path.Combine(
                        _pathSource.Replace("#soc#", _gRepository.GetActiveCompany().MainFolder),
                        $"{matricola.anno}_{matricola.mese}",
                        $"{matricola.matricola}.pdf");
                    string pathDest = _pathDest
                        .Replace("#YYYY#", matricole[0].anno)
                        .Replace("#MM#", matricole[0].mese); ;

                    if (!System.IO.Directory.Exists(pathDest))
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(pathDest);
                        }
                        catch (Exception ex)
                        {
                            return ($"C'è un problema nella creazione della cartella {pathSource}: {ex.Message}").toResponse();
                        }
                    }
                    if (System.IO.File.Exists(pathSource))
                    {
                        try
                        {
                            System.IO.File.Copy(pathSource, Path.Combine(pathDest, $"{matricola.matricola}.pdf"), true);
                        }
                        catch (Exception ex)
                        {
                            return ($"C'è un problema nella copia dei file nella cartella {pathDest}: {ex.Message}").toResponse();
                        }
                    }
                    else
                    {
                        files.Add(pathSource);
                        errori.Add(matricola);
                    }
                }
                EbitempResponse res = new EbitempResponse() { 
                    pathDest = _pathDest,
                    errori = errori,
                    files = files
                };
                return res.toResponse();
            }
            catch (Exception ex) { return ex.Message.toResponse(); }
        }
        public async Task<GenericResponse> PreparaDocumenti(string PeriodoInizio, string PeriodoFine, List<string> Matricole, bool IncludeCU, string? Folder = null)
        {
            if (Matricole == null || Matricole.Count() == 0) return new GenericResponse("Non sono ancora state indicate le matricole");
            if (PeriodoInizio.isNullOrempty() || PeriodoFine.isNullOrempty()) return new GenericResponse("Non sono stati forniti i periodi di inizio e/o fine");
            if (DateTime.TryParseExact($"{PeriodoInizio}01", "yyyyMMdd", new CultureInfo("it-IT"), DateTimeStyles.None, out DateTime dateValueI) == false)
                return new GenericResponse("Il periodo di partenza (anno e mese) non è valido");
            if (DateTime.TryParseExact($"{PeriodoFine}01", "yyyyMMdd", new CultureInfo("it-IT"), DateTimeStyles.None, out DateTime dateValueF) == false)
                return new GenericResponse("Il periodo di fine (anno e mese) non è valido");

            BecaParameters parameters = new BecaParameters();
            parameters.Add("matricole", string.Join(",", Matricole));

            try
            {
                int res = await _gRepository.ExecuteProcedure("DbDati", "spMatricole4DocPrepara", parameters.parameters);
                if (res == 0) return new GenericResponse("Problemi durante la compilazione della tabella di appoggio");
            }
            catch (Exception ex) { return new GenericResponse($"Problemi durante la compilazione della tabella di appoggio: {ex.Message}"); }

            List<Matricola> matricole;
            try
            {
                List<object> list = _gRepository.GetDataBySQL("DbDati", "Select * From Matricole4Doc Order By IDEMPLOY", (new BecaParameters()).parameters);
                if (list == null || list.Count() == 0) return new GenericResponse("Problemi durante la lettura dei dati");
                matricole = convertToClass(list);
            }
            catch (Exception ex) { return new GenericResponse($"Problemi durante la lettura dei dati: {ex.Message}"); }

            string section = _env.IsDevelopment() ? "AppSettingsCustomLocal" : "AppSettingsCustom";
            string pathDest = _cfg.GetSection($"{section}:IspezioniPathDest").Value ?? "";
            string pathCedo = (_cfg.GetSection($"{section}:IspezioniPathCedolini").Value ?? "").Replace("#soc#", _gRepository.GetActiveCompany().MainFolder);
            string pathLUL = (_cfg.GetSection($"{section}:IspezioniPathLUL").Value ?? "").Replace("#soc#", _gRepository.GetActiveCompany().MainFolder);
            string pathCU = (_cfg.GetSection($"{section}:IspezioniPathCU").Value ?? "").Replace("#soc#", _gRepository.GetActiveCompany().MainFolder);

            if (pathDest.isNullOrempty() || !Directory.Exists(pathCU)) return new GenericResponse($"Problemi nell'accesso alla cartella di destinazione ({pathDest})");

            if (pathCedo.isNullOrempty() || !Directory.Exists(pathCedo)) return new GenericResponse("Problemi nell'accesso alla cartella dei cedolini");
            if (pathLUL.isNullOrempty() || !Directory.Exists(pathLUL)) return new GenericResponse("Problemi nell'accesso alla cartella dei LUL");
            if (pathCU.isNullOrempty() || !Directory.Exists(pathCU)) return new GenericResponse("Problemi nell'accesso alla cartella delle CU");

            pathDest = Path.Combine(pathDest, DateTime.Now.ToString("yyyyMMddHHmmss"));
            if (!Directory.Exists(pathDest)) Directory.CreateDirectory(pathDest);


            string[] periodi = Enumerable
                .Range(0, (DateTime.ParseExact(PeriodoFine, "yyyyMM", null)
                              .Subtract(DateTime.ParseExact(PeriodoInizio, "yyyyMM", null))
                              .Days / 30) + 1)
                .Select(i => DateTime.ParseExact(PeriodoInizio, "yyyyMM", null).AddMonths(i).ToString("yyyyMM"))
                .ToArray();

            string result = "";
            foreach (string periodo in periodi)
            {
                result += CopyFiles(pathDest, pathCedo, pathLUL, pathCU, IncludeCU, periodo.left(4), periodo.right(2), matricole);
            }

            DocumentiResponse response = new DocumentiResponse()
            {
                zip = GetDirectoryAsBase64(pathDest),
                message = result != "" ? $"Problemi durante la copia dei file: {result}" : result,
                path = pathDest,
            };
            return response.toResponse();
        }

        private List<Matricola> convertToClass(List<object> matricole)
        {
            return matricole.Select(item => new Matricola()
            {
                idEmploy = item.GetPropertyString("IDEMPLOY"),
                matricola = item.GetPropertyString("Matricola"),
                DataInizio = (DateTime)item.GetPropertyValue("DataInizio"),
                DataFine = (DateTime)item.GetPropertyValue("DataFine"),
                CF = item.GetPropertyString("CF")
            }).ToList();
        }

        private string CopyFiles(string pathDest, string pathCedo, string pathLUL, string pathCU, bool IncludeCU, string anno, string mese, List<Matricola> matricole)
        {
            string periodoTFR = DateTime.ParseExact($"{anno}{mese}", "yyyyMM", null).AddMonths(1).ToString("yyyyMM");
            string pathTFR = Path.Combine(pathLUL, periodoTFR.left(4), periodoTFR.right(2));
            pathLUL = Path.Combine(pathLUL, anno, mese);
            pathCedo = Path.Combine(pathCedo, $"{anno}_{mese}");

            string pathDestCedo = Path.Combine(pathDest, $"{anno}_{mese}");
            string pathDestTFR = Path.Combine(pathDest, $"{periodoTFR.left(4)}_{periodoTFR.right(2)}");
            string pathDestCU = Path.Combine(pathDest, $"{anno}_CU");

            if (!Directory.Exists(pathDestCedo)) Directory.CreateDirectory(pathDestCedo);

            string result = "";
            try
            {
                foreach (Matricola matricola in matricole)
                {
                    DateTime dataConfronto = new DateTime(int.Parse(anno), int.Parse(mese), 1);
                    if (string.Compare(matricola.DataInizio.ToString("yyyyMM"), $"{anno}{mese}") <= 0 && string.Compare(matricola.DataFine.ToString("yyyyMM"), $"{anno}{mese}") > 0)
                    {
                        List<string> files = Directory.GetFiles(pathLUL).Where(f => f.Contains(matricola.idEmploy) && f.ToLower().right(4) == ".pdf").ToList();
                        if (files.Count > 0)
                        {
                            File.Copy(Path.Combine(pathLUL, files[0]), Path.Combine(pathDestCedo, $"{matricola.idEmploy}_1_LUL.pdf"), true);
                        }
                        else
                        {
                            result += $"{matricola.idEmploy} {anno}_{mese}: manca il LUL\r\n";
                            string nameCedo = Path.Combine(pathCedo, $"{matricola.matricola}.pdf");
                            if (File.Exists(nameCedo))
                            {
                                File.Copy(nameCedo, Path.Combine(pathDestCedo, $"{matricola.idEmploy}_2_Cedolino.pdf"), true);
                            }
                            else
                            {
                                result += $"{matricola.idEmploy} {anno}_{mese}: manca il Cedolino\r\n";
                            }
                        }
                        string name13 = $"{matricola.matricola}T.pdf";
                        if (File.Exists(Path.Combine(pathCedo, name13)))
                        {
                            File.Copy(Path.Combine(pathCedo, name13), Path.Combine(pathDestCedo, $"{matricola.idEmploy}_3_Tredicesima.pdf"), true);
                        }
                        if (matricola.DataFine.ToString("yyyyMM") == $"{anno}{mese}")
                        {
                            string nameTFR = Path.Combine(pathTFR, $"{matricola.matricola}.pdf");
                            if (File.Exists(nameTFR))
                            {
                                if (!Directory.Exists(pathDestTFR)) Directory.CreateDirectory(pathDestTFR);
                                File.Copy(nameTFR, Path.Combine(pathDestTFR, $"{matricola.idEmploy}_4_TFR.pdf"), true);
                            }
                            else
                            {
                                if (DateAndTime.DateDiff(DateInterval.Day, matricola.DataInizio, matricola.DataFine) >= 15)
                                {
                                    result += $"{matricola.idEmploy} {anno}_{mese}: manca il TFR({DateAndTime.DateDiff(DateInterval.Day, matricola.DataInizio, matricola.DataFine)}gg)\r\n";
                                }
                            }
                        }
                        if (IncludeCU)
                        {
                            string nameCU = Path.Combine(pathCU, $"{matricola.CF}_CU_{int.Parse(anno) + 1}.pdf");
                            string nameCU2 = Path.Combine(pathDestCU, $"{matricola.CF}_CU_{int.Parse(anno) + 1}.pdf");
                            if (File.Exists(nameCU) && !File.Exists(nameCU2))
                            {
                                if (!Directory.Exists(pathDestCU)) Directory.CreateDirectory(pathDestCU);
                                File.Copy(nameCU, nameCU2, true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { return ex.Message; }

            return result;
        }
        static string GetDirectoryAsBase64(string directoryPath)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Crea uno zip in memoria
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var filePath in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
                    {
                        // Percorso relativo per il file all'interno dello zip
                        string relativePath = Path.GetRelativePath(directoryPath, filePath);

                        // Aggiungi il file allo zip
                        var entry = archive.CreateEntry(relativePath);
                        using (var entryStream = entry.Open())
                        using (var fileStream = File.OpenRead(filePath))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }

                // Converti lo zip in Base64
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        private class Matricola
        {
            public int id { get; set; }
            public required string idEmploy { get; set; }
            public required string matricola { get; set; }
            public DateTime DataInizio { get; set; }
            public DateTime DataFine { get; set; }
            public required string CF { get; set; }
        }

        private class DocumentiResponse
        {
            public string? zip { get; set; }
            public string? message { get; set; }
            public string? path { get; set; }
        }

        private class EbitempResponse {
            public required string pathDest { get; set; }
            public required List<string> files { get; set; }
            public required List<Matricole4Ebitemp> errori { get; set; }
        }
    }
}
