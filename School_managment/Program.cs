using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using School_managment.Features.Classes.Models;
using School_managment.Features.Classes.Orchestrators;
using School_managment.Features.Teachers.Orchestrators;
using School_managment.Infrastructure;
using School_managment.Infrastructure.Interface;
using School_managment.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext with SQL Server connection string from appsettings.json
builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepository<>), typeof(GeneralRepository<>));

// سجل TeacherOrchestrator نفسه
builder.Services.AddScoped<IClassRepository<Class>, ClassRepository>();

builder.Services.AddScoped<TeacherOrchestrator>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(TeacherOrchestrator).Assembly);
});

builder.Services.AddScoped<ClassOrchestrator>();




builder.Services.AddControllers();

// Configure CORS policy to allow Angular app on localhost:4200
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Use CORS policy
app.UseCors("AllowAngularApp");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
