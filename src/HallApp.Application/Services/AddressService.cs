using AutoMapper;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;
using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Application.Services;

public class AddressService : IAddressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddressService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Address>> GetAddressesByCustomerIdAsync(int customerId)
    {
        return await _unitOfWork.AddressRepository.GetAddressesByCustomerIdAsync(customerId);
    }

    public async Task<Address?> GetAddressByIdAsync(int addressId)
    {
        return await _unitOfWork.AddressRepository.GetByIdAsync(addressId);
    }

    public async Task<Address?> GetMainAddressByCustomerIdAsync(int customerId)
    {
        return await _unitOfWork.AddressRepository.GetMainAddressByCustomerIdAsync(customerId);
    }

    public async Task<Address> CreateAddressAsync(Address address)
    {
        // If this is the first address for the customer, make it main
        var existingAddresses = await _unitOfWork.AddressRepository.GetAddressesByCustomerIdAsync(address.CustomerId);
        if (!existingAddresses.Any())
        {
            address.IsMain = true;
        }

        await _unitOfWork.AddressRepository.AddAsync(address);
        await _unitOfWork.Complete();
        return address;
    }

    public async Task<Address?> UpdateAddressAsync(Address address)
    {
        _unitOfWork.AddressRepository.Update(address);
        await _unitOfWork.Complete();
        return address;
    }

    public async Task<bool> DeleteAddressAsync(int customerId, int addressId)
    {
        var result = await _unitOfWork.AddressRepository.DeleteByCustomerIdAsync(customerId, addressId);
        if (result)
        {
            await _unitOfWork.Complete();
        }
        return result;
    }

    public async Task<bool> SetMainAddressAsync(int customerId, int addressId)
    {
        var result = await _unitOfWork.AddressRepository.SetMainAddressAsync(customerId, addressId);
        if (result)
        {
            await _unitOfWork.Complete();
        }
        return result;
    }
}
