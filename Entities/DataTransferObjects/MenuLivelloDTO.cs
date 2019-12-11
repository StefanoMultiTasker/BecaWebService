using Entities.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.DataTransferObjects
{
    public class MenuLivelloDTO
    {
        public int idLivello { get; set; }
        public List<MenuAreaDTO> Aree { get; set; }

        public MenuLivelloDTO(IEnumerable<VMenuLivello> data)
        {
            MenuAreaDTO area;
            MenuGruppoDTO gruppo;
            MenuSubGroup sub;

            foreach (VMenuLivello menu in data)
            {
                if (this.Aree == null) Aree = new List<MenuAreaDTO>();
                if (!Aree.Exists(A => A.AreaCod == menu.AreaCod)) {
                    Aree.Add(new MenuAreaDTO { 
                        AreaCod = menu.AreaCod, 
                        AreaDesc = menu.AreaDesc, 
                        AreaIcona = menu.AreaIcona, 
                        AreaOrdine = menu.AreaOrdine 
                    });
                }
                area = Aree.Find(A => A.AreaCod == menu.AreaCod);
                
                if (area.Gruppi == null) area.Gruppi = new List<MenuGruppoDTO>();
                if (!area.Gruppi.Exists(A => A.GruppoCod == menu.GruppoCod))
                {
                    area.Gruppi.Add(new MenuGruppoDTO
                    {
                        GruppoCod = menu.GruppoCod,
                        GruppoDesc = menu.GruppoDesc,
                        GruppoIcona = menu.GruppoIcona,
                        GruppoOrdine = menu.GruppoOrdine
                    });
                }
                gruppo = area.Gruppi.Find(A => A.GruppoCod == menu.GruppoCod);

                if (gruppo.SubGroup == null) gruppo.SubGroup = new List<MenuSubGroup>();
                if (!gruppo.SubGroup.Exists(A => A.SottoGruppo == menu.SottoGruppo))
                {
                    gruppo.SubGroup.Add(new MenuSubGroup
                    {
                        SottoGruppo = menu.SottoGruppo
                    });
                }
                sub = gruppo.SubGroup.Find(A => A.SottoGruppo == menu.SottoGruppo);

                if (sub.Voci == null) sub.Voci = new List<MenuVoceDTO>();
                if (!sub.Voci.Exists(A => A.MenuCod == menu.MenuCod))
                {
                    sub.Voci.Add(new MenuVoceDTO
                    {
                        MenuCod = menu.MenuCod,
                        Posizione = menu.Posizione,
                        Caption = menu.Caption,
                        DetailsForm = menu.DetailsForm,
                        FlAdd = menu.FlAdd,
                        FlEdit = menu.FlEdit,
                        FlDel = menu.FlDel,
                        FlDetail = menu.FlDetail,
                        FlExcel = menu.FlExcel,
                        Id = menu.Id
                    });
                }
            }
        }
    }

    public class MenuAreaDTO
    {
        public string AreaCod { get; set; }
        public string AreaDesc { get; set; }
        public string AreaIcona { get; set; }
        public short AreaOrdine { get; set; }
        public List<MenuGruppoDTO> Gruppi { get; set; }
    }

    public class MenuGruppoDTO
    {
        public string GruppoCod { get; set; }
        public string GruppoDesc { get; set; }
        public string GruppoIcona { get; set; }
        public short GruppoOrdine { get; set; }
        public List<MenuSubGroup> SubGroup { get; set; }
    }

    public class MenuSubGroup
    {
        public int SottoGruppo { get; set; }
        public List<MenuVoceDTO> Voci { get; set; }
    }

    public class MenuVoceDTO
    {
        public string MenuCod { get; set; }
        public int Posizione { get; set; }
        public string Caption { get; set; }
        public string DetailsForm { get; set; }
        public bool FlAdd { get; set; }
        public bool FlEdit { get; set; }
        public bool FlDel { get; set; }
        public bool FlDetail { get; set; }
        public bool FlList { get; set; }
        public bool FlExcel { get; set; }
        public int Id { get; set; }
    }
}
