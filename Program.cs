using System.Reflection;
using JiraDashboard;
using JiraDashboard.Data;
using JiraDashboard.interfaces;
using JiraDashboard.Repository;
using JiraDashboard.Services;
using Microsoft.EntityFrameworkCore;
using IOverviewService = JiraDashboard.IOverviewService;

var  MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("EnableCors", corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader().WithExposedHeaders("*")
            .Build();
    });
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("JiraDumpConnection")));

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IJiraService, JiraService>();
builder.Services.AddScoped<IBugService, BugService>();
builder.Services.AddScoped<IOverviewService, OverviewService>();
builder.Services.AddScoped<IGanttService, GanttService>();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(MyAllowSpecificOrigins);

app.UseHttpsRedirection();
app.UseCors("EnableCors");
app.UseAuthorization();

app.MapControllers();

app.Run();

