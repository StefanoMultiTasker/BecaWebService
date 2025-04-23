using AutoMapper;
using BecaWebService.ExtensionsLib;
using Entities.DataTransferObjects;
using Entities.Models;
using NLog.Targets;

namespace BecaWebService.Mappings
{
    public class MappingBecaView : Profile
    {
        public MappingBecaView()
        {
            MapFilter();
            MapPanel();
            MapFormula();
            MapChild();
            MapView();

            MapUI();
        }

        private void MapFilter()
        {
            CreateMap<BecaViewFilters, dtoBecaFilter>()
                    .ForMember(dest => dest.FieldsUse,
                            opts => opts.MapFrom(
                                src => src.idFieldsUse
                                )
                            )
                    .ForMember(dest => dest.FieldName,
                            opts => opts.MapFrom(
                                src => src.FieldName//.ToLowerToCamelCase()
                                )
                            )
                    .ForMember(dest => dest.Type,
                            opts => opts.MapFrom(
                                src => src.idFilterType
                                )
                            )
                    .ForMember(dest => dest.Field1,
                            opts => opts.MapFrom((src, dest) =>
                            {
                                string[] f = (src.FilterReference ?? "").Split(",");
                                return f.Length > 0 ? f[0].ToLower() : null;
                            })
                            )
                    .ForMember(dest => dest.Field2,
                            opts => opts.MapFrom((src, dest) =>
                            {
                                string[] f = (src.FilterReference ?? "").Split(",");
                                return f.Length > 1 ? f[1].ToLower() : null;
                            })
                            );

            CreateMap<BecaViewFilterValues, dtoBecaFilterValue>()
                .ForMember(dest => dest.filterName,
                        opts => opts.MapFrom(
                            src => src.Name
                            )
                        )
                .ForMember(dest => dest.Default,
                        opts => opts.MapFrom(
                            src => src.DefaultValue
                            )
                        );

        }

        private void MapPanel()
        {
            CreateMap<BecaViewPanels, dtoBecaPanel>()
                .ForMember(dest => dest.AggregationType,
                        opts => opts.MapFrom(
                            src => src.IdAggregationType
                            )
                        )
                .ForMember(dest => dest.formula,
                        opts => opts.MapFrom(
                            src => src.IdFormulaNavigation
                            )
                        )
                .ForMember(dest => dest.Filters,
                        opts => opts.MapFrom(
                            src => src.BecaPanelFilters
                            )
                        );

            CreateMap<BecaPanelFilters, dtoBecaFilter>()
                .ForMember(dest => dest.FieldsUse,
                        opts => opts.MapFrom(
                            src => src.idFieldsUse
                            )
                        )
                .ForMember(dest => dest.FieldName,
                        opts => opts.MapFrom(
                            src => src.FieldName//.ToLowerToCamelCase()
                            )
                        )
                .ForMember(dest => dest.Type,
                        opts => opts.MapFrom(
                            src => src.idFilterType
                            )
                        )
                .ForMember(dest => dest.Field1,
                        opts => opts.MapFrom((src, dest) =>
                        {
                            string[] f = (src.FilterReference ?? "").Split(",");
                            return f.Length > 0 ? f[0] : null;
                        })
                        )
                .ForMember(dest => dest.Field2,
                        opts => opts.MapFrom((src, dest) =>
                        {
                            string[] f = (src.FilterReference ?? "").Split(",");
                            return f.Length > 1 ? f[1] : null;
                        })
                        );
        }

        private void MapFormula()
        {
            CreateMap<BecaFormula, dtoBecaFormula>()
                .ForMember(dest => dest.data,
                        opts => opts.MapFrom(
                            src => src.BecaFormulaData
                            )
                        );

            CreateMap<BecaFormulaData, dtoBecaFormulaData>()
                .ForMember(dest => dest.AggregationType,
                        opts => opts.MapFrom(
                            src => src.IdAggregationType
                            )
                        )
                .ForMember(dest => dest.Name,
                        opts => opts.MapFrom(
                            src => src.FormulaDataName
                            )
                        )
                .ForMember(dest => dest.Filters,
                        opts => opts.MapFrom(
                            src => src.BecaFormulaDataFilters
                            )
                        );

            CreateMap<BecaFormulaDataFilters, dtoBecaFilter>()
                .ForMember(dest => dest.FieldsUse,
                        opts => opts.MapFrom(
                            src => src.idFieldsUse
                            )
                        )
                    .ForMember(dest => dest.FieldName,
                            opts => opts.MapFrom(
                                src => src.FieldName.ToLower()
                                )
                            )
                .ForMember(dest => dest.Type,
                        opts => opts.MapFrom(
                            src => src.idFilterType
                            )
                        )
                .ForMember(dest => dest.Field1,
                        opts => opts.MapFrom((src, dest) =>
                        {
                            string[] f = (src.FilterReference ?? "").Split(",");
                            return f.Length > 0 ? f[0] : null;
                        })
                        )
                .ForMember(dest => dest.Field2,
                        opts => opts.MapFrom((src, dest) =>
                        {
                            string[] f = (src.FilterReference ?? "").Split(",");
                            return f.Length > 1 ? f[1] : null;
                        })
                        );
        }

        private void MapChild()
        {
            CreateMap<BecaViewChildData, dtoBecaData>()
                .ForMember(dest => dest.Name,
                        opts => opts.MapFrom(
                            src => src.field.ToLower()//.ToCamelCase()
                            )
                        )
                .ForMember(dest => dest.DataType,
                        opts => opts.MapFrom(
                            src => src.idDataType
                            )
                        )
                ;

            CreateMap<BecaViewChild, dtoBecaViewChild>()
                .ForMember(dest => dest.form,
                        opts => opts.MapFrom(
                            src => src.childForm
                            )
                        )
                .ForMember(dest => dest.Caption,
                        opts => opts.MapFrom(
                            src => src.childCaption
                            )
                        )
                .ForPath(dest => dest.KeyFields,
                        opts => opts.MapFrom(
                            src => src.PrimaryKey.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(k => k.ToLower()).ToList()
                            )
                        )
                .ForPath(dest => dest.ChildFields,
                        opts => opts.MapFrom(
                            src => src.BecaFormChildData
                            )
                        )
                .ForMember(dest => dest.ComboAddSql1,
                        opts => opts.MapFrom(
                            src => ((src.ComboAddSql1 ?? "") != "" || (src.ComboAddSp1 ?? "") != "") ? true : false
                            )
                        )
                .ForMember(dest => dest.ComboAddSql2,
                        opts => opts.MapFrom(
                            src => ((src.ComboAddSql2 ?? "") != "" || (src.ComboAddSp2 ?? "") != "") ? true : false
                            )
                        )
                .ForMember(dest => dest.ComboAddSql3,
                        opts => opts.MapFrom(
                            src => ((src.ComboAddSql3 ?? "") != "" || (src.ComboAddSp3 ?? "") != "") ? true : false
                            )
                        )
                .ForMember(dest => dest.ComboAddSql1Display,
                        opts => opts.MapFrom(
                            src => (src.ComboAddSql1Display ?? "").ToLower()//.ToCamelCase()
                            )
                        )
                .ForMember(dest => dest.ComboAddSql2Display,
                        opts => opts.MapFrom(
                            src => (src.ComboAddSql2Display ?? "").ToLower()//.ToCamelCase()
                            )
                        )
                .ForMember(dest => dest.ComboAddSql3Display,
                        opts => opts.MapFrom(
                            src => (src.ComboAddSql3Display ?? "").ToLower()//.ToCamelCase()
                            )
                        )
                ;
        }

        private void MapView()
        {
            CreateMap<BecaView, dtoBecaView>()
                .ForMember(dest => dest.idView,
                        opts => opts.MapFrom(
                            src => src.idBecaView
                            )
                        )
                .ForMember(dest => dest.Type,
                        opts => opts.MapFrom(
                            src => src.idBecaViewType
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.KeyFields,
                        opts => opts.MapFrom(
                            src => src.PrimaryKey.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(k => k.ToLower()).ToList()
                            )
                        )
                .ForMember(dest => dest.Filters,
                        opts => opts.MapFrom(
                            src => src.BecaViewFilters
                            )
                        )
                .ForMember(dest => dest.FilterValues,
                        opts => opts.MapFrom(
                            src => src.BecaViewFilterValues
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.HasGrid,
                        opts => opts.MapFrom(
                            src => src.HasGrid
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.HasChart,
                        opts => opts.MapFrom(
                            src => src.HasChart
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.IsChartFromApi,
                        opts => opts.MapFrom(
                            src => src.IsChartFromApi
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.isPanelsFromApi,
                        opts => opts.MapFrom(
                            src => src.isPanelsFromApi
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.ChartHasDetail,
                        opts => opts.MapFrom(
                            src => src.ChartHasDetail
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXformula,
                        opts => opts.MapFrom(
                            src => src.viewAxisXformula
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXData,
                        opts => opts.MapFrom(
                            src => (src.viewAxisXData ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXFilters,
                        opts => opts.MapFrom(
                            src => (src.viewAxisXFilters ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXActions,
                        opts => opts.MapFrom(
                            src => (src.viewAxisXActions ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXZoomIf,
                        opts => opts.MapFrom(
                            src => (src.viewAxisXZoomIf ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXZoomTo,
                        opts => opts.MapFrom(
                            src => (src.viewAxisXZoomTo ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.ChartDefinition.viewAxisXStep,
                        opts => opts.MapFrom(
                            src => src.viewAxisXStep
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.HttpGetUrl,
                        opts => opts.MapFrom(
                            src => src.HttpGetUrl
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.viewFields,
                        opts => opts.MapFrom(
                            src => src.BecaViewData
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.viewPanels,
                        opts => opts.MapFrom(
                            src => src.BecaViewPanels
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.childrenForm,
                        opts => opts.MapFrom(
                            src => src.BecaViewChildren
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.DetailComponent,
                        opts => opts.MapFrom(
                            src => src.DetailComponent
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.AddRecord,
                        opts => opts.MapFrom(
                            src => src.AddRecord
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.EditRecord,
                        opts => opts.MapFrom(
                            src => src.EditRecord
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.DeleteRecord,
                        opts => opts.MapFrom(
                            src => src.DeleteRecord
                            )
                        )
                ;

            CreateMap<BecaViewData, dtoBecaData>()
                .ForMember(dest => dest.Name,
                        opts => opts.MapFrom(
                            src => src.Field.ToLower()//.ToCamelCase()
                            )
                        )
                .ForMember(dest => dest.DataType,
                        opts => opts.MapFrom(
                            src => src.idDataType
                            )
                        )
                .ForMember(dest => dest.FromValue,
                        opts => opts.MapFrom(
                            src => src.DropDownDisplayField == null ? null : src.DropDownDisplayField.ToString().Split(",", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()!.ToLower()
                            )
                        )
                .ForMember(dest => dest.Format,
                        opts => opts.MapFrom(
                            src => src.Format ?? ""
                            )
                        );

            CreateMap<BecaViewAction, dtoBecaViewActions>().ReverseMap()
                .ForMember(dest => dest.sqlEmailFrom,
                        opts => opts.MapFrom((src, dest) => (dest.sqlEmailFrom ?? "") != "")
                        );
        }

        private void MapUI()
        {
            CreateMap<BecaViewUI, BecaViewFilterUI>().ReverseMap()
                    .ForMember(dest => dest.Name,
                            opts => opts.MapFrom(
                                src => src.Name.ToLower()
                                )
                            ); ;
            CreateMap<BecaViewUI, BecaViewDetailUI>().ReverseMap()
                    .ForMember(dest => dest.Name,
                            opts => opts.MapFrom(
                                src => src.Name.ToLower()
                                )
                            ); ; ;
        }
    }
}
