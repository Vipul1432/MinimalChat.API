using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Retrieves an entity by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to retrieve.</param>
        /// <returns>The entity with the specified identifier or <c>null</c> if not found.</returns>
        Task<TEntity> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all entities of the specified type asynchronously.
        /// </summary>
        /// <returns>A list of all entities of the specified type.</returns>
        Task<List<TEntity>> GetAllAsync();

        /// <summary>
        /// Adds a new entity to the repository and saves changes asynchronously.
        /// </summary>
        /// <param name="entity">The entity to be added.</param>
        /// <returns>The added entity with any updates applied by the database.</returns>
        Task<TEntity> AddAsync(TEntity entity);

        /// <summary>
        /// Updates an existing entity in the repository and saves changes asynchronously.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <returns>The updated entity with any changes applied by the database.</returns>
        Task<TEntity> UpdateAsync(TEntity entity);

        /// <summary>
        /// Deletes an entity by its unique identifier and saves changes asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to delete.</param>
        /// <returns><c>true</c> if the entity was deleted; otherwise, <c>false</c>.</returns>
        Task<bool> DeleteAsync(int id);
    }
}
