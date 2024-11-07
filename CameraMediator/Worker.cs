using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MQTTnet.Protocol;
using MQTTnet;
using MQTTnet.Diagnostics;
using MQTTnet.Client;
using Serilog;
using System.Text.Json;
using CameraMediator;
using Microsoft.Extensions.Options;

namespace Worker;



internal class MqttServer
{

	private readonly MqttSettings _mqttSettings;

	public MqttServer(IOptions<MqttSettings> mqttSettings)
	{
		_mqttSettings = mqttSettings.Value;
	}
	public class CameraMessage
	{
		public string? person_id { get; set; }
		public string? person_name { get; set; }
		public string? time { get; set; }
		public string? camera_id { get; set; }
	}

	public static CameraMessage ParseCameraMessage(string jsonPayload)
	{
		// Thiết lập tùy chọn để ánh xạ tên thuộc tính không phân biệt hoa thường
		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		// Deserialize JSON vào đối tượng CameraMessage
		CameraMessage cameraMessage = JsonSerializer.Deserialize<CameraMessage>(jsonPayload, options);

		return cameraMessage;
	}

	public async Task Run_Minimal_Server()
	{
		/*
         * This sample starts a simple MQTT server which will accept any TCP connection.
         */

		var mqttServerFactory = new MqttFactory(new ConsoleLogger());

		// The port for the default endpoint is 1883.
		// The default endpoint is NOT encrypted!
		// Use the builder classes where possible.
		var mqttServerOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint()
			.WithDefaultEndpointPort(_mqttSettings.Port).Build();

		// The port can be changed using the following API (not used in this example).
		// new MqttServerOptionsBuilder()
		//     .WithDefaultEndpoint()
		//     .WithDefaultEndpointPort(1234)
		//     .Build();



		using (var mqttServer = mqttServerFactory.CreateMqttServer(mqttServerOptions))

		{

			mqttServer.ValidatingConnectionAsync += async context =>
			{
				// Kiểm tra username và password
				if (context.UserName != _mqttSettings.Username || context.Password != _mqttSettings.Password)
				{
					context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
					Console.WriteLine($"Client {context.ClientId} isn't accepted.");
					Log.Information($"Authentication: Client {context.ClientId} isn't accepted.");
				}
				else
				{
					Console.WriteLine($"Client {context.ClientId} is accepted.");
					Log.Information($"Authentication: Client {context.ClientId} is accepted.");
				}




				await Task.CompletedTask;
			};



			await mqttServer.StartAsync();
			Console.WriteLine("MQTT server started with authentication. Press Enter to stop.");



			mqttServer.InterceptingPublishAsync += async context =>
			{
				Console.OutputEncoding = System.Text.Encoding.UTF8;


	


				//Console.WriteLine($"Received on topic '{context.ApplicationMessage.Topic}': {Encoding.UTF8.GetString(context.ApplicationMessage.Payload)}");
				Console.WriteLine($"Received on topic '{context.ApplicationMessage.Topic}': {Encoding.UTF8.GetString(context.ApplicationMessage.PayloadSegment.ToArray())}");
				Log.Information($"Package: '{context.ApplicationMessage.Topic}': {Encoding.UTF8.GetString(context.ApplicationMessage.PayloadSegment.ToArray())}");
				// Thay đổi nội dung của tin nhắn (nếu cần)
				//context.ApplicationMessage.Payload = Encoding.UTF8.GetBytes("Modified payload");

				// Kiểm soát việc gửi tin nhắn đi tiếp
				//context.ProcessPublish = true; // Nếu false, tin nhắn sẽ không được gửi đến các subscriber

				if (context.ApplicationMessage.Topic == "/topic/online")
				{
					// Dừng hàm
					return;
				}


				using var httpClient = new HttpClient();

				// Chuyển đổi payload của tin nhắn từ byte[] sang string JSON
				//var jsonPayload = Encoding.UTF8.GetString(context.ApplicationMessage.Payload);
				var jsonPayload = Encoding.UTF8.GetString(context.ApplicationMessage.PayloadSegment.ToArray());
				CameraMessage message = ParseCameraMessage(jsonPayload);

				

				Console.OutputEncoding = System.Text.Encoding.UTF8;
				Console.WriteLine($"Person ID: {message.person_id}");
				Console.WriteLine($"Person Name: {message.person_name}");
				Console.WriteLine($"Time: {message.time}");
				Console.WriteLine($"Camera ID: {message.camera_id}");


				//Console.OutputEncoding = System.Text.Encoding.UTF8;


				var options = new JsonSerializerOptions
				{
					WriteIndented = true, // Để có định dạng JSON dễ đọc
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Tránh escape ký tự
				};
				string jsonPayloadNew = JsonSerializer.Serialize(message, options);

				Console.WriteLine($"Serialized JSON: {jsonPayloadNew}");
				// Tạo nội dung JSON cho request

				var content = new StringContent(jsonPayloadNew, Encoding.UTF8, "application/json");
				string contentString = await content.ReadAsStringAsync();
				Console.WriteLine($"SEND '{contentString}");


				// URL của endpoint API
				var apiUrl = _mqttSettings.ApiUrl; // Thay đổi thành URL thực tế của bạn

				try
				{
					// Gửi POST request đến API
					var response = await httpClient.PostAsync(apiUrl, content);
					var responseText = await response.Content.ReadAsStringAsync();

					Console.WriteLine($"response: {responseText}.");

					// Kiểm tra nếu request thành công
					if (response.IsSuccessStatusCode)
					{
						Console.WriteLine("Message successfully sent to API.");
						Log.Information("State: Message successfully sent to API.");
					}
					else
					{
						Console.WriteLine($"Failed to send message to API. Status Code: {response.StatusCode}");
						Log.Information($"State: Failed to send message to API. Status Code: {response.StatusCode}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error while sending message to API: {ex.Message}");
					Log.Information($"State: Error while sending message to API: {ex.Message}");
				}





				await Task.CompletedTask; // Đảm bảo phương thức bất đồng bộ hoàn thành
			};


			Console.ReadLine();

			// Stop and dispose the MQTT server if it is no longer needed!
			//await mqttServer.StopAsync();

		}
	}



}

class ConsoleLogger : IMqttNetLogger
{
	readonly object _consoleSyncRoot = new();

	public bool IsEnabled => true;

	public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
	{
		var foregroundColor = ConsoleColor.White;
		switch (logLevel)
		{
			case MqttNetLogLevel.Verbose:
				foregroundColor = ConsoleColor.White;
				break;

			case MqttNetLogLevel.Info:
				foregroundColor = ConsoleColor.Green;
				break;

			case MqttNetLogLevel.Warning:
				foregroundColor = ConsoleColor.DarkYellow;
				break;

			case MqttNetLogLevel.Error:
				foregroundColor = ConsoleColor.Red;
				break;
		}

		if (parameters?.Length > 0)
		{
			message = string.Format(message, parameters);
		}

		lock (_consoleSyncRoot)
		{
			Console.ForegroundColor = foregroundColor;
			Console.WriteLine(message);

			if (exception != null)
			{
				Console.WriteLine(exception);
			}
		}
	}



}

