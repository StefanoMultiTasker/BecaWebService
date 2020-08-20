using System;
using System.Collections.Generic;
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
        }

        public int idBecaView { get; set; }
        public string Name { get; set; }
        public short idBecaViewType { get; set; }
        public string Caption { get; set; }
        public bool ChartHasDetail { get; set; }
        public short viewAxisXformula { get; set; }
        public string HttpGetUrl { get; set; }

        public virtual BecaViewTypes idBecaViewTypeNavigation { get; set; }
        public virtual ICollection<BecaViewData> BecaViewData { get; set; }
        public virtual ICollection<BecaViewFilterValues> BecaViewFilterValues { get; set; }
        public virtual ICollection<BecaViewFilters> BecaViewFilters { get; set; }
        public virtual ICollection<BecaViewPanels> BecaViewPanels { get; set; }
    }

    public partial class BecaViewData 
    {
        public int idBecaView { get; set; }
        public int idDataDefinition { get; set; }
        public string Name { get; set; }
        public short idDataType { get; set; }
        public string Title { get; set; }
        public string Format { get; set; }
        public bool IsOptional { get; set; }
        public bool IsVisible { get; set; }
        public short TableOrder { get; set; }

        public virtual BecaView idBecaViewNavigation { get; set; }
    }

    public partial class BecaViewFilterValues 
    {
        public int idBecaView { get; set; }
        public int idFilterValue { get; set; }
        public string Name { get; set; }
        public bool Api { get; set; }
        public string? DefaultValue { get; set; }
        public short DefaultUse { get; set; }
        public string FromFilterName { get; set; }
        public short? FromFilterIndex { get; set; }
        public bool FromFilterProp { get; set; }   
        
        public virtual BecaView idBecaViewNavigation { get; set; }
    }

    public partial class BecaViewFilters 
    {
        public int idBecaView { get; set; }
        public string Field1 { get; set; }
        public string Field2 { get; set; }
        public string FieldName { get; set; }
        public string FilterName { get; set; }
        public string Format { get; set; }
        public int idBecaFilter { get; set; }
        public short? idFieldsUse { get; set; }
        public string FieldsUse { get; set; }
        public short idFilterType { get; set; }
        public string FilterType { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }
        public string ValueModifier1 { get; set; }
        public string ValueModifier2 { get; set; }
  
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
        public string Prefix { get; set; }
        public string Postfix { get; set; }
        public string Format { get; set; }
        public string Icon { get; set; }
        public string Class { get; set; }
        public bool HasDetail { get; set; }
        public bool? IsChart { get; set; }
        public string ChartColor { get; set; }
        public string MainField { get; set; }
        public short? IdAggregationType { get; set; }
        public int? IdFormula { get; set; }

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
        public string Field1 { get; set; }
        public string Field2 { get; set; }
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
        public string Field1 { get; set; }
        public string Field2 { get; set; }
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
}
