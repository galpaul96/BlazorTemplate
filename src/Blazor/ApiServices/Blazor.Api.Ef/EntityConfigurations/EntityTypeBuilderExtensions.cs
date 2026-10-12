using Blazor.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Blazor.Api.Ef.EntityConfigurations
{
    internal static class EntityTypeBuilderExtensions
    {
        public static void ConfigureAuditedEntity<TEntity>(this EntityTypeBuilder<TEntity> configuration, string tableName)
            where TEntity : Audit
        {
            configuration.ToTable(tableName);

            configuration.HasKey(o => o.Id);

            configuration.Property(o => o.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("uuid_generate_v4()");

            configuration.Property<Guid>("ExternalId")
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("uuid_generate_v4()");

            configuration.HasIndex("ExternalId")
                .IsUnique();

            configuration.Property(o => o.CreatedDate)
                .HasDefaultValueSql("now()");

            configuration.Property(o => o.UpdatedDate)
                .HasDefaultValueSql("now()");

            configuration.Property(o => o.IsDeleted)
                .HasDefaultValue(false);

            configuration.HasIndex(o => new { o.IsDeleted, o.UpdatedDate });
            configuration.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}
