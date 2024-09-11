using BecaWebService.Models.Users;
using Contracts;
using Entities.Models;
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
        public HomePageService(IHomePageRepository repo) { _repo = repo; }
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
                        options=item.options
                    };

                    row.columns.Add(column);
                }

                // Aggiungo la riga al risultato finale
                homePage.homeModelRow.Add(row);
            }

            return homePage;
        }
    }
}
