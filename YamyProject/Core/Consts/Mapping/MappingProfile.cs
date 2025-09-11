using YamyProject.Core.ViewModel;

namespace YamyProject.Core.Consts.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<CompanyViewModel,TblCompany>().ReverseMap();
        }            
    }
}
