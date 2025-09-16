namespace YamyProject.Core.Consts.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CompanyViewModels,TblCompany>().ReverseMap();
        }            
    }
}
