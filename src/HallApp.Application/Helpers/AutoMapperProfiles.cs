using AutoMapper;
using HallApp.Core.Entities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.ChamperEntities.ContactEntities;
using HallApp.Core.Entities.ChamperEntities.MediaEntities;
using HallApp.Core.Entities.ChamperEntities.PackageEntities;
using HallApp.Core.Entities.ChamperEntities.ServiceEntities;
using HallApp.Application.DTOs;
using HallApp.Application.DTOs.Vendors;
using HallApp.Application.DTOs.Customer;
using HallApp.Application.DTOs.Auth;
using HallApp.Application.DTOs.Vendor;
using HallApp.Application.DTOs.HallManager;
using HallApp.Application.DTOs.Champer.Hall;
using HallApp.Application.DTOs.Champer.HallManager;
using HallApp.Application.DTOs.Champer.Contact;
using HallApp.Application.DTOs.Champer.Location;
using HallApp.Application.DTOs.Champer.Media;
using HallApp.Application.DTOs.Champer.Package;
using HallApp.Application.DTOs.Champer.Service;

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

        // Address mappings
        CreateMap<Address, AddressDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AddressName, opt => opt.MapFrom(src => src.Street)) // Map Street to AddressName
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.Street1, opt => opt.MapFrom(src => src.Street))
            .ForMember(dest => dest.Street2, opt => opt.MapFrom(src => src.State)) // Map State to Street2
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.ZipCode) ? 0 : int.Parse(src.ZipCode)))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.ZipCode) ? 0 : int.Parse(src.ZipCode)))
            .ForMember(dest => dest.IsMain, opt => opt.MapFrom(src => src.IsMain))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<AddressDto, Address>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street1 ?? src.AddressName))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Street2 ?? ""))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.ZipCode.ToString()))
            .ForMember(dest => dest.IsMain, opt => opt.MapFrom(src => src.IsMain))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.Customer, opt => opt.Ignore());

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
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? ""))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone ?? ""))
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
                    Updated = src.UserUpdated
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

        // Initialize Media mappings
        CreateMediaMappings();

        // Hall mappings
        CreateMap<Hall, HallDto>()
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
            .ForMember(dest => dest.MaleActive, opt => opt.MapFrom(src => src.MaleActive))
            .ForMember(dest => dest.FemaleWeekDays, opt => opt.MapFrom(src => src.FemaleWeekDays))
            .ForMember(dest => dest.FemaleWeekEnds, opt => opt.MapFrom(src => src.FemaleWeekEnds))
            .ForMember(dest => dest.FemaleMin, opt => opt.MapFrom(src => src.FemaleMin))
            .ForMember(dest => dest.FemaleMax, opt => opt.MapFrom(src => src.FemaleMax))
            .ForMember(dest => dest.FemaleActive, opt => opt.MapFrom(src => src.FemaleActive))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src => src.AverageRating))
            .ForMember(dest => dest.Media, opt => opt.MapFrom(src => src.Logo))
            .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles ?? new List<HallMedia>()))
            .ForMember(dest => dest.Managers, opt => opt.MapFrom(src => src.Managers ?? new List<HallManager>()))
            .ForMember(dest => dest.Contacts, opt => opt.MapFrom(src => src.Contacts ?? new List<Contact>()))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.Packages ?? new List<Package>()))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services ?? new List<Service>()))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<HallDto, Hall>()
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
            .ForMember(dest => dest.MaleActive, opt => opt.MapFrom(src => src.MaleActive))
            .ForMember(dest => dest.FemaleWeekDays, opt => opt.MapFrom(src => src.FemaleWeekDays))
            .ForMember(dest => dest.FemaleWeekEnds, opt => opt.MapFrom(src => src.FemaleWeekEnds))
            .ForMember(dest => dest.FemaleMin, opt => opt.MapFrom(src => src.FemaleMin))
            .ForMember(dest => dest.FemaleMax, opt => opt.MapFrom(src => src.FemaleMax))
            .ForMember(dest => dest.FemaleActive, opt => opt.MapFrom(src => src.FemaleActive))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.Logo, opt => opt.MapFrom(src => src.Media))
            .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles))
            .ForMember(dest => dest.Managers, opt => opt.MapFrom(src => src.Managers))
            .ForMember(dest => dest.Contacts, opt => opt.MapFrom(src => src.Contacts))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.Packages))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Description, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
            .ForMember(dest => dest.Active, opt => opt.Ignore());

        CreateMap<HallCreateDto, Hall>()
            .ForMember(dest => dest.ID, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.CommercialRegisteration, opt => opt.MapFrom(src => src.CommercialRegisteration))
            .ForMember(dest => dest.Vat, opt => opt.MapFrom(src => src.Vat))
            .ForMember(dest => dest.BothWeekDays, opt => opt.MapFrom(src => src.BothWeekDays))
            .ForMember(dest => dest.BothWeekEnds, opt => opt.MapFrom(src => src.BothWeekEnds))
            .ForMember(dest => dest.MaleWeekDays, opt => opt.MapFrom(src => src.MaleWeekDays))
            .ForMember(dest => dest.MaleWeekEnds, opt => opt.MapFrom(src => src.MaleWeekEnds))
            .ForMember(dest => dest.MaleMin, opt => opt.MapFrom(src => src.MaleMin))
            .ForMember(dest => dest.MaleMax, opt => opt.MapFrom(src => src.MaleMax))
            .ForMember(dest => dest.MaleActive, opt => opt.MapFrom(src => src.MaleActive))
            .ForMember(dest => dest.FemaleWeekDays, opt => opt.MapFrom(src => src.FemaleWeekDays))
            .ForMember(dest => dest.FemaleWeekEnds, opt => opt.MapFrom(src => src.FemaleWeekEnds))
            .ForMember(dest => dest.FemaleMin, opt => opt.MapFrom(src => src.FemaleMin))
            .ForMember(dest => dest.FemaleMax, opt => opt.MapFrom(src => src.FemaleMax))
            .ForMember(dest => dest.FemaleActive, opt => opt.MapFrom(src => src.FemaleActive))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles))
            .ForMember(dest => dest.Contacts, opt => opt.MapFrom(src => src.Contacts))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.Packages))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Logo, opt => opt.Ignore())
            .ForMember(dest => dest.Managers, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
            .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true));

        CreateMap<HallUpdateDto, Hall>()
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
            .ForMember(dest => dest.MaleActive, opt => opt.MapFrom(src => src.MaleActive))
            .ForMember(dest => dest.FemaleWeekDays, opt => opt.MapFrom(src => src.FemaleWeekDays))
            .ForMember(dest => dest.FemaleWeekEnds, opt => opt.MapFrom(src => src.FemaleWeekEnds))
            .ForMember(dest => dest.FemaleMin, opt => opt.MapFrom(src => src.FemaleMin))
            .ForMember(dest => dest.FemaleMax, opt => opt.MapFrom(src => src.FemaleMax))
            .ForMember(dest => dest.FemaleActive, opt => opt.MapFrom(src => src.FemaleActive))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.MediaFiles, opt => opt.MapFrom(src => src.MediaFiles))
            .ForMember(dest => dest.Contacts, opt => opt.MapFrom(src => src.Contacts))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.Packages, opt => opt.MapFrom(src => src.Packages))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.Services))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Logo, opt => opt.Ignore())
            .ForMember(dest => dest.Managers, opt => opt.Ignore())
            .ForMember(dest => dest.Reviews, opt => opt.Ignore())
            .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
            .ForMember(dest => dest.Active, opt => opt.Ignore())
            .ForMember(dest => dest.Created, opt => opt.Ignore());

        // Booking mappings - Enhanced with comprehensive customer information
        CreateMap<HallApp.Core.Entities.BookingEntities.Booking, HallApp.Application.DTOs.Booking.BookingDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.HallId, opt => opt.MapFrom(src => src.HallId))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod ?? ""))
            .ForMember(dest => dest.Coupon, opt => opt.MapFrom(src => src.Coupon ?? ""))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments ?? ""))
            // Financial fields - new structure
            .ForMember(dest => dest.HallCost, opt => opt.MapFrom(src => src.HallCost))
            .ForMember(dest => dest.VendorServicesCost, opt => opt.MapFrom(src => src.VendorServicesCost))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
            .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.DiscountAmount))
            .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount))
            .ForMember(dest => dest.TaxRate, opt => opt.MapFrom(src => src.TaxRate))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency ?? "SAR"))
            .ForMember(dest => dest.PaidAt, opt => opt.MapFrom(src => src.PaidAt))
            .ForMember(dest => dest.VisitDate, opt => opt.MapFrom(src => src.VisitDate))
            .ForMember(dest => dest.IsVisitCompleted, opt => opt.MapFrom(src => src.IsVisitCompleted))
            .ForMember(dest => dest.IsBookingConfirmed, opt => opt.MapFrom(src => src.IsBookingConfirmed))
            .ForMember(dest => dest.BookingDate, opt => opt.MapFrom(src => src.BookingDate))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus ?? "Pending"))
            // Enhanced customer information
            .ForMember(dest => dest.EventDate, opt => opt.MapFrom(src => src.EventDate))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType ?? ""))
            .ForMember(dest => dest.GuestCount, opt => opt.MapFrom(src => src.GuestCount))
            .ForMember(dest => dest.GenderPreference, opt => opt.MapFrom(src => src.GenderPreference))
            .ForMember(dest => dest.VendorServices, opt => opt.MapFrom(src => src.VendorBookings ?? new List<HallApp.Core.Entities.VendorEntities.VendorBooking>()))
            .ForMember(dest => dest.FinancialSummary, opt => opt.MapFrom(src => new HallApp.Application.DTOs.Booking.BookingFinancialSummary 
            {
                HallCost = src.HallCost,
                VendorsCost = src.VendorServicesCost,
                SubTotal = src.Subtotal,
                DiscountAmount = src.DiscountAmount,
                TaxAmount = src.TaxAmount,
                TaxRate = src.TaxRate,
                TotalAmount = src.TotalAmount,
                Currency = src.Currency ?? "SAR",
                CalculatedAt = DateTime.UtcNow,
                VendorBreakdown = new List<HallApp.Application.DTOs.Booking.VendorFinancialBreakdownDto>()
            }))
            // Map hall information when available
            .ForMember(dest => dest.Hall, opt => opt.MapFrom(src => src.Hall))
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.PackageDetails, opt => opt.Ignore());

        CreateMap<HallApp.Application.DTOs.Booking.BookingDto, HallApp.Core.Entities.BookingEntities.Booking>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.HallId, opt => opt.MapFrom(src => src.HallId))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
            .ForMember(dest => dest.Coupon, opt => opt.MapFrom(src => src.Coupon))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
            .ForMember(dest => dest.IsVisitCompleted, opt => opt.MapFrom(src => src.IsVisitCompleted))
            .ForMember(dest => dest.IsBookingConfirmed, opt => opt.MapFrom(src => src.IsBookingConfirmed))
            .ForMember(dest => dest.BookingDate, opt => opt.MapFrom(src => src.BookingDate))
            .ForMember(dest => dest.VisitDate, opt => opt.MapFrom(src => src.VisitDate))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.VendorBookings, opt => opt.Ignore())
            .ForMember(dest => dest.PackageDetails, opt => opt.Ignore())
            .ForMember(dest => dest.EventDate, opt => opt.MapFrom(src => src.EventDate))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType))
            .ForMember(dest => dest.GuestCount, opt => opt.MapFrom(src => src.GuestCount))
            .ForMember(dest => dest.GenderPreference, opt => opt.MapFrom(src => src.GenderPreference))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus))
            // New financial fields
            .ForMember(dest => dest.HallCost, opt => opt.MapFrom(src => src.HallCost))
            .ForMember(dest => dest.VendorServicesCost, opt => opt.MapFrom(src => src.VendorServicesCost))
            .ForMember(dest => dest.Subtotal, opt => opt.MapFrom(src => src.Subtotal))
            .ForMember(dest => dest.DiscountAmount, opt => opt.MapFrom(src => src.DiscountAmount))
            .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount))
            .ForMember(dest => dest.TaxRate, opt => opt.MapFrom(src => src.TaxRate))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.PaidAt, opt => opt.MapFrom(src => src.PaidAt))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        // Hall to HallBookingDto mapping - enhanced for booking details
        CreateMap<HallApp.Core.Entities.ChamperEntities.Hall, HallApp.Application.DTOs.Champer.Hall.HallBookingDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.WhatsApp, opt => opt.MapFrom(src => src.WhatsApp))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.Managers, opt => opt.MapFrom(src => src.Managers));

        // VendorBooking to VendorBookingDto mapping
        CreateMap<HallApp.Core.Entities.VendorEntities.VendorBooking, HallApp.Application.DTOs.Vendors.VendorBookingDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BookingId, opt => opt.MapFrom(src => src.BookingId))
            .ForMember(dest => dest.VendorId, opt => opt.MapFrom(src => src.VendorId))
            .ForMember(dest => dest.VendorName, opt => opt.MapFrom(src => src.Vendor != null ? src.Vendor.Name : "Unknown Vendor"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => (double)src.TotalAmount))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.BookingReference, opt => opt.Ignore())
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.IsPaid, opt => opt.MapFrom(src => src.IsPaid))
            .ForMember(dest => dest.PaidAt, opt => opt.MapFrom(src => src.PaidAt))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus))
            .ForMember(dest => dest.CancellationReason, opt => opt.MapFrom(src => src.CancellationReason))
            .ForMember(dest => dest.CancelledAt, opt => opt.MapFrom(src => src.CancelledAt))
            .ForMember(dest => dest.ServiceItems, opt => opt.MapFrom(src => src.Services))
            .ForMember(dest => dest.ContactInfo, opt => opt.MapFrom(src => src.Vendor != null ? new VendorContactInfo
            {
                Phone = src.Vendor.Phone ?? "",
                Email = src.Vendor.Email ?? "",
                WhatsApp = ""
            } : null));

        // VendorBookingService to VendorBookingServiceDto mapping
        CreateMap<HallApp.Core.Entities.VendorEntities.VendorBookingService, HallApp.Application.DTOs.Vendors.VendorBookingServiceDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ServiceItem != null ? src.ServiceItem.Id : src.ServiceItemId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ServiceItem != null ? src.ServiceItem.Name : "Unknown Service"))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ServiceItem != null ? src.ServiceItem.Description : ""))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => (double)src.UnitPrice))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.SpecialInstructions, opt => opt.MapFrom(src => src.SpecialInstructions ?? ""))
            .ForMember(dest => dest.VendorBookingId, opt => opt.MapFrom(src => src.VendorBookingId));

        // Notification mappings
        CreateMap<HallApp.Core.Entities.NotificationEntities.Notification, HallApp.Application.DTOs.Notifications.NotificationResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUserId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type ?? "General"))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAt));

        CreateMap<HallApp.Application.DTOs.Notifications.NotificationDto, HallApp.Core.Entities.NotificationEntities.Notification>()
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUserId))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ReadAt, opt => opt.Ignore());
    }

    // Helper method to calculate financial summary for BookingDto
    private static HallApp.Application.DTOs.Booking.BookingFinancialSummary CalculateFinancialSummary(HallApp.Core.Entities.BookingEntities.Booking booking)
    {
        // Use new financial fields from updated Booking entity
        var hallCost = booking.HallCost;
        var vendorsCost = booking.VendorServicesCost;
        var subTotal = booking.Subtotal;
        var discountAmount = booking.DiscountAmount;
        var taxAmount = booking.TaxAmount;
        var totalAmount = booking.TotalAmount;

        return new HallApp.Application.DTOs.Booking.BookingFinancialSummary
        {
            HallCost = hallCost,
            VendorsCost = vendorsCost,
            SubTotal = subTotal,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            TaxRate = booking.TaxRate,
            TotalAmount = totalAmount,
            Currency = booking.Currency ?? "SAR",
            CalculatedAt = DateTime.UtcNow,
            VendorBreakdown = new List<HallApp.Application.DTOs.Booking.VendorFinancialBreakdownDto>()
        };
    }

    private void CreateMediaMappings()
    {
        // Base Media mappings
        CreateMap<Media, MediaDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.MediaType, opt => opt.MapFrom(src => src.MediaType))
            .ForMember(dest => dest.URL, opt => opt.MapFrom(src => src.URL))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.index, opt => opt.MapFrom(src => src.index))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<MediaDto, Media>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.MediaType, opt => opt.MapFrom(src => src.MediaType))
            .ForMember(dest => dest.URL, opt => opt.MapFrom(src => src.URL))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.index, opt => opt.MapFrom(src => src.index))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Hall, opt => opt.Ignore());

        // HallMedia mappings
        CreateMap<HallMedia, HallMediaDto>()
            .IncludeBase<Media, MediaDto>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender));

        CreateMap<HallMediaDto, HallMedia>()
            .IncludeBase<MediaDto, Media>()
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender));

        // Contact mappings
        CreateMap<Contact, ContactDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<ContactDto, Contact>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Hall, opt => opt.Ignore());

        // Location mappings
        CreateMap<HallApp.Core.Entities.ChamperEntities.LocationEntities.Location, LocationDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Altitude, opt => opt.MapFrom(src => src.Altitude))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<LocationDto, HallApp.Core.Entities.ChamperEntities.LocationEntities.Location>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.Altitude, opt => opt.MapFrom(src => src.Altitude))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Hall, opt => opt.Ignore());

        // Package mappings
        CreateMap<Package, PackageDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<PackageDto, Package>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Hall, opt => opt.Ignore());

        // Service mappings
        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ArabicName, opt => opt.MapFrom(src => src.ArabicName))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated));

        CreateMap<ServiceDto, Service>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.ArabicName, opt => opt.MapFrom(src => src.ArabicName))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.HallID, opt => opt.MapFrom(src => src.HallID))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.Updated))
            .ForMember(dest => dest.Hall, opt => opt.Ignore());

        // HallManager mappings
        CreateMap<HallManager, HallManagerDto>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CompanyName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.AppUser != null ? src.AppUser.Email : ""))
            .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.AppUser != null ? src.AppUser.PhoneNumber : ""))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.AppUser != null ? src.AppUser.UserName : ""))
            .ForMember(dest => dest.AppUserID, opt => opt.MapFrom(src => src.AppUserId))
            .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.IsApproved))
            .ForMember(dest => dest.Created, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.Updated, opt => opt.MapFrom(src => src.ApprovedAt ?? src.CreatedAt))
            .ForMember(dest => dest.Password, opt => opt.Ignore())
            .ForMember(dest => dest.Halls, opt => opt.Ignore());

        CreateMap<HallManagerDto, HallManager>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.AppUserId, opt => opt.MapFrom(src => src.AppUserID))
            .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.Active))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.Created))
            .ForMember(dest => dest.ApprovedAt, opt => opt.MapFrom(src => src.Active ? src.Updated : (DateTime?)null))
            .ForMember(dest => dest.CommercialRegistrationNumber, opt => opt.Ignore())
            .ForMember(dest => dest.AppUser, opt => opt.Ignore());
    }
}
