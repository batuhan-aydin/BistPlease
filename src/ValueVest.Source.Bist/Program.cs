using Microsoft.AspNetCore.Mvc;
using ValueVest.Source.Bist.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IIsInvestmentService, IsInvestmentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/valuations", ([FromServices] IIsInvestmentService service) =>
{
	var result = service.GetValuations();
	return result;
})
.WithName("Valuations")
.WithOpenApi();

app.Run();
