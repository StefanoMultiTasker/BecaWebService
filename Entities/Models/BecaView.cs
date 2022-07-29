using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.Models
{
    public partial class BecaView
    {
        public BecaView()
        {
            BecaViewData = new List<BecaViewData>();
            BecaViewFilterValues = new List<BecaViewFilterValues>();
            BecaViewFilters = new List<BecaViewFilters>();
            BecaViewPanels = new List<BecaViewPanels>();
            BecaViewChildren = new List<BecaViewChild>();
        }

        public int idBecaView { get; set; }
        public string Name { get; set; }
        public short idBecaViewType { get; set; }
        public string Caption { get; set; }
        public bool HasGrid { get; set; }
        public bool HasChart { get; set; }
        public bool ChartHasDetail { get; set; }
        public bool IsChartFromApi { get; set; }
        public bool isPanelsFromApi { get; set; }
        public short? viewAxisXformula { get; set; }
        public string? viewAxisXData { get; set; }
        public string? viewAxisXFilters { get; set; }
        public string? viewAxisXActions { get; set; }
        public string? viewAxisXZoomIf { get; set; }
        public string? viewAxisXZoomTo { get; set; }
        public short? viewAxisXStep { get; set; }
        public string? HttpGetUrl { get; set; }

        public virtual BecaViewTypes idBecaViewTypeNavigation { get; set; }
        public virtual ICollection<BecaViewData> BecaViewData { get; set; }
        public virtual ICollection<BecaViewFilterValues> BecaViewFilterValues { get; set; }
        public virtual ICollection<BecaViewFilters> BecaViewFilters { get; set; }
        public virtual ICollection<BecaViewPanels> BecaViewPanels { get; set; }
        [NotMapped]
        public ICollection<BecaViewChild> BecaViewChildren { get; set; }
    }

    public partial class BecaViewData 
    {
        public int idBecaView { get; set; }
        //public int idDataDefinition { get; set; }
        public string Field { get; set; }
        public short idDataType { get; set; }
        public short? FormulaFooter { get; set; }
        public string? Title { get; set; }
        public string? Format { get; set; }
        public bool isGridOptional { get; set; }
        public bool isGridVisible { get; set; }
        public short GridOrder { get; set; }
        public string? GridHeatColor { get; set; }

        public virtual BecaView idBecaViewNavigation { get; set; }
    }

    public partial class BecaViewDataUser
    {
        public int idBecaView { get; set; }
        public string field { get; set; }
        public int idCompany { get; set; }
        public int idUtente { get; set; }
        public bool isGridVisible { get; set; }
        public short GridOrder { get; set; }
    }

    public partial class BecaViewUI
    {
        public int idBecaView { get; set; }
        public string ViewName { get; set; }
        public string Name { get; set; }
        public short idDataType { get; set; }
        public string DataType { get; set; }
        public string? Title { get; set; }
        public string? Format { get; set; }
        public short Row { get; set; }
        public short SubRow { get; set; }
        public short Col { get; set; }
        public short Col_Order { get; set; }
        public short SubCol { get; set; }
        public short SubCol_Order { get; set; }
        public string? ColSize { get; set; }
        public string? SubColSize { get; set; }
        public string? Filter_options { get; set; }
        //public string? Filter_Title { get; set; }
        public string? Filter_Name { get; set; }
        public bool Filter_API { get; set; }
        //public short Filter_ConcatSequence { get; set; }
        public string? Filter_Reference { get; set; }
        public string FieldType { get; set; }
        public string? FieldInput { get; set; }
        //public string? DropDownList { get; set; }
        public bool DropDownListAll { get; set; }
        public bool DropDownListNull { get; set; }
        public string? DropDownDisplayField { get; set; }
        public string? DropDownKeyFields { get; set; }
        public string? Parameters { get; set; }
        public bool ParametersReq { get; set; }
        public string? HelpShort { get; set; }
        public string? HelpFull { get; set; }
    }

    public  class BecaViewFilterUI : BecaViewUI { }
    public partial class BecaViewDetailUI : BecaViewUI { }


    //public partial class BecaViewDetailUI
    //{
    //    public int idBecaView { get; set; }
    //    public string ViewName { get; set; }
    //    public string Name { get; set; }
    //    public short idDataType { get; set; }
    //    public string DataType { get; set; }
    //    public string? Title { get; set; }
    //    public string? Format { get; set; }
    //    public short Detail_Row { get; set; }
    //    public short Detail_SubRow { get; set; }
    //    public short Detail_Col { get; set; }
    //    public short Detail_SubCol { get; set; }
    //    public string Detail_Size { get; set; }
    //    public string? Detail_options { get; set; }
    //    public string? Detail_Title { get; set; }
    //    public string Detail_Name { get; set; }
    //    public string FieldType { get; set; }
    //    public string? FieldInput { get; set; }
    //    public string? DropDownList { get; set; }
    //    public bool DropDownListAll { get; set; }
    //    public bool DropDownListNull { get; set; }
    //    public string? DropDownDisplayField { get; set; }
    //    public string? DropDownKeyFields { get; set; }
    //    public string? Parameters { get; set; }
    //    public bool ParametersReq { get; set; }
    //    public string? HelpShort { get; set; }
    //    public string? HelpFull { get; set; }
    //}

    public partial class BecaViewFilterValues 
    {
        public int idBecaView { get; set; }
        public int idFilterValue { get; set; }
        public string Name { get; set; }
        public bool Api { get; set; }
        public string? DefaultValue { get; set; }
        public string? DefaultFunc { get; set; }
        public short DefaultUse { get; set; }
        public string FromFilterName { get; set; }
        public short? FromFilterIndex { get; set; }
        public bool FromFilterProp { get; set; }
        public bool subFilter { get; set; }

        public virtual BecaView idBecaViewNavigation { get; set; }
    }

    public partial class BecaViewFilters 
    {
        public int idBecaView { get; set; }
        public string? FilterReference { get; set; }
        //public string Field1 { get; set; }
        //public string Field2 { get; set; }
        public string FieldName { get; set; }
        public string FilterName { get; set; }
        public string? Format { get; set; }
        public int idBecaFilter { get; set; }
        public short? idFieldsUse { get; set; }
        public string? FieldsUse { get; set; }
        public short idFilterType { get; set; }
        public string FilterType { get; set; }
        public string? Parameter1 { get; set; }
        public string? Parameter2 { get; set; }
        public string? ValueModifier1 { get; set; }
        public string? ValueModifier2 { get; set; }
  
        public virtual BecaView idBecaViewNavigation { get; set; }
    }

    public partial class BecaViewPanels
    {
        public BecaViewPanels()
        {
            BecaPanelFilters = new List<BecaPanelFilters>();
        }

        public int idBecaViewPanel { get; set; }
        public int idBecaView { get; set; }
        public string Name { get; set; }
        public short Row { get; set; }
        public short Position { get; set; }
        public string Size { get; set; }
        public bool IsFilterRequired { get; set; }
        public string Caption { get; set; }
        public string? Prefix { get; set; }
        public string? Postfix { get; set; }
        public string? Format { get; set; }
        public string? Icon { get; set; }
        public string? Class { get; set; }
        public bool HasDetail { get; set; }
        public bool? IsChart { get; set; }
        public string? ChartColor { get; set; }
        public string? MainField { get; set; }
        public short? IdAggregationType { get; set; }
        public int? IdFormula { get; set; }
        public string? HelpTitle { get; set; }
        public string? HelpText { get; set; }
        public string? Color { get; set; }

        public virtual BecaAggregationTypes IdAggregationTypeNavigation { get; set; }
        public virtual BecaView idBecaViewNavigation { get; set; }
        public virtual BecaFormula IdFormulaNavigation { get; set; }
        public virtual IList<BecaPanelFilters> BecaPanelFilters { get; set; }
    }

    public partial class BecaPanelFilters  
    {
        public int idBecaViewPanel { get; set; }
        public int idBecaFilter { get; set; }
        public string FilterName { get; set; }
        public string FieldName { get; set; }
        public short? idFieldsUse { get; set; }
        public string FieldsUse { get; set; }
        public short idFilterType { get; set; }
        public string FilterType { get; set; }
        public string Format { get; set; }
        public string? FilterReference { get; set; }
        //public string Field1 { get; set; }
        //public string Field2 { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }
        public string ValueModifier1 { get; set; }
        public string ValueModifier2 { get; set; }

        public virtual BecaViewPanels idBecaViewPanelNavigation { get; set; }
    }

    public partial class BecaFormula
    {
        public BecaFormula()
        {
            BecaFormulaData = new List<BecaFormulaData>();
            BecaViewPanels = new List<BecaViewPanels>();
        }

        public int IdFormula { get; set; }
        public string Name { get; set; }
        public string Formula { get; set; }

        public virtual IList<BecaFormulaData> BecaFormulaData { get; set; }
        public virtual IList<BecaViewPanels> BecaViewPanels { get; set; }
    }

    public partial class BecaFormulaData
    {
        public BecaFormulaData()
        {
            BecaFormulaDataFilters = new List<BecaFormulaDataFilters>();
        }

        public int IdFormulaData { get; set; }
        public int IdFormula { get; set; }
        public string FormulaDataName { get; set; }
        public string FromPanelName { get; set; }
        public short? IdAggregationType { get; set; }
        public string MainField { get; set; }

        public virtual BecaAggregationTypes IdAggregationTypeNavigation { get; set; }
        public virtual BecaFormula IdFormulaNavigation { get; set; }
        public virtual IList<BecaFormulaDataFilters> BecaFormulaDataFilters { get; set; }
    }

    public partial class BecaFormulaDataFilters 
    {
        public int IdFormulaData { get; set; }
        public int idBecaFilter { get; set; }
        public string FilterName { get; set; }
        public string FieldName { get; set; }
        public short? idFieldsUse { get; set; }
        public string FieldsUse { get; set; }
        public short idFilterType { get; set; }
        public string FilterType { get; set; }
        public string Format { get; set; }
        public string? FilterReference { get; set; }
        //public string Field1 { get; set; }
        //public string Field2 { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }
        public string ValueModifier1 { get; set; }
        public string ValueModifier2 { get; set; }

        public virtual BecaFormulaData IdFormulaDataNavigation { get; set; }
    }

    public partial class BecaViewTypes
    {
        public BecaViewTypes()
        {
            BecaView = new HashSet<BecaView>();
        }

        public short idBecaViewType { get; set; }
        public string BecaViewType { get; set; }

        public virtual ICollection<BecaView> BecaView { get; set; }
    }

    public partial class BecaAggregationTypes
    {
        public BecaAggregationTypes()
        {
            BecaFormulaData = new HashSet<BecaFormulaData>();
            BecaViewPanels = new HashSet<BecaViewPanels>();
        }

        public short IdAggregationType { get; set; }
        public string AggregationType { get; set; }

        public virtual ICollection<BecaFormulaData> BecaFormulaData { get; set; }
        public virtual ICollection<BecaViewPanels> BecaViewPanels { get; set; }
    }

    public partial class BecaViewChild
    {
        public int idBecaView { get; set; }
        public string form { get; set; }
        public string childForm { get; set; }
        public int subLevel { get; set; }
        public string childCaption { get; set; }
        public ICollection<BecaViewChildData> BecaFormChildData { get; set; }

        public BecaViewChild()
        {
            BecaFormChildData = new List<BecaViewChildData>();
        }
    }

    //[Owned]
    public partial class BecaViewChildData
    {
        //public virtual BecaViewChild idBecaViewChildNavigation { get; set; }
        public string form { get; set; }
        public string field { get; set; }
        public short idDataType { get; set; }
        public short? FormulaFooter { get; set; }
        public string? Title { get; set; }
        public string? Format { get; set; }
        public bool isGridOptional { get; set; }
        public bool isGridVisible { get; set; }
        public short GridOrder { get; set; }
        public string? GridHeatColor { get; set; }
        public BecaViewChild containerForm { get; set; }
    }
}
