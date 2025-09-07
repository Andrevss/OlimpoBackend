using OlimpoBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Adicionar servi�os
builder.Services.AddControllers();

// Configura��o de sess�o simplificada
builder.Services.AddDistributedMemoryCache(); // Adicionar esta linha
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Adicionar para CORS
});

// Registrar servi�os
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
        policy.WithOrigins("https://olimpo081.vercel.app", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configurar pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseSession(); // Certificar que est� antes de UseRouting
app.UseRouting();
app.MapControllers();

app.Run();