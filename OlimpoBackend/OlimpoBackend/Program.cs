using OlimpoBackend.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Adicionar serviços
builder.Services.AddControllers();

// Configuração de sessão simplificada
builder.Services.AddDistributedMemoryCache(); // Adicionar esta linha
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Adicionar para CORS
});

// Registrar serviços
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<ICorreiosService, CorreiosService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// HTTP Clients
builder.Services.AddHttpClient<MercadoPagoService>();
builder.Services.AddHttpClient<CorreiosService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://olimpo081.vercel.app",
            "http://localhost:3000"  // sua porta
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });


var app = builder.Build();



// Configurar pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseSession(); // Certificar que está antes de UseRouting
app.UseRouting();
app.MapControllers();

app.Run();