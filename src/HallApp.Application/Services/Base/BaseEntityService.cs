using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;

namespace HallApp.Application.Services.Base;

public abstract class BaseEntityService<TEntity> where TEntity : class
{
    protected readonly IUnitOfWork _unitOfWork;

    protected BaseEntityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected abstract IGenericRepository<TEntity> Repository { get; }

    public virtual async Task<TEntity> GetByIdAsync(int id)
    {
        return await Repository.GetByIdAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await Repository.GetAllAsync();
    }

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        await Repository.AddAsync(entity);
        await _unitOfWork.Complete();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await Repository.GetByIdAsync(id);
        if (entity == null) return false;
        
        Repository.Delete(entity);
        await _unitOfWork.Complete();
        return true;
    }

    public virtual async Task<int> CountAsync()
    {
        var entities = await Repository.GetAllAsync();
        return entities.Count();
    }
}
