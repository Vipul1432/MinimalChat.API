using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinmalChat.Data.Repository
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly MinimalChatDbContext _context;

        public Repository(MinimalChatDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves an entity by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to retrieve.</param>
        /// <returns>The entity with the specified identifier or <c>null</c> if not found.</returns>
        public async Task<TEntity> GetByIdAsync(int id)
        {
            return await _context.Set<TEntity>().FindAsync(id);
        }

        /// <summary>
        /// Retrieves all entities of the specified type asynchronously.
        /// </summary>
        /// <returns>A list of all entities of the specified type.</returns>
        public async Task<List<TEntity>> GetAllAsync()
        {
            return await _context.Set<TEntity>().ToListAsync();
        }

        /// <summary>
        /// Adds a new entity to the repository and saves changes asynchronously.
        /// </summary>
        /// <param name="entity">The entity to be added.</param>
        /// <returns>The added entity with any updates applied by the database.</returns>
        public async Task<TEntity> AddAsync(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Updates an existing entity in the repository and saves changes asynchronously.
        /// </summary>
        /// <param name="entity">The entity to be updated.</param>
        /// <returns>The updated entity with any changes applied by the database.</returns>
        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Deletes an entity by its unique identifier and saves changes asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the entity to delete.</param>
        /// <returns><c>true</c> if the entity was deleted; otherwise, <c>false</c>.</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
                return false;

            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Asynchronously retrieves a list of groups associated with a user identified by their user ID.
        /// </summary>
        /// <param name="currentUserId">The user's unique identifier.</param>
        /// <returns>A list of <see cref="Group"/> objects representing the user's groups.</returns>
        public async Task<List<Group>> GetUserGroupsByUserIdAsync(string currentUserId)
        {
            return await _context.Groups.Include(x => x.Members).Where(g => g.Members.Any(m => m.UserId == currentUserId)).ToListAsync();
        }

        /// <summary>
        /// Asynchronously checks if a user is a member of a specific group.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <param name="memberId">The unique identifier of the user to check.</param>
        /// <returns>A <see cref="GroupMember"/> entity if the user is a member, or <c>null</c> if not part of the group.</returns>
        public async Task<GroupMember> MemberExistsInGroupAsync(Guid groupId, string memberId)
        {
            return await _context.Set<GroupMember>().FirstOrDefaultAsync(grpmem => grpmem.GroupId == groupId && grpmem.UserId == memberId);
        }

        /// <summary>
        /// Asynchronously retrieves a <see cref="GroupMember"/> entity based on the provided member ID.
        /// </summary>
        /// <param name="memberId">The unique identifier of the group member.</param>
        /// <returns>The <see cref="GroupMember"/> entity representing the specified group member.</returns>
        public async Task<GroupMember> GetGroupMemberByIdAsync(string memberId)
        {
            return await _context.Set<GroupMember>().FirstOrDefaultAsync(grpmem => grpmem.UserId == memberId);
        }

        /// <summary>
        /// Asynchronously retrieves a <see cref="Group"/> entity based on its unique identifier.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <returns>The <see cref="Group"/> entity representing the specified group.</returns>
        public async Task<Group> GetGroupByIdAsync(Guid groupId)
        {
            return await _context.Set<Group>().FirstOrDefaultAsync(grp => grp.Id == groupId);
        }

        /// <summary>
        /// Asynchronously deletes the provided entity from the database.
        /// </summary>
        /// <param name="entity">The entity to be deleted.</param>
        /// <returns>A boolean indicating whether the operation was successful.</returns>
        public async Task<bool> DeleteAsync(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Asynchronously fetches the chat history timestamp for a group based on its unique identifier.
        /// </summary>
        /// <param name="groupId">The unique identifier of the group.</param>
        /// <returns>A nullable <see cref="DateTime"/> representing the chat history time if available, or <c>null</c> if not found.</returns>
        public async Task<DateTime?> GetChatHistoryTimeAsync(Guid groupId)
        {
            return await _context.Set<GroupMember>().Where(grp => grp.GroupId == groupId).Select(x => x.ChatHistoryTime).FirstOrDefaultAsync();
        }
    }

}
