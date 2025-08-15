using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Services;
using VersopayDatabase.Data;

var builder = WebApplication.CreateBuilder(args);

// DB
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

// Blob (usa ConnectionString com Account Key)
var blobConn = builder.Configuration.GetConnectionString("BlobStorage");
builder.Services.AddSingleton(new BlobServiceClient(blobConn));
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
