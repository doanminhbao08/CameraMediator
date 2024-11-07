using CameraMediator;
using Serilog;
using Worker;

public class Program
{
	public static async Task Main(string[] args) // Đảm bảo Main là async và trả về Task
	{
		// Cấu hình Serilog
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
			.CreateLogger();

		try
		{
			Log.Information("Starting up the application...");

			var host = CreateHostBuilder(args).Build();

			// Lấy MqttServer từ DI container
			var mqttServer = host.Services.GetRequiredService<MqttServer>();

			// Gọi phương thức Run_Minimal_Server() từ instance của MqttServer
			await mqttServer.Run_Minimal_Server(); // Dùng await trong async method

			await host.RunAsync();
		}
		catch (Exception ex)
		{
			Log.Fatal(ex, "Application start-up failed");
		}
		finally
		{
			Log.CloseAndFlush();
		}
	}

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) =>
			{
				config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
			})
			.UseSerilog()
			.ConfigureServices((hostContext, services) =>
			{
				services.Configure<MqttSettings>(hostContext.Configuration.GetSection("MqttSettings"));
				services.AddSingleton<MqttServer>();
			});
}
