using AutoMapper;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend;

/// <summary>
/// Auto-mappering profile for <see cref="ShortenedUrl"/>
/// </summary>
public sealed class ShortenedUrlProfile : Profile
{
    public ShortenedUrlProfile()
    {
        CreateMap<ShortenedUrl, ShortenedUrlOuput>()
            .ForMember(static x => x.Alias, static opt => opt.MapFrom(static src => src.UrlSafeAlias))
            .ForMember(static x => x.FullUrl, static opt => opt.MapFrom(static src => src.FullUrl))
            .ForMember(static x => x.Created, static opt => opt.MapFrom(static src => src.Created));
    }
}
