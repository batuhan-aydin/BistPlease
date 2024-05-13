using Microsoft.AspNetCore.Mvc;
using ValueVest.Domain;
using ValueVest.Source.Bist.Core;
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
builder.Services.AddScoped<IWebParser, WebParser>();
builder.Services.AddScoped<IIsInvestmentService, IsInvestmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/companies", async ([FromServices] IIsInvestmentService service) =>
{
	var result = await service.GetCompanies();
	return result;
})
.WithName("Companies")
.WithOpenApi();

app.MapGet("/company/{symbol}", async ([FromServices] IIsInvestmentService service, string symbol,
[FromQuery] string currency) =>
{
	var currencyValue = currency == "USD" ? Currency.USD : Currency.TRY;
	var result = await service.GetCompany(symbol, currencyValue);
	return result.IsError ? throw new InvalidOperationException(result.FirstError.ToString()) 
	: CommonFunctions.Serialize(result.Value);
})
.WithName("Company")
.WithOpenApi();

app.Run();
