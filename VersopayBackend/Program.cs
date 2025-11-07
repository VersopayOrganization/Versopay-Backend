using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using VersopayBackend.Auth;
using VersopayBackend.Common;
using VersopayBackend.Dtos.Common;
using VersopayBackend.Options;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.NovaSenha;
using VersopayBackend.Repositories.Vexy;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayBackend.Services;
using VersopayBackend.Services.Auth;
using VersopayBackend.Services.Email;
using VersopayBackend.Services.KycKyb;
using VersopayBackend.Services.KycKybFeature;
using VersopayBackend.Services.Taxas;
using VersopayBackend.Services.Vexy;
using VersopayBackend.Webhooks;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------
// DB
// ------------------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("VersopayDatabase"))
);

// ------------------------------
// Options (appsettings / env)
// ------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<BrandSettings>(builder.Configuration.GetSection("Brand"));
builder.Services.Configure<TaxasOptions>(builder.Configuration.GetSection("Taxas"));

// aceitar payloads grandes (webhooks)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50_000_000;
});

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Faltou a seção Jwt no appsettings.");

// ------------------------------
// AuthN / AuthZ (JWT)
// ------------------------------
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

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
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtRegisteredClaimNames.Sub
        };
    });

// NENHUMA fallback/global policy aqui
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ------------------------------
// Controllers / JSON
// ------------------------------
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// ------------------------------
// CORS (DEV)
// ------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsDev", p =>
        p.WithOrigins(
            "http://localhost:4200", "https://localhost:4200",
            "http://127.0.0.1:4200", "https://127.0.0.1:4200",
            "http://localhost:4000", "https://localhost:4000",
            "https://kind-stone-0967bd30f.3.azurestaticapps.net"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});

// ------------------------------
// HttpClient(s) Vexy
// ------------------------------
builder.Services.AddHttpClient("Vexy", http =>
{
    var baseUrl = builder.Configuration["Providers:Vexy:BaseUrl"]
                  ?? "https://api.sandbox.vexybank.com";
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpClient("VexyBank", http =>
{
    // Troque para a URL oficial da VexyBank em produção
    http.BaseAddress = new Uri(Environment.GetEnvironmentVariable("VEXYBANK_BASE_URL")
                               ?? "https://api.vexybank.com");
    http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // se precisar: ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
})
// timeouts razoáveis p/ chamadas bancárias
.AddPolicyHandler(Polly.Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30)));


// ------------------------------
// DI – Repositórios / Serviços
// ------------------------------
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
builder.Services.AddScoped<IDocumentosService, DocumentosService>();

builder.Services.AddScoped<IUsuariosService, UsuariosService>();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidoReadRepository, PedidoRepository>();
builder.Services.AddScoped<IPedidosService, PedidosService>();

builder.Services.AddScoped<INovaSenhaRepository, NovaSenhaRepository>();
builder.Services.AddScoped<IUsuarioSenhaHistoricoRepository, UsuarioSenhaHistoricoRepository>();

builder.Services.AddScoped<IKycKybRepository, KycKybRepository>();
builder.Services.AddScoped<IKycKybService, KycKybService>();

builder.Services.AddScoped<IBypassTokenRepository, BypassTokenRepository>();
builder.Services.AddScoped<IDeviceTrustChallengeRepository, DeviceTrustChallengeRepository>();

builder.Services.AddScoped<IAntecipacaoRepository, AntecipacaoRepository>();
builder.Services.AddScoped<IAntecipacoesService, AntecipacoesService>();

builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhooksService, WebhooksService>();

builder.Services.AddScoped<IInboundWebhookLogRepository, InboundWebhookLogRepository>();
builder.Services.AddScoped<IPedidoMatchRepository, PedidoMatchRepository>();
builder.Services.AddScoped<ITransferenciaMatchRepository, TransferenciaMatchRepository>();
builder.Services.AddScoped<IInboundWebhookService, InboundWebhookService>();

builder.Services.AddScoped<ITransferenciaRepository, TransferenciaRepository>();
builder.Services.AddScoped<ITransferenciasService, TransferenciasService>();

builder.Services.AddScoped<IExtratoRepository, ExtratoRepository>();
builder.Services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
builder.Services.AddScoped<IExtratoService, ExtratoService>();

builder.Services.AddScoped<IProviderCredentialRepository, ProviderCredentialRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioAutenticadoService, UsuarioAutenticadoService>();

// Vexy API(s)
builder.Services.AddScoped<IVexyClient, VexyClient>();
builder.Services.AddScoped<IVexyService, VexyService>();

builder.Services.AddScoped<IVexyBankClient, VexyBankClient>();
builder.Services.AddScoped<IVexyBankService, VexyBankService>();

builder.Services.AddScoped<IVexyBankPixInRepository, VexyBankPixInRepository>();

builder.Services.AddScoped<VexyVendorWebhookHandler>();
builder.Services.AddScoped<VersellVendorWebhookHandler>();

builder.Services.AddScoped<IVendorWebhookHandlerFactory, VendorWebhookHandlerFactory>();

// utilitários
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddSingleton<IEmailEnvioService, EmailEnvioService>();
builder.Services.AddSingleton<ITaxasProvider, TaxasConfigProvider>();

// ------------------------------
// Swagger + Bearer
// ------------------------------
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

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ------------------------------
// App
// ------------------------------
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("CorsDev");
}

app.UseAuthentication();   // <- sempre antes
app.UseAuthorization();    // <- de Authorization

// NÃO usar .RequireAuthorization() global
if (app.Environment.IsDevelopment())
    app.MapControllers().RequireCors("CorsDev");
else
    app.MapControllers();

app.Run();
