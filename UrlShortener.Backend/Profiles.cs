using System.Text;
using AutoMapper;
using UrlShortener.Backend.Data;
using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend;

public sealed class ShortenedUrlProfile : Profile
{
    public ShortenedUrlProfile()
    {
        CreateMap<ShortenedUrl, ShortenedUrlOuput>()
            .ForMember(static x => x.Alias, static opt => opt.MapFrom(static src =>
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{src.Alias}{src.Offset}")).Base64ToUrlSafe()))
            .ForMember(static x => x.FullUrl, static opt => opt.MapFrom(static src => src.FullUrl))
            .ForMember(static x => x.Created, static opt => opt.MapFrom(static src => src.Created));
    }
}
