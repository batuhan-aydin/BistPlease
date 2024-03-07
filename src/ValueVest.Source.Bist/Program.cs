using AngleSharp;
using Microsoft.AspNetCore.Mvc;
using ValueVest.Source.Bist.Models;
using ValueVest.Source.Bist.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<IsInvestmentSettings>(builder.Configuration.GetSection(nameof(IsInvestmentSettings)));
builder.Services.Configure<ExhangesApiSettings>(builder.Configuration.GetSection(nameof(ExhangesApiSettings)));
builder.Services.AddScoped<IIsInvestmentService, IsInvestmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/companies", ([FromServices] IIsInvestmentService service) =>
{
	var result = service.GetCompanyValuations();
	return result;
})
.WithName("Companies")
.WithOpenApi();

app.Run();
