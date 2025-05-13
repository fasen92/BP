using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Mappers;
using WifiLocator.Core.Services.Interfaces;
using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Repositories;
using WifiLocator.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace WifiLocator.Core.Services
{
    public class AddressService(
       IUnitOfWorkFactory unitOfWorkFactory,
        ModelMapper<AddressEntity, AddressModel> addressMapper) : IAddressService
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory = unitOfWorkFactory;
        private readonly ModelMapper<AddressEntity, AddressModel> _addressMapper = addressMapper;

        public async Task<AddressModel> SaveAsync(AddressModel model)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<AddressEntity> repository = unitOfWork.GetRepository<AddressEntity>();
            AddressEntity entity = _addressMapper.MapToEntity(model);
            AddressModel returnModel;

            if (await repository.ExistsAsync(entity))
            {
                entity = await repository.Update(entity);
                returnModel = _addressMapper.MapToModel(entity);
            }
            else
            {
                AddressEntity addedEntity = await repository.AddAsync(entity);
                returnModel = _addressMapper.MapToModel(addedEntity);
            }

            await unitOfWork.SaveChangesAsync();
            return returnModel;
        }

        public async Task<AddressModel> GetByAddressAsync(AddressModel model)
        {
            using IUnitOfWork unitOfWork = _unitOfWorkFactory.CreateUnitOfWork();
            IRepository<AddressEntity> repository = unitOfWork.GetRepository<AddressEntity>();
            IQueryable<AddressEntity> query = repository.Query();

            query = query.Where(address =>
                 address.Country == model.Country &&
                 address.City == model.City &&
                 address.Road == model.Road &&
                 address.Region == model.Region &&
                 address.PostalCode == model.PostalCode);

            AddressEntity? returnEntity = await query.FirstOrDefaultAsync();

            if (returnEntity != null)
            {
                 return _addressMapper.MapToModel(returnEntity);
            }
            else
            {
                return await SaveAsync(model);
            }
        }
    }
}
