using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public partial class UserMenu
    {
        public int idUtente { get; set; }
        public string UserName { get; set; }
        public int idCompany { get; set; }
        public string CompanyName { get; set; }
        public int idArea { get; set; }
        public string Area { get; set; }
        public string? AreaIconType { get; set; }
        public string? AreaIcon { get; set; }
        public int idPanel { get; set; }
        public string Panel { get; set; }
        public string? PanelIconType { get; set; }
        public string? PanelIcon { get; set; }
        public int idItem { get; set; }
        public string Caption { get; set; }
        public string DescMenuItem { get; set; }
        public string? IconType { get; set; }
        public string? Icon { get; set; }
        public int idGroup { get; set; }
        public int Position { get; set; }
        public string? Form { get; set; }
        public string? DetailsForm { get; set; }
        public string? CustomForm { get; set; }
        public bool GridWait4Param { get; set; }
        public string? Parameters { get; set; }
        public bool flAdd { get; set; }
        public bool flEdit { get; set; }
        public bool flDel { get; set; }
        public bool flDetail { get; set; }
        public bool flList { get; set; }
        public bool flExcel { get; set; }
    }
}
