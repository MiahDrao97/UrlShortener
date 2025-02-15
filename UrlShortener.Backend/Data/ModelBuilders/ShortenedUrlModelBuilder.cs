using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Backend.Data.Entities;

namespace UrlShortener.Backend.Data.ModelBuilders;

/// <summary>
/// Builds out the table definition of <see cref="ShortenedUrl"/>
/// </summary>
public sealed class ShortenedUrlModelBuilder : IEntityTypeConfiguration<ShortenedUrl>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShortenedUrl> builder)
    {
        builder.HasKey(static x => x.RowId);
        builder.HasIndex(static x => x.Alias); // non-unique; handle collision edge cases
    }
}
