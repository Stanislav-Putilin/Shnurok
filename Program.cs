using shnurok.Services.CosmosDb;
using shnurok.Services.Hash;
using shnurok.Services.Kdf;

namespace shnurok
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			builder.Configuration.AddJsonFile("azuresettings.json");

			builder.Services.AddSingleton<IContainerProvider, NoSqlContainerProvider>();
			builder.Services.AddSingleton<IHashService, Sha1HashService>();
			builder.Services.AddSingleton<IKdfService, Pbkdf1Service>();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();

			app.MapControllers();
			app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

			app.Run();
		}
	}
}