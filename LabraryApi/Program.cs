using LabraryApi.DataSchemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("MyAllowAnyOriginPolicy",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var databaseName = Environment.GetEnvironmentVariable("MONGO_DB");
    var host = Environment.GetEnvironmentVariable("MONGO_HOST");
    var port = Environment.GetEnvironmentVariable("MONGO_PORT");
    var connectionString = $"{host}:{port}";
    var client = new MongoClient(connectionString);
    return client.GetDatabase(databaseName);
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseCors("MyAllowAnyOriginPolicy");

app.Run();
