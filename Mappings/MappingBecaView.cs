using AutoMapper;
using BecaWebService.ExtensionsLib;
using Entities.DataTransferObjects;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BecaWebService.Mappings
{
    public class MappingBecaView : Profile
    {
        public MappingBecaView()
        {
            CreateMap<BecaViewFilters, dtoBecaFilter>()
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
                            );
            CreateMap<BecaPanelFilters, dtoBecaFilter>()
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
            CreateMap<BecaFormula, dtoBecaFormula>()
                .ForMember(dest => dest.data,
                        opts => opts.MapFrom(
                            src => src.BecaFormulaData
                            )
                        );

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

            CreateMap<BecaViewData, dtoBecaData>()
                .ForMember(dest => dest.Name,
                        opts => opts.MapFrom(
                            src => src.Name.ToCamelCase()
                            )
                        )
                .ForMember(dest => dest.DataType,
                        opts => opts.MapFrom(
                            src => src.idDataType
                            )
                        );

            CreateMap<BecaView, dtoBecaView>()
                .ForMember(dest => dest.Type,
                        opts => opts.MapFrom(
                            src => src.idBecaViewType
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
                .ForPath(dest => dest.ViewDefinition.ChartHasDetail,
                        opts => opts.MapFrom(
                            src => src.ChartHasDetail
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
                .ForPath(dest => dest.ViewDefinition.viewAxisXData,
                        opts => opts.MapFrom(
                            src => src.viewAxisXData.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
                            )
                        )
                .ForPath(dest => dest.ViewDefinition.viewAxisXFilters,
                        opts => opts.MapFrom(
                            src => src.viewAxisXFilters.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList<string>()
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
                );
        }
    }
}
