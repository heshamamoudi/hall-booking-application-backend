using AutoMapper;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Web.DTOs;

namespace HallApp.Web.Helpers
{
    public class WebAutoMapperProfiles : Profile
    {
        public WebAutoMapperProfiles()
        {
            // HallUpdateWithFilesDto mapping (for file upload endpoint)
            CreateMap<HallUpdateWithFilesDto, Hall>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.CommercialRegisteration, opt => opt.MapFrom(src => src.CommercialRegisteration))
                .ForMember(dest => dest.Vat, opt => opt.MapFrom(src => src.Vat))
                .ForMember(dest => dest.BothWeekDays, opt => opt.MapFrom(src => src.BothWeekDays))
                .ForMember(dest => dest.BothWeekEnds, opt => opt.MapFrom(src => src.BothWeekEnds))
                .ForMember(dest => dest.MaleWeekDays, opt => opt.MapFrom(src => src.MaleWeekDays))
                .ForMember(dest => dest.MaleWeekEnds, opt => opt.MapFrom(src => src.MaleWeekEnds))
                .ForMember(dest => dest.MaleMin, opt => opt.MapFrom(src => src.MaleMin))
                .ForMember(dest => dest.MaleMax, opt => opt.MapFrom(src => src.MaleMax))
                .ForMember(dest => dest.FemaleWeekDays, opt => opt.MapFrom(src => src.FemaleWeekDays))
                .ForMember(dest => dest.FemaleWeekEnds, opt => opt.MapFrom(src => src.FemaleWeekEnds))
                .ForMember(dest => dest.FemaleMin, opt => opt.MapFrom(src => src.FemaleMin))
                .ForMember(dest => dest.FemaleMax, opt => opt.MapFrom(src => src.FemaleMax))
                .ForMember(dest => dest.FemaleActive, opt => opt.MapFrom(src => src.FemaleActive))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Logo, opt => opt.Ignore())
                .ForMember(dest => dest.MediaFiles, opt => opt.Ignore())  // Handled separately in controller
                .ForMember(dest => dest.Managers, opt => opt.Ignore())
                .ForMember(dest => dest.Contacts, opt => opt.Ignore())
                .ForMember(dest => dest.Location, opt => opt.Ignore())
                .ForMember(dest => dest.Packages, opt => opt.Ignore())
                .ForMember(dest => dest.Services, opt => opt.Ignore())
                .ForMember(dest => dest.Reviews, opt => opt.Ignore())
                .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
                .ForMember(dest => dest.Active, opt => opt.Ignore())
                .ForMember(dest => dest.Created, opt => opt.Ignore())
                .ForMember(dest => dest.MaleActive, opt => opt.Ignore());
        }
    }
}
