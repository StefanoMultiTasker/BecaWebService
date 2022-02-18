using Entities.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class ViewChart
    {
        public ViewChart()
        {
            axisX = new ViewAxisX();
            values = new List<ViewChartValue>();
        }
        public ViewAxisX axisX { get; set; }
        public IList<ViewChartValue> values { get; set; }
    }

    public class ViewAxisX
    {

        public ViewAxisX()
        {
            value = new List<ViewAxisXvalue>();
            caption = new List<string>();
        }
        public IList<ViewAxisXvalue> value { get; set; }
        public IList<string> caption { get; set; }
    }

    public class ViewAxisXvalue
    {
        public ViewAxisXvalue()
        {
            filterValues = new List<dtoBecaFilterValue>();
            filerFormula = new List<string>();
        }
        public object value { get; set; }
        public IList<dtoBecaFilterValue> filterValues { get; set; }
        public IList<string> filerFormula { get; set; }
    }

    public class ViewChartValue
    {
        public ViewChartValue()
        {
            data = new List<object>();
        }
        public IList<object> data { get; set; }
        public string label { get; set; }
    }
}
