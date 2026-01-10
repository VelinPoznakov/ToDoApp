using Microsoft.Extensions.Logging;
using ToDoApp.Maui.Services;
using ToDoApp.Maui.Services.Interfaces;
using ToDoApp.Maui.ViewModels;

namespace ToDoApp.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
		
		builder.Services.AddSingleton<ITodoApiService, TodoApiService>();
		builder.Services.AddTransient<TodosViewModel>();


#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
