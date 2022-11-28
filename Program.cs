using GastonAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json.Serialization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*
 * El builder permite agregar servicios
 * Contexto de la base de datos es la clase en el explorador de soluciones
 * UseSqlServer porque se utiliza SqlServer como manejador, tendría que revisarse la documentación
 */
builder.Services.AddDbContext<GastonDbContext>(obj => obj.UseSqlServer(builder.Configuration.GetConnectionString("cadenaSQL")));

/*
 * Ignorar bucles dentro de los objetos JSON que pueden hacer referencia a objetos que ya 
 * pertenecen a ellos mismos
 */
builder.Services.AddControllers().AddJsonOptions(opt =>
{ 
    opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; 
});

/*
 * Esquema de autenticación con Jwt
 */
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(Optional =>
{
    Optional.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

var CorsRules = "CorsRules";

builder.Services.AddCors(opt =>
{
    opt.AddPolicy(name: CorsRules, builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsRules);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
