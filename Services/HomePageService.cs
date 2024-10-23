using BecaWebService.ExtensionsLib;
using BecaWebService.Models.Users;
using Contracts;
using Entities.Models;
using ExtensionsLib;
using Microsoft.EntityFrameworkCore;

namespace BecaWebService.Services
{
    public interface IHomePageService
    {
        public BecaHomePageResponse GetHomePageByUser();
    }

    public class HomePageService: IHomePageService
    {
        private readonly IHomePageRepository _repo;
        private readonly IGenericRepository _genericRepository;

        public HomePageService(IHomePageRepository repo, IGenericRepository genericRepository) { _repo = repo; _genericRepository = genericRepository; }
        public BecaHomePageResponse GetHomePageByUser()
        {
            BecaHomePageResponse homePage = new BecaHomePageResponse();
            IList<BecaHomePage> rawBecaHomePage = _repo.GetHomePageByUser();
            if (rawBecaHomePage == null) return homePage;

            var rows = rawBecaHomePage
                .GroupBy(m => new { m.rowPosition })
                .OrderBy(g => g.Key.rowPosition)
                .ToList();

            homePage.homeModelRow = new List<homeModelRow>();
            foreach (var rowGroup in rows)
            {
                homeModelRow row = new homeModelRow
                {
                    position = rowGroup.Key.rowPosition, // La posizione è la rowPosition
                    styleClass = rowGroup.First().rowClass, // Stile della riga, presumo sia uguale per tutte le colonne nella stessa riga
                    columns = new List<homeModelColumn>()
                };

                // Ordino le colonne per colPosition e le aggiungo alla riga
                foreach (var item in rowGroup.OrderBy(b => b.colPosition))
                {
                    var column = new homeModelColumn
                    {
                        position = item.colPosition,
                        colDimension = item.colDimension,
                        styleClass = item.colClass,
                        title = item.colTitle,
                        contentType = item.colContentType,
                        content = item.colContent,
                        options=item.options,
                        icon=item.colIcon,
                        iconColor=item.colIconColor,
                        color =item.colColor,
                        fontColor = item.colFontColor,
                        redirect=item.colRedirect,
                    };

                    if(item.sourceSQL != null)
                    {
                        List<object> testi = _genericRepository.GetDataBySQL(item.ConnectionName, item.sourceSQL, new List<BecaParameter>() );
                        if (testi.Count > 0) {
                            switch (item.sourceType)
                            {
                                case "JSON":
                                    column.content = String.Join("", testi.Select(o => o.GetPropertyStringByPos(0))); break;
                                default:
                                    column.content = testi[0].GetPropertyString("Testo"); break;
                            }
                        } else
                        {
                            column.content = item.colContentDefault ?? "";
                        }
                    }

                    row.columns.Add(column);
                }

                // Aggiungo la riga al risultato finale
                homePage.homeModelRow.Add(row);
            }

            return homePage;
        }
    }
}
