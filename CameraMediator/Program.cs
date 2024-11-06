using Serilog;
using Worker;



public class Program
{
	public static void Main(string[] args)
	{

		
		// Cấu hình Serilog
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
			.CreateLogger();

		try
		{
			Log.Information("Starting up the application...");
			MqttServer.Run_Minimal_Server().Wait();
			CreateHostBuilder(args).Build().Run();
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
			.UseSerilog() // Sử dụng Serilog cho logging
			.ConfigureServices((hostContext, services) =>
			{
				// Cấu hình các dịch vụ tại đây
			});
}
