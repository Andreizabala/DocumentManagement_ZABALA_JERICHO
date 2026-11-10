using DocumentManagement.Api.Middleware;
using DocumentManagement.Application.Services;
using DocumentManagement.Infrastructure;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<DocumentService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = DocumentService.MaximumFileSize);
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = DocumentService.MaximumFileSize + (1024 * 1024));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program;

