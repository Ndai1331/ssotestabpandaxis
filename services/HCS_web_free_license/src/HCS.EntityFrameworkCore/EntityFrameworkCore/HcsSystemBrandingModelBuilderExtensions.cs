using HCS.Branding;
using Microsoft.EntityFrameworkCore;

namespace HCS.EntityFrameworkCore;

public static class HcsSystemBrandingModelBuilderExtensions
{
    public static void ConfigureHcsSystemBranding(this ModelBuilder builder)
    {
        builder.Entity<SystemBrandingAsset>(b =>
        {
            b.ToTable("HcsSystemBrandingAssets");
            b.HasKey(x => x.Slot);
            b.Property(x => x.Slot).IsRequired().HasMaxLength(32);
            b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ContentType).IsRequired().HasMaxLength(128);
            b.Property(x => x.BlobName).IsRequired().HasMaxLength(512);
            b.Property(x => x.Size).IsRequired();
            b.Property(x => x.Sha256).IsRequired().HasMaxLength(64);
            b.Property(x => x.Revision).IsRequired();
            b.Property(x => x.CreationTime).IsRequired();
            b.Property(x => x.LastModificationTime).IsRequired();
            b.HasIndex(x => x.BlobName).IsUnique();
        });
    }
}
