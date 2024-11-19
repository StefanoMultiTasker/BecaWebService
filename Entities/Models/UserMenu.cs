namespace Entities.Models
{
    public partial class BasicMenu
    {
        public int idItem { get; set; }
        public string Caption { get; set; }
        public string DescMenuItem { get; set; }
        public string? IconType { get; set; }
        public string? Icon { get; set; }
        public int idGroup { get; set; }
        public int Position { get; set; }
        public bool isContainer { get; set; }
        public int? ParentItem { get; set; }
        public string? Form { get; set; }
        public string? DetailsForm { get; set; }
        public string? CustomForm { get; set; }
        public bool GridWait4Param { get; set; }
        public string? Parameters { get; set; }
    }

    public partial class UserMenu
    {
        public UserMenu() { }
        public UserMenu(BasicMenu basic)
        {
            foreach (var prop in basic.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(basic));
            }
        }

        public int? idUtente { get; set; }
        public string? UserName { get; set; }
        public int idCompany { get; set; }
        public string CompanyName { get; set; }
        //public int idArea { get; set; }
        //public string Area { get; set; }
        //public string? AreaIconType { get; set; }
        //public string? AreaIcon { get; set; }
        //public int idPanel { get; set; }
        //public string Panel { get; set; }
        //public string? PanelIconType { get; set; }
        //public string? PanelIcon { get; set; }
        public int idItem { get; set; }
        public string Caption { get; set; }
        public string DescMenuItem { get; set; }
        public string? IconType { get; set; }
        public string? Icon { get; set; }
        public int idGroup { get; set; }
        public int Position { get; set; }
        public bool isContainer { get; set; }
        public int? ParentItem { get; set; }
        public string? Form { get; set; }
        public string? DetailsForm { get; set; }
        public string? CustomForm { get; set; }
        public bool GridWait4Param { get; set; }
        public string? Parameters { get; set; }
        public bool flAdd { get; set; } = false;
        public bool flEdit { get; set; } = false;
        public bool flDel { get; set; } = false;
        public bool flDetail { get; set; } = false;
        public bool flList { get; set; } = false;
        public bool flExcel { get; set; } = false;
    }

    public partial class ProfileMenu
    {
        public ProfileMenu() { }
        public ProfileMenu(BasicMenu basic)
        {
            foreach (var prop in basic.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                this.GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(basic));
            }
        }

        public int idProfile { get; set; }
        public string Profile { get; set; }
        public int idCompany { get; set; }
        public string CompanyName { get; set; }
        public int idItem { get; set; }
        public string Caption { get; set; }
        public string DescMenuItem { get; set; }
        public string? IconType { get; set; }
        public string? Icon { get; set; }
        public int idGroup { get; set; }
        public int Position { get; set; }
        public bool isContainer { get; set; }
        public int? ParentItem { get; set; }
        public string? Form { get; set; }
        public string? DetailsForm { get; set; }
        public string? CustomForm { get; set; }
        public bool GridWait4Param { get; set; }
        public string? Parameters { get; set; }
        public bool flAdd { get; set; } = false;
        public bool flEdit { get; set; } = false;
        public bool flDel { get; set; } = false;
        public bool flDetail { get; set; } = false;
        public bool flList { get; set; } = false;
        public bool flExcel { get; set; } = false;
    }
}
