namespace YamyProject.Core.Consts.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CompanyViewModels,TblCompany>().ReverseMap();



            CreateMap<StockSettlementIndexViewModel, TblItemStockSettlement>().ReverseMap();
            //CreateMap<TblItemStockSettlement, ItemStockSettlementListVm>()
           //.ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.HasValue ? src.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null));

            //CreateMap<TblItemStockSettlementDetail, ItemStockSettlementItemVm>()
            //    .ForMember(d => d.ItemCode, opt => opt.MapFrom(s => s.Item!.Code))
            //    .ForMember(d => d.ItemName, opt => opt.MapFrom(s => s.Item!.Name));
        }            
    }
}
