using AutoMapper;
using HallApp.Core.Entities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Application.DTOs;
using HallApp.Application.DTOs.Vendors;
using HallApp.Application.DTOs.Customer;
using HallApp.Application.DTOs.Auth;
using HallApp.Application.DTOs.Vendor;
using HallApp.Application.DTOs.HallManager;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Application.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        // User mappings
        CreateMap<AppUser, HallApp.Application.DTOs.UserDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ReverseMap();

        // Vendor mappings
        CreateMap<Vendor, VendorDto>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.ReviewCount));
        
        CreateMap<Vendor, VendorListDto>()
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.ReviewCount));
            
        CreateMap<VendorDto, Vendor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LogoUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.ReviewCount));

        CreateMap<CreateVendorDto, Vendor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LogoUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CoverImageUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.ReviewCount));

        // VendorManager mappings
        CreateMap<VendorManager, VendorManagerDto>();

        // ServiceItem mappings
        CreateMap<ServiceItem, ServiceItemDto>();
        CreateMap<ServiceItemDto, ServiceItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

        // VendorLocation mappings
        CreateMap<VendorLocation, VendorLocationDto>();
        CreateMap<VendorLocationDto, VendorLocation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // VendorBusinessHour mappings
        CreateMap<VendorBusinessHour, VendorBusinessHourDto>();
        CreateMap<VendorBusinessHourDto, VendorBusinessHour>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // VendorBlockedDate mappings
        CreateMap<VendorBlockedDate, VendorBlockedDateDto>();
        CreateMap<VendorBlockedDateDto, VendorBlockedDate>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // VendorType mappings
        CreateMap<VendorType, VendorTypeDto>();
        CreateMap<VendorTypeDto, VendorType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        // Customer mappings
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUserId))
            .ForMember(dest => dest.CreditMoney, opt => opt.MapFrom(src => src.CreditMoney))
            .ForMember(dest => dest.NumberOfOrders, opt => opt.MapFrom(src => src.NumberOfOrders))
            .ForMember(dest => dest.SelectedAddressId, opt => opt.MapFrom(src => src.SelectedAddressId))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Bookings, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.Favorites, opt => opt.Ignore());

        CreateMap<CustomerDto, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true)) // Default to active
            .ForMember(dest => dest.Confirmed, opt => opt.MapFrom(src => false)) // Default to unconfirmed
            .ForMember(dest => dest.AppUser, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.Bookings, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.Favorites, opt => opt.Ignore());

        // Customer Business DTO mappings
        CreateMap<Customer, CustomerBusinessDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.CreditMoney, opt => opt.MapFrom(src => src.CreditMoney))
            .ForMember(dest => dest.NumberOfOrders, opt => opt.MapFrom(src => src.NumberOfOrders))
            .ForMember(dest => dest.SelectedAddressId, opt => opt.MapFrom(src => src.SelectedAddressId))
            .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.Active))
            .ForMember(dest => dest.Confirmed, opt => opt.MapFrom(src => src.Confirmed))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<CustomerBusinessDto, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AppUserId, opt => opt.Ignore())
            .ForMember(dest => dest.AppUser, opt => opt.Ignore())
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.Bookings, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.Favorites, opt => opt.Ignore());

        // User Profile DTO mappings
        CreateMap<AppUser, UserProfileDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => src.EmailConfirmed))
            .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.Active))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<UserProfileDto, AppUser>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedUserName, opt => opt.Ignore())
            .ForMember(dest => dest.NormalizedEmail, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityStamp, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnd, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEnabled, opt => opt.Ignore())
            .ForMember(dest => dest.AccessFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore());

        // Customer Profile DTO mappings (combines AppUser + Customer)
        CreateMap<CustomerProfileDto, (AppUser AppUser, Customer Customer)>()
            .ConvertUsing((src, dest, context) => {
                var appUser = new AppUser
                {
                    Id = src.AppUserId,
                    Email = src.Email,
                    FirstName = src.FirstName,
                    LastName = src.LastName,
                    PhoneNumber = src.PhoneNumber,
                    EmailConfirmed = src.EmailConfirmed,
                    Active = src.Active,
                    Created = src.UserCreated,
                    Updated = DateTime.UtcNow
                };
                
                var customer = new Customer
                {
                    Id = src.CustomerId,
                    AppUserId = src.AppUserId,
                    CreditMoney = src.CreditMoney,
                    NumberOfOrders = src.NumberOfOrders,
                    SelectedAddressId = src.SelectedAddressId,
                    Active = src.Active,
                    Confirmed = src.CustomerConfirmed,
                    Created = src.CustomerCreated,
                    Updated = src.CustomerUpdated
                };
                
                return (appUser, customer);
            });

        // Vendor Business DTO mappings
        CreateMap<Vendor, VendorBusinessDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.LogoUrl, opt => opt.MapFrom(src => src.LogoUrl))
            .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.CoverImageUrl))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Rating))
            .ForMember(dest => dest.ReviewCount, opt => opt.MapFrom(src => src.ReviewCount))
            .ForMember(dest => dest.VendorTypeId, opt => opt.MapFrom(src => src.VendorTypeId))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        CreateMap<VendorBusinessDto, Vendor>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.Managers, opt => opt.Ignore())
            .ForMember(dest => dest.ServiceItems, opt => opt.Ignore())
            .ForMember(dest => dest.BusinessHours, opt => opt.Ignore())
            .ForMember(dest => dest.BlockedDates, opt => opt.Ignore())
            .ForMember(dest => dest.VendorBookings, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.VendorType, opt => opt.Ignore());

        // VendorManager Business DTO mappings
        CreateMap<VendorManager, VendorManagerBusinessDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUserId))
            .ForMember(dest => dest.CommercialRegistrationNumber, opt => opt.MapFrom(src => src.CommercialRegistrationNumber))
            .ForMember(dest => dest.VatNumber, opt => opt.MapFrom(src => src.VatNumber))
            .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ApprovedAt, opt => opt.MapFrom(src => src.ApprovedAt));

        CreateMap<VendorManagerBusinessDto, VendorManager>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AppUser, opt => opt.Ignore())
            .ForMember(dest => dest.Vendors, opt => opt.Ignore());

        // VendorManager Profile DTO mappings (combines AppUser + VendorManager)
        CreateMap<VendorManagerProfileDto, (AppUser AppUser, VendorManager VendorManager)>()
            .ConvertUsing((src, dest, context) => {
                var appUser = new AppUser
                {
                    Id = src.AppUserId,
                    Email = src.Email,
                    FirstName = src.FirstName,
                    LastName = src.LastName,
                    PhoneNumber = src.PhoneNumber,
                    EmailConfirmed = src.EmailConfirmed,
                    Active = src.Active,
                    Created = src.UserCreated,
                    Updated = src.UserUpdated ?? DateTime.UtcNow
                };
                
                var vendorManager = new VendorManager
                {
                    Id = src.VendorManagerId,
                    AppUserId = src.AppUserId,
                    CommercialRegistrationNumber = src.CommercialRegistrationNumber,
                    VatNumber = src.VatNumber,
                    IsApproved = src.IsApproved,
                    CreatedAt = src.VendorManagerCreated,
                    ApprovedAt = src.ApprovedAt
                };
                
                return (appUser, vendorManager);
            });
    }
}
