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
            .HasOne(u => u.ApplicationUser)
            .WithMany(s => s.SupportMessages)
            .HasForeignKey(u => u.ApplicationUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}