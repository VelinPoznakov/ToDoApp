using System.ComponentModel.DataAnnotations;
using TodoApp.Models.Todo;

namespace TodoApp.Services.Tests.Todos;

public class CreateEditViewModelTests
{
    private static List<ValidationResult> Validate(DateOnly dueDate)
    {
        var model = new CreateEditViewModel { DueDate = dueDate };

        return model.Validate(new ValidationContext(model)).ToList();
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    [Fact]
    public void Validate_PastDueDate_IsRejectedOnTheDueDateField()
    {
        ValidationResult error = Assert.Single(Validate(Today.AddDays(-1)));

        Assert.Contains(nameof(CreateEditViewModel.DueDate), error.MemberNames);
    }

    [Fact]
    public void Validate_TodayAndFutureDueDates_AreAccepted()
    {
        Assert.Empty(Validate(Today));
        Assert.Empty(Validate(Today.AddDays(1)));
    }
}
