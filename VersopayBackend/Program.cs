using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VersopayBackend.Auth;
using VersopayBackend.Common;
using VersopayBackend.Options;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.NovaSenha;
using VersopayBackend.Repositories.Webhook;
using VersopayBackend.Services;
using VersopayBackend.Services.Auth;
using VersopayBackend.Services.Email;
using VersopayBackend.Services.KycKyb;
using VersopayBackend.Services.KycKybFeature;
using VersopayBackend.Services.Webhooks;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Faltou a seção Jwt no appsettings.");

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<BrandSettings>(builder.Configuration.GetSection("Brand"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// CORS (dev)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsDev", p =>
        p.WithOrigins(
            "http://localhost:4200",
            "https://localhost:4200",
            "http://127.0.0.1:4200",
            "https://127.0.0.1:4200"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials() // << necessário para cookies cross-site
    );
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<BrandSettings>(builder.Configuration.GetSection("Brand"));

// DI (removi duplicata de IUsuarioRepository)
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
builder.Services.AddScoped<IDocumentosService, DocumentosService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidosService, PedidosService>();
builder.Services.AddScoped<INovaSenhaRepository, NovaSenhaRepository>();
builder.Services.AddScoped<IUsuarioSenhaHistoricoRepository, UsuarioSenhaHistoricoRepository>();

builder.Services.AddScoped<IKycKybRepository, KycKybRepository>();
builder.Services.AddScoped<IKycKybService, KycKybService>();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddSingleton<IEmailEnvioService, EmailEnvioService>();

builder.Services.AddScoped<IBypassTokenRepository, BypassTokenRepository>();
builder.Services.AddScoped<IDeviceTrustChallengeRepository, DeviceTrustChallengeRepository>();

builder.Services.AddScoped<IAntecipacaoRepository, AntecipacaoRepository>();
builder.Services.AddScoped<IAntecipacoesService, AntecipacoesService>();

builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhooksService, WebhooksService>();

// Swagger + Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Versopay API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT no header Authorization (Bearer {token})",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// (1) Roteamento explícito ajuda a garantir ordem do middleware com endpoint routing
app.UseRouting();

// (2) CORS ANTES de Auth/Authorization
app.UseCors("CorsDev");

app.UseAuthentication();
app.UseAuthorization();

// (3) Anexe a policy aos endpoints (garante CORS nos pré-flights/404 etc.)
app.MapControllers().RequireCors("CorsDev");

app.Run();
