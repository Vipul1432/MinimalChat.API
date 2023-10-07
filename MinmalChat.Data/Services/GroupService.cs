using Microsoft.EntityFrameworkCore;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Interfaces;
using MinimalChat.Domain.Models;
using MinmalChat.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinmalChat.Data.Services
{
    public class GroupService : IGroupService
    {
        private readonly Repository<Group> _groupRepository;

        public GroupService(Repository<Group> groupRepository)
        {
            _groupRepository = groupRepository;
        }

        public async Task<Group> CreateGroupAsync(GroupDto groupDto)
        {
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = groupDto.Name,
                Members = groupDto.Members
            };

            var addedGroup = await _groupRepository.AddAsync(group);

            return addedGroup;
        }
    }
}
