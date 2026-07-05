using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Models.Data;

namespace TodoApp.Data.Configurations;

public class GroupConfiguration: IEntityTypeConfiguration<Group>
{
    private readonly Group[] groups =
    {
        new Group
        {
            Id = Guid.Parse("67938441-4f26-429a-a328-828208f6f0e6"),
            Name = "Work Tasks",
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            CreatedOn = new DateTime(2026, 5, 22, 8, 30, 0)
        },
        new Group
        {
            Id = Guid.Parse("eb68a07e-7fc9-40e8-bdd4-ce5cd056fb51"),
            Name = "University",
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            CreatedOn = new DateTime(2026, 5, 21, 11, 15, 0)
        },
        new Group
        {
            Id = Guid.Parse("9ee30a53-b22e-40af-9171-077da6d5d34a"),
            Name = "Gaming",
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            CreatedOn = new DateTime(2026, 5, 20, 16, 45, 0)
        },
        new Group
        {
            Id = Guid.Parse("b4c7c90c-2094-4b50-b3a0-3b123a1583f6"),
            Name = "Personal",
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            CreatedOn = new DateTime(2026, 5, 19, 9, 0, 0)
        },
        new Group
        {
            Id = Guid.Parse("a1a1ab13-d1bc-4a25-8ba7-ca141db73f85"),
            Name = "Fitness",
            UserId = Guid.Parse("07420943-635f-4cd8-8850-e0ce781094f2"),
            CreatedOn = new DateTime(2026, 5, 18, 7, 20, 0)
        }
    };

    public void Configure(EntityTypeBuilder<Group> entity)
    {
        entity.HasData(groups);
    }
}