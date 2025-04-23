using Entities.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Communications
{
    public enum DataFormSaveActions
    {
        Add,
        Update,
        AddOrUpdate
    }
    public class DataFormPostParameters
    {
        public required List<DataFormPostParameter> RequestList { get; set; }
    }
    public class DataFormPostParameter
    {
        public string? Form { get; set; }
        public int? idView { get; set; }
        public string? FormField { get; set; }
        public string? DbName { get; set; }
        public string? ProcedureName { get; set; }
        public BecaParameters? Parameters { get; set; }
        public bool? force { get; set; }
        public JObject? newData { get; set; }
        public JObject? originalData { get; set; }
        public JArray? newListData { get; set; }
        public JArray? originalListData { get; set; }
        public bool lowerCase { get; set; }
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }
    }

    public class DataFormFieldsPostParameter
    {
        public required List<DataFormPostParameter> RequestList { get; set; }
    }

    public class DataFormChildElem
    {
        public int? idView { get; set; }
        public string? Form { get; set; }
        public string? FormChild { get; set; }
        public short sqlNumber { get; set; }
        public JObject? parentData { get; set; }
        public JObject? child1 { get; set; }
        public JObject? child2 { get; set; }
        public JObject? child3 { get; set; }
        public bool lowerCase { get; set; }
    }
}
