using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Services;
using VersopayDatabase.Data;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Blob
//var blobConn = builder.Configuration.GetConnectionString("BlobStorage")
//    ?? throw new InvalidOperationException("Faltou ConnectionStrings:BlobStorage no appsettings.");
//builder.Services.AddSingleton(new BlobServiceClient(blobConn));
//builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
