using BecaWebService.Helpers;
using BecaWebService.Models.Communications;
using Contracts;
using Contracts.Custom;
using Entities.Models;
using Entities.Models.Custom;
using ExtensionsLib;
using iText.Forms.Fields;
using iText.Forms;
using iText.IO.Source;
using iText.Kernel.Pdf;
using System.IO;
using iText.Forms.Fields;
using iText.Forms;
using iText.IO.Source;
using iText.Kernel.Pdf;

namespace BecaWebService.Services.Custom
{
    public class PrintService : IPrintService
    {
        private readonly IGenericRepository _gRepository;
        private readonly ILoggerManager _logger;
        private IWebHostEnvironment _env;

        private string fileModuleName;
        private string sTipoModulo;
        private List<object> dtSource;
        private bool bAutoName;
        private string autoName;
        private string autoPath;
        private string autoZip;
        private decimal MB4Zip;
        public PrintService(IGenericRepository genRepository, IWebHostEnvironment env, ILoggerManager logger)
        {
            _gRepository = genRepository;
            _logger = logger;
            _env = env;
        }

        public GenericResponse PrintModule(string modulo, List<BecaParameter> parameters)
        {
            try
            {
                string moduloResponse = getModulo(modulo, parameters);
                if (!string.IsNullOrEmpty(moduloResponse)) return moduloResponse.toResponse();

                switch (sTipoModulo)
                {
                    case "PDF":
                        return "Tipo di stampa (PDF) non ancora implementata".toResponse();
                        break;
                    case "PDF_Form":
                    case "PDF_Forms":
                        return new GenericResponse(new { pdf = printPDFForm() });
                        break;
                    default:
                        return $"Tipo di stampa ({sTipoModulo}) non ancora implementata".toResponse();
                        break;
                }
                return new GenericResponse(true);
            }
            catch (Exception ex) { return ex.Message.toResponse(); }
        }

        private string getModulo(string nomeModulo, List<BecaParameter> parameters)
        {
            try
            {
                BecaParameters _parameters = new BecaParameters();
                _parameters.Add("CodStampa", nomeModulo);
                List<object> moduli = _gRepository.GetDataBySQL("DbDati", "SELECT * From Anag_Stampe", _parameters.parameters);
                if (moduli == null || moduli.Count() == 0) { return $"Non ho trovato la configurazione per il modulo di stampa {nomeModulo}"; }

                object modulo = moduli[0];
                fileModuleName = modulo.GetPropertyString("Modulo");
                sTipoModulo = modulo.GetPropertyString("TipoModulo");
                return getData(modulo.GetPropertyString("Source"), modulo.GetPropertyString("SourceType"), parameters);
            }
            catch (Exception ex) { return ex.Message; }
        }

        private string getData(string sql, string tipoSql, List<BecaParameter> parameters)
        {
            string res = "";
            try
            {
                switch (tipoSql)
                {
                    case "T":
                    case "V":
                        dtSource = _gRepository.GetDataBySQL("DbDati", $"SELECT * From {sql}", parameters);
                        break;
                    case "P":
                        dtSource = _gRepository.GetDataBySP<object>("DbDati", sql, parameters);
                        break;
                    default:
                        res = "Tipo sorgente dati non gestito";
                        break;
                }
                return res;
            }
            catch (Exception ex) { return ex.Message; }
        }

        private MemoryStream printPDFForm()
        {
            string baseFolder = _gRepository.GetActiveCompany().MainFolder != "localhost" ?
                Path.Combine(_env.ContentRootPath) :
                Path.Combine("E:", "BecaWeb");
            string folderName = Path.Combine(baseFolder, "Web", "Moduli", _gRepository!.GetActiveCompany()!.MainFolder!);
            string sourceName = Path.Combine(folderName, fileModuleName); MemoryStream stream = new MemoryStream();

            PdfDocument destPdfDocumentSmartMode = new PdfDocument(new PdfWriter(stream).SetSmartMode(true));

            int count = 0;
            foreach (object o in dtSource)
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

            //Log.Information($"PrintPresenze return pdf");
            return stream;
        }
    }
}
