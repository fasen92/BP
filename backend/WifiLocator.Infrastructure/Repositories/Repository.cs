using Microsoft.EntityFrameworkCore;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Mappers;

namespace WifiLocator.Infrastructure.Repositories
{
    public class Repository<TEntity>(WifiLocatorDbContext context) : IRepository<TEntity>
        where TEntity : class, IEntity
    {   
        private readonly DbSet<TEntity> DBSet = context.Set<TEntity>();
        private static readonly Dictionary<Type, object> MapperCache = new();

        public IQueryable<TEntity> Query()
        {
            return DBSet;
        }

        public async Task<TEntity> GetByIdAsync(Guid Id, params string[]? navigationPaths)
        {
            
            IQueryable<TEntity> query = DBSet;

            if (navigationPaths != null)
            {
                foreach (var path in navigationPaths)
                {
                    query = query.Include(path);
                }
            }

            return await query.SingleAsync(i => i.Id == Id);
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
           return (await DBSet.AddAsync(entity)).Entity;
        }

        public async ValueTask<bool> ExistsAsync(TEntity entity)
        {
            if (await DBSet.AnyAsync(i => i.Id == entity.Id) && entity.Id != Guid.Empty)
            {
                return true;
            }else{ 
                return false; 
            }
        }

        public async Task<TEntity> Update(TEntity entity)
        {
            TEntity existingEntity = await DBSet.SingleAsync(i=> i.Id == entity.Id);
            var mapper = CreateMapperInstance() ?? throw new InvalidOperationException("Mapper does not exist for this entity");
            mapper.MapToEntity(entity, existingEntity);

            return existingEntity;
        }

        public void Delete(TEntity entity)
        {
            DBSet.Remove(entity);
        }

        public async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
        {
            await DBSet.AddRangeAsync(entities);
            return entities;
        }

        public async Task<List<TEntity>> BulkSaveAsync(IEnumerable<TEntity> entities)
        {
            var entityList = entities.ToList();
            if (entityList.Count == 0) return []; 

            List<Guid> entityIds = entityList.Select(e => e.Id).Where(id => id != Guid.Empty).ToList();
            var existingEntities = await DBSet.Where(e => entityIds.Contains(e.Id)).ToListAsync();

            var existingEntitiesDict = existingEntities.ToDictionary(e => e.Id);
            List<TEntity> newEntities = new List<TEntity>();
            List<TEntity> updatedEntities = new List<TEntity>();

            foreach (var entity in entityList)
            {
                if (entity.Id == Guid.Empty || !existingEntitiesDict.TryGetValue(entity.Id, out TEntity? existingEntity))
                {
                    entity.Id = Guid.NewGuid();
                    newEntities.Add(entity);
                }
                else
                {
                    var mapper = CreateMapperInstance() ?? throw new InvalidOperationException("Mapper not found for this entity");
                    mapper.MapToEntity(entity, existingEntity);
                    updatedEntities.Add(existingEntity);
                }
            }

            if (newEntities.Count != 0)
            {
                await DBSet.AddRangeAsync(newEntities);
            }

            return newEntities.Concat(updatedEntities).ToList();

        }


        private static IMapper<TEntity>? CreateMapperInstance()
        {
            if (MapperCache.TryGetValue(typeof(TEntity), out var cached))
            {
                return (IMapper<TEntity>)cached;
            }

            var mapperType = typeof(IMapper<>).MakeGenericType(typeof(TEntity));

            var mapperImplementation = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => mapperType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (mapperImplementation == null)
                return null;

            var instance = (IMapper<TEntity>?)Activator.CreateInstance(mapperImplementation);
            if (instance != null)
            {
                MapperCache[typeof(TEntity)] = instance;
            }

            return instance;
        }
    }
}
