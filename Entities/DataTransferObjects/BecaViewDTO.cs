using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.DataTransferObjects
{
    public class dtoBecaView
    {
        public dtoBecaView()
        {
            ViewDefinition = new dtoBecaViewDefinition();
            Filters = new List<dtoBecaFilter>();
            FilterValues = new List<dtoBecaFilterValue>();
        }
        public string Name { get; set; }
        public short Type { get; set; }
        public string Caption { get; set; }
        public bool ChartHasDetail { get; set; }
        public string HttpGetUrl { get; set; }
        public dtoBecaViewDefinition ViewDefinition { get; set; }
        public IList<dtoBecaFilter> Filters { get; set; }
        public IList<dtoBecaFilterValue> FilterValues { get; set; }
        public UIform FilterUI { get; set; }
    }

    public class dtoBecaViewDefinition
    {
        public dtoBecaViewDefinition()
        {
            viewFields = new List<dtoBecaData>();
            viewPanels = new List<dtoBecaPanel>();
            viewAxisXData = new List<string>();
            viewAxisXFilters = new List<string>();
        }
        public IList<dtoBecaData> viewFields { get; set; }
        public IList<dtoBecaPanel> viewPanels { get; set; }
        public bool ChartHasDetail { get; set; }
        public short viewAxisXformula { get; set; }
        public IList<string> viewAxisXData { get; set; }
        public IList<string> viewAxisXFilters { get; set; }
        public string HttpGetUrl { get; set; }
    }

    public class dtoBecaData
    {
        public string Name { get; set; }
        public short DataType { get; set; }
        public string Title { get; set; }
        public string Format { get; set; }
        public bool isGridOptional { get; set; }
        public bool isGridVisible { get; set; }
        public short GridOrder { get; set; }
    }

    public partial class BecaViewFilterUI
    {
        public string Name { get; set; }
        public short DataType { get; set; }
        public string? Title { get; set; }
        public string? Format { get; set; }
        public short Filter_Row { get; set; }
        public short Filter_Col { get; set; }
        public string Filter_Size { get; set; }
        public string? Filter_options { get; set; }
        public string? Filter_Title { get; set; }
        public string Filter_Name { get; set; }
        public string FieldType { get; set; }
        public string? FieldInput { get; set; }
        public string? DropDownList { get; set; }
        public bool DropDownListAll { get; set; }
        public bool DropDownListNull { get; set; }
        public string? DropDownDisplayField { get; set; }
        public string? DropDownKeyFields { get; set; }
        public string? Parameters { get; set; }
        public bool ParametersReq { get; set; }
    }
    public class dtoBecaPanel
    {
        public dtoBecaPanel()
        {
            Filters = new List<dtoBecaFilter>();
            formula = new dtoBecaFormula();
        }
        public string Name { get; set; }
        public short Row { get; set; }
        public short Position { get; set; }
        public string Size { get; set; }
        public bool isFilterRequired { get; set; }
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
        public short? AggregationType { get; set; }
        public IList<dtoBecaFilter> Filters { get; set; }
        public dtoBecaFormula formula { get; set; }
        public string? HelpTitle { get; set; }
        public string? HelpText { get; set; }
    }

    public class dtoBecaFilter
    {
        public string FieldName { get; set; }
        public short? FieldsUse { get; set; }
        public short Type { get; set; }
        public string Format { get; set; }
        public string Field1 { get; set; }
        public string Field2 { get; set; }
        public string Parameter1 { get; set; }
        public string Parameter2 { get; set; }
        public string ValueModifier1 { get; set; }
        public string ValueModifier2 { get; set; }
    }

    public class dtoBecaFilterValue
    {
        public string filterName { get; set; }
        public bool Api { get; set; }
        public string? value { get; set; }
        public string Default { get; set; }
        public string FromFilterName { get; set; }
        public short? FromFilterIndex { get; set; }
        public bool FromFilterProp { get; set; }
    }

    public class dtoBecaFormula
    {
        public dtoBecaFormula()
        {
            data = new List<dtoBecaFormulaData>();
        }

        public string Formula { get; set; }
        public IList<dtoBecaFormulaData> data { get; set; }
    }

    public class dtoBecaFormulaData
    {
        public dtoBecaFormulaData()
        {
            Filters = new List<dtoBecaFilter>();
        }
        public string Name { get; set; }
        public string? FromPanelName { get; set; }
        public short? AggregationType { get; set; }
        public string? MainField { get; set; }
        public IList<dtoBecaFilter> Filters { get; set; }
    }
}
