using Microsoft.Extensions.Logging;
using ToDoApp.Maui.Services;
using ToDoApp.Maui.Services.Interfaces;
using ToDoApp.Maui.ViewModels;
using ToDoApp.Maui.Views;

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
		
// HttpClient + Service
		builder.Services.AddSingleton(new HttpClient());
		builder.Services.AddSingleton<ITodoApiService, TodoApiService>();

// ViewModel + Page
		builder.Services.AddTransient<TodosViewModel>();
		builder.Services.AddTransient<TodosPage>();



#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
