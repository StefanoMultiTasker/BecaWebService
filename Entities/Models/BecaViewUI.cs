namespace Entities.Models
{
    [AttributeUsage(AttributeTargets.Property,
         Inherited = false,
         AllowMultiple = false)]
    internal sealed class OptionalAttribute : Attribute { }

    public class FieldConfig
    {
        public string name { get; set; }
        [Optional] public string label { get; set; }
        [Optional] public string placeholder { get; set; }
        public string fieldType { get; set; }
        [Optional] public string inputType { get; set; }
        [Optional] public string? format { get; set; }
        [Optional] public string[] options { get; set; }
        [Optional] public string? optionDisplayed { get; set; }
        [Optional] public string DropDownList { get; set; }
        [Optional] public string? DropDownKeyFields { get; set; }
        public int DropDownItems { get; set; }
        public bool DropDownListAll { get; set; }
        public bool DropDownListNull { get; set; }
        [Optional] public string? reference { get; set; }
        public bool filterAPI { get; set; }
        public int row { get; set; }
        public int col { get; set; }
        public int subRow { get; set; }
        public int subCol { get; set; }
        public string? ColClass { get; set; }
        public string? ColSize { get; set; }
        public string? SubColSize { get; set; }
        public bool? disabled { get; set; }
        //public bool? Readonly { get; set; }
        public bool? required { get; set; }
        //[Optional] public string help { get; set; }
        //[Optional] public string[] options { get; set; }
        //[Optional] public string icon { get; set; }
        //[Optional] public string icon2 { get; set; }
        //[Optional] public string inputType { get; set; }
        //[Optional] public string validation { get; set; }
        //[Optional] public string extensions { get; set; }
        //[Optional] public string max { get; set; }
        //[Optional] public string min { get; set; }
        //[Optional] public string value { get; set; }
    }

    public class UIcol
    {
        public int num { get; set; }
        public string size { get; set; }
        [Optional] public FieldConfig content { get; set; }
        [Optional] public UIrows rows { get; set; }
    }

    public class UIrow
    {
        public UIrow()
        {
            this.cols = new List<UIcol>();
        }
        public UIcol GetCol(int num)
        {
            foreach (UIcol col in this.cols)
            {
                if (col.num == num) return col;
            }
            return null;
        }
        public UIcol GetCol(int num, bool add)
        {
            UIcol col = this.GetCol(num);
            if (col == null)
            {
                col = new UIcol();
                col.num = num;
                this.cols.Add(col);
            }
            return col;
        }
        public void AddCol(UIcol col)
        {
            while (col.num > this.cols.Count + 1)
            {
                UIcol _col = new UIcol();
                _col.num = this.cols.Count + 1;
                this.cols.Add(_col);
            }
            this.cols.Add(col);
        }
        public int num { get; set; }
        public List<UIcol> cols { get; set; }
    }

    public class UIrows : List<UIrow>
    {
        public UIrow GetRow(int num)
        {
            foreach (UIrow row in this)
            {
                if (row.num == num) return row;
            }
            return null;
        }
        public UIrow GetRow(int num, bool add)
        {
            UIrow row = this.GetRow(num);
            if (row == null)
            {
                row = new UIrow();
                row.num = num;
                this.Add(row);
            }
            return row;
        }
    }
    public class UIform
    {
        public UIform(string name)
        {
            this.name = name;
            this.rows = new UIrows();
            this.fields = new List<FieldConfig>();
        }
        [Optional] public List<FieldConfig> fields { get; set; }
        public string name { get; set; }
        public UIrows rows { get; set; }
    }

    public class gridColDef
    {
        public short pos { get; set; }
        public string field { get; set; }
        public string type { get; set; }
        public string headerName { get; set; }
        public bool sortable { get; set; }
        public object filter { get; set; }
        [Optional] public object filterParams { get; set; }
        public bool? hide { get; set; }
        [Optional] public string pinned { get; set; }
        [Optional] public string sort { get; set; }
        public bool? resizable { get; set; }
        public bool? editable { get; set; }
    }

    public class UIgrid
    {
        public UIgrid()
        {
            this.config = new List<gridColDef>();
        }
        public int addCol(gridColDef col)
        {
            bool added = false;
            for (short i = 0; i < this.config.Count; i++)
            {
                if (this.config[i].pos > col.pos)
                {
                    this.config.Insert(i, col);
                    added = true;
                    return i;
                }
            }
            if (!added) this.config.Add(col);
            return this.config.Count - 1;
        }
        public List<gridColDef> config { get; set; }
        public int? rowHeight { get; set; }
    }

    public class UIpage
    {
        public string name { get; set; }
        public UIgrid grid { get; set; }
        public UIform detail { get; set; }
    }
}
