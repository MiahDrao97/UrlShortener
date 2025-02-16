using AutoMapper;
using UrlShortener.Backend.Data.Entities;
using UrlShortener.Backend.Models;

namespace UrlShortener.Backend;

public sealed class ShortenedUrlProfile : Profile
{
    public ShortenedUrlProfile()
    {
        CreateMap<ShortenedUrl, ShortenedUrlModel>()
            .ForMember(x => x.Alias, opt => opt.MapFrom(src => $"{src.Alias}{src.Offset}"))
            .ForMember(x => x.FullUrl, opt => opt.MapFrom(src => src.FullUrl))
            .ForMember(x => x.Created, opt => opt.MapFrom(src => src.Created));
    }
}
