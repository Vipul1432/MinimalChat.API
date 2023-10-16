using AutoMapper;
using Microsoft.VisualBasic;
using MinimalChat.Domain.DTOs;
using MinimalChat.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<MessageDto, Message>().ReverseMap();
            CreateMap<GroupMember, GroupMemberDto>().ReverseMap();
            CreateMap<Message, GetMessagesDto>().ReverseMap();
            CreateMap<MinimalChatUser, UserDto>().ReverseMap().ForMember(dest => dest.Email, opt => opt.NullSubstitute(null));
            CreateMap<Group, MinimalChatUser>().ReverseMap().ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
                                               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
        }
    }
}
