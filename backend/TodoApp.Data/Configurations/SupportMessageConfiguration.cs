using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Models.Data;

namespace TodoApp.Data.Configurations;

public class SupportMessageConfiguration: IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> entity)
    {
        entity.HasQueryFilter(s => s.IsHandled == false);
        
        entity
            .HasOne(s => s.ApplicationUser)
            .WithMany(u => u.SupportMessages)
            .HasForeignKey(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(s => s.HandledByAdminUser)
            .WithMany(a => a.HandledSupportMessages)
            .HasForeignKey(s => s.HandledByAdminUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}