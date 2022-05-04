using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BecaWebService.Models.Users
{
    public class UserMenuResponse
    {
        public UserMenuResponse() => Companies = new List<UserMenuCompany>();
        public List<UserMenuCompany> Companies { get; set; }
}

    public class UserMenuCompany
    {
        public UserMenuCompany() =>  Areas = new List<UserMenuArea>();
        public int idCompany { get; set; }
        public List<UserMenuArea> Areas { get; set; }

    }

    public class UserMenuArea
    {
        public UserMenuArea() => Panels = new List<UserMenuPanel>();
        public int idArea { get; set; }
        public string Area { get; set; }
        public string? IconType { get; set; }
        public string? Icon { get; set; }
        public List<UserMenuPanel> Panels { get; set; }
    }

    public class UserMenuPanel
    {
        public UserMenuPanel() => Menu = new List<UserMenuItem>();
        public int idPanel { get; set; }
        public string Panel { get; set; }
        public string? IconType { get; set; }
        public string? Icon { get; set; }
        public List<UserMenuItem> Menu { get; set; }
    }

    public class UserMenuItem
    {
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
