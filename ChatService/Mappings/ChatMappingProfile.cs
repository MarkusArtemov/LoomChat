using AutoMapper;
using De.Hsfl.LoomChat.Chat.Models;
using De.Hsfl.LoomChat.Chat.Dtos.Responses;

namespace De.Hsfl.LoomChat.Chat.Mappings
{
    /// <summary>
    /// Defines mapping from entities to response records
    /// </summary>
    public class ChatMappingProfile : Profile
    {
        public ChatMappingProfile()
        {
            // Simple entity-to-response maps
            CreateMap<Channel, ChannelResponse>();
            CreateMap<ChatMessage, ChatMessageResponse>();
            CreateMap<ChannelMember, ChannelMemberResponse>();

            // For channel details, we also map collections
            CreateMap<Channel, ChannelDetailsResponse>()
                .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.ChannelMembers))
                .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.ChatMessages));
        }
    }
}
