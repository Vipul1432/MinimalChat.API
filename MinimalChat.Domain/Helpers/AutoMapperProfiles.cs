using AutoMapper;
using Microsoft.AspNetCore.Http;
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
            CreateMap<MinimalChatUser, UserDto>().ReverseMap()
            .ForMember(dest => dest.Email, opt => opt.NullSubstitute(null));
            CreateMap<Group, MinimalChatUser>().ReverseMap()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));
            CreateMap<IFormFile, FileUploadDto>()
            .ForMember(dest => dest.ReceiverId, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.GroupId, opt => opt.Ignore())
            .ForMember(dest => dest.SenderId, opt => opt.Ignore())
            .ForMember(dest => dest.FileData, opt => opt.MapFrom(src => src.OpenReadStream()))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.UploadDirectory, opt => opt.Ignore());
            CreateMap<ResponseMessageDto, Message>()
            .ForMember(dest => dest.Sender, opt => opt.Ignore())
            .ForMember(dest => dest.Receiver, opt => opt.Ignore())
            .ForMember(dest => dest.Group, opt => opt.Ignore()).ReverseMap();
            CreateMap<ResponseGroupDto, Group>()
            .ForMember(dest => dest.Members, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore())
            .ReverseMap();
            CreateMap<ResponseMessageDto, GetMessagesDto>()
            .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderId)) 
            .ReverseMap();
        }
    }
}
