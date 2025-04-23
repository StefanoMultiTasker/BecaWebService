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
using Path = System.IO.Path;

using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Layer;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using System.Reflection;

namespace BecaWebService.Services.Custom
{
    public class PrintService : IPrintService
    {
        private readonly IGenericRepository _gRepository;
        private readonly ILoggerManager _logger;
        private IWebHostEnvironment _env;

        private string fileModuleName = "";
        private string sTipoModulo = "";
        private List<object> dtSource = [];
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
                        return $"Tipo di stampa ({sTipoModulo}) non ancora implementata".toResponse();
                    case "PDF_Form":
                    case "PDF_Forms":
                        return new GenericResponse(new { pdf = printPDFForm() });
                    case "PDF_Tags":
                        return new GenericResponse(new { pdf = printPDFTags() });
                    default:
                        return $"Tipo di stampa ({sTipoModulo}) non ancora implementata".toResponse();
                }
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
            string baseFolder = _gRepository.GetActiveCompany()!.MainFolder != "localhost" ?
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
                sourcePdfDocument.Close();
                //Log.Information($"PrintPresenze compiled {count}° form");
            }
            destPdfDocumentSmartMode.Close();

            //Log.Information($"PrintPresenze return pdf");
            return stream;
        }

        private MemoryStream printPDFTags()
        {
            string baseFolder = _gRepository.GetActiveCompany()!.MainFolder != "localhost" ?
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
                //Fill out fields
                //PdfFormField toSet;
                foreach (PropertyInfo field in o.GetType().GetProperties())
                {
                    PdfReplace(sourcePdfDocument, count+1, field.Name, o.GetPropertyString(field.Name), null, 0);
                }
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
        public void PdfReplace(PdfDocument pdfDoc, int pageNum, string sFind, string sReplace, PdfFont? pdfFont, float fontSize)
        {
            PdfPage page = pdfDoc.GetPage(pageNum);

            // Usa la strategia personalizzata per estrarre il testo con le coordinate
            CustomLocationTextExtractionStrategy strategy = new CustomLocationTextExtractionStrategy();
            PdfTextExtractor.GetTextFromPage(page, strategy);

            List<TextLocation> lstMatches = strategy.GetTextLocations()
                .Where(t => t.Text.Equals(sFind, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            if (lstMatches.Count == 0)
            {
                Console.WriteLine("Testo non trovato nella pagina.");
                return;
            }

            PdfCanvas pdfCanvas = new PdfCanvas(page);
            PdfLayer pdLayer = new PdfLayer("Overwrite", pdfDoc);
            pdfCanvas.BeginLayer(pdLayer);

            pdfCanvas.SetFillColor(ColorConstants.WHITE);

            foreach (TextLocation match in lstMatches)
            {
                Rectangle rect = match.Bounds;

                // Copre il testo originale con un rettangolo bianco
                pdfCanvas.Rectangle(rect.GetX(), rect.GetY(), rect.GetWidth(), rect.GetHeight());
                pdfCanvas.Fill();

                // Imposta il nuovo stato grafico
                PdfExtGState pgState = new PdfExtGState();
                pdfCanvas.SetExtGState(pgState);
                pdfCanvas.SetFillColor(ColorConstants.BLACK);

                // Scrive il nuovo testo nella stessa posizione
                pdfCanvas.BeginText();
                if (pdfFont != null) pdfCanvas.SetFontAndSize(pdfFont, fontSize);
                pdfCanvas.MoveText(rect.GetX(), rect.GetY());
                pdfCanvas.ShowText(sReplace);
                pdfCanvas.EndText();
            }

            pdfCanvas.EndLayer();
        }
    }
}
