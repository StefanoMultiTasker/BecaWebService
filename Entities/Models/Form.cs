using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Models
{
    public class BecaParameters
    {
        public List<BecaParameter> parameters { get; set; }

        public BecaParameters() { this.parameters = new List<BecaParameter>(); }

        public BecaParameter Add(string name, object val)
        {
            BecaParameter par = new BecaParameter(name, val);
            if (this.parameters == null) this.parameters = new List<BecaParameter>();
            this.parameters.Add(par);
            return par;
        }
        public BecaParameter Add(string name, object val, string comparison)
        {
            BecaParameter par = new BecaParameter(name, val);
            par.comparison = comparison;
            if (this.parameters == null) this.parameters = new List<BecaParameter>();
            this.parameters.Add(par);
            return par;
        }
        public void Remove(string name)
        {
            foreach (BecaParameter par in this.parameters)
            {
                if (par.name == name)
                {
                    this.parameters.Remove(par);
                    break;
                }
            }
        }
    }

    public class BecaParameter
    {
        public string name { get; set; }
        public object value1 { get; set; }
        public object value2 { get; set; }
        public string comparison { get; set; }
        public string dataType { get; set; }

        public BecaParameter() { }
        public BecaParameter(string name, object value1) { this.name = name; this.value1 = value1; this.comparison = "="; }
        public BecaParameter(string name, object value1, object value2) { this.name = name; this.value1 = value1; this.value2 = value2; this.comparison = "between"; }
    }

    public class BecaForm
    {
        public string Form { get; set; }
        public string DescForm { get; set; }
        public bool AddRecord { get; set; }
        public bool EditRecord { get; set; }
        public bool DeleteRecord { get; set; }
        public string TableNameDB { get; set; }
        public string TableName { get; set; }
        public string ViewNameDB { get; set; }
        public string ViewName { get; set; }
        public string PrimaryKey { get; set; }
        public string SelectProcedureName { get; set; }
        public string UpdateProcedureName { get; set; }
        public string AddProcedureName { get; set; }
        public string DeleteProcedureName { get; set; }
        public bool UseDefaultParam { get; set; }
        public string SelectSql { get; set; }
        public int DefaultRows { get; set; }
        public bool CheckObjPermissions { get; set; }
        public string EMailOnAdd { get; set; }
        public string EMailOnUpdate { get; set; }
        public string EMailOnDelete { get; set; }
    }

    public class BecaFormLevels
    {
        public string Form { get; set; }
        public int SubLevel { get; set; }
        public int ParentLevel { get; set; }
        public string ChildForm { get; set; }
        public string ChildCaption { get; set; }
        public string RelationName { get; set; }
        public string RelationColumn { get; set; }
        public bool LoadInGrid { get; set; }
        public bool LoadInDetails { get; set; }
        public int DefaultRows { get; set; }
        public string ComboAddSql { get; set; }
        public string ComboAddSql1 { get; set; }
        public string ComboAddSql2 { get; set; }
        public string ComboAddSp { get; set; }
        public string ComboAddSp1 { get; set; }
        public string ComboAddSp2 { get; set; }
        public string ChildAddOk { get; set; }
        public string ChildAddErr { get; set; }
        public string ChildAddSaveBefore { get; set; }
    }

    public class BecaFormField
    {
        public string Form { get; set; }
        public int PosizioneTabella { get; set; }
        public string Campo { get; set; }
        public string DescCampo { get; set; }
        public string Titolo { get; set; }
        public string TipoCampo { get; set; }
        public string? TipoInput { get; set; }
        public string? Struttura { get; set; }
        public Int16 LunghezzaMin { get; set; }
        public int LunghezzaMax { get; set; }
        public string? DropDownListDB { get; set; }
        public string? DropDownList { get; set; }
        public string? Parametri { get; set; }
        public bool ParametriObbl { get; set; }
        public string? ValoreMin { get; set; }
        public string? ValoreMax { get; set; }
        public int PosizioneGriglia { get; set; }
        public bool Visibile { get; set; }
        public bool Editabile { get; set; }
        public int MisuraLarghezzaGriglia { get; set; }
        public int MisuraAltezzaGriglia { get; set; }
        public string? Ordinamento { get; set; }
        public Int16 SequenzaOrdinamento { get; set; }
        public string? FormatoData { get; set; }
        public int PosizioneRicerca { get; set; }
        public int PosizioneRicercaProg { get; set; }
        public int MisuraLarghezzaRicerca { get; set; }
        public int MisuraAltezzaRicerca { get; set; }
        public bool DropDownListAll { get; set; }
        public bool DropDownListNull { get; set; }
        public bool PostBack { get; set; }
        public int PosizioneDetails { get; set; }
        public int PosizioneDetailsProg { get; set; }
        public bool Locked { get; set; }
        public int MisuraLarghezzaDetails { get; set; }
        public int MisuraAltezzaDetails { get; set; }
        public bool Obbligatorio { get; set; }
        public bool Modificabile { get; set; }
    }

    public class BecaFormFieldLevel
    {
        public string Form { get; set; }
        public string objName { get; set; }
        public int idLivello { get; set; }
        public bool flgVisible { get; set; }
        public bool flgEditable { get; set; }
        public string? DropDownList { get; set; }
        public string? Parametri { get; set; }
        public bool DropDownListAll { get; set; }
        public bool DropDownListNull { get; set; }
        public bool PostBack { get; set; }
    }
}
