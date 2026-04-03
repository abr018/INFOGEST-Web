using INFOGEST_Web.Data;
using INFOGEST_Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================
// 1. CONFIGURAÇÃO DA BASE DE DADOS
// ==========================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// NOTA: Confirma se a tua classe se chama 'InfogestDbContext' ou 'ApplicationDbContext'
builder.Services.AddDbContext<InfogestDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==========================================================
// 2. SERVIÇOS DE SESSÃO
// ==========================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ==========================================================
// 3. CONTROLLERS E JSON (SOLUÇÃO DO PROBLEMA DE DADOS)
// ==========================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Esta linha é a MÁGICA que resolve o erro "Cycle detected"
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddRazorPages();

// Registar PasswordHasher (coloca junto dos outros builder.Services...)
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<INFOGEST_Web.Models.Funcionario>, Microsoft.AspNetCore.Identity.PasswordHasher<INFOGEST_Web.Models.Funcionario>>();

var app = builder.Build();

// ==========================================================
// 4. MIDDLEWARE
// ==========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- ORDEM IMPORTANTE ---
app.UseSession();        // 1. Carrega a Sessão
app.UseAuthentication(); // 2. Identifica quem é o user (se usares Identity no futuro)
app.UseAuthorization();  // 3. Verifica permissões

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";

    if (path.StartsWith("/api/"))
    {
        await next();
        return;
    }

    // Obtemos o cargo tal como foi gravado no AuthController
    var cargo = context.Session.GetString("cargo");
    var username = context.Session.GetString("username");

    // 1. Caminhos que NUNCA são bloqueados (Página de login, API de login, ficheiros estáticos)
    if (path == "/login" || path.StartsWith("/api/login") || path.Contains("."))
    {
        await next();
        return;
    }

    // 2. BLOQUEIOS INDIVIDUAIS POR CARGO
    // Usamos .Equals com OrdinalIgnoreCase para evitar problemas de Maiúsculas/Minúsculas

    // --- Restrições para FUNCIONÁRIO ---
    if (cargo != null && cargo.Equals("Funcionario", StringComparison.OrdinalIgnoreCase))
    {
        if (path.Contains("homeadmin") || path.Contains("homegerentes") ||
            path.Contains("homesupervisores") || path.Contains("gerir") ||
            path.Contains("criar") || path.EndsWith("departamento") ||
            path.Contains("painelcontrolo"))
        {
            context.Response.Redirect("/HomeTrabalhadores");
            return;
        }
    }

    // --- Restrições para SUPERVISOR ---
    if (cargo != null && cargo.Equals("Supervisor", StringComparison.OrdinalIgnoreCase))
    {
        if (path.Contains("homeadmin") || path.Contains("homegerentes") ||
            path.Contains("hometrabalhadores") || path.EndsWith("departamento") ||
            path.Contains("criar") || path.Contains("criar"))
        {
            context.Response.Redirect("/HomeSupervisores");
            return;
        }
    }

    // --- Restrições para GERENTE ---
    if (cargo != null && cargo.Equals("Gerente", StringComparison.OrdinalIgnoreCase))
    {
        if (path.Contains("homeadmin") || path.Contains("hometrabalhadores") ||
            path.Contains("homesupervisores") || path.Contains("painelcontrolo") ||
            path.EndsWith("departamento") || path.Contains("criar") || 
            path.Contains("gerirdepartamentos") || path.Contains("gerirfuncionarios") || 
            path.Contains("gerirfiliais")
            ) 
        {
            context.Response.Redirect("/HomeGerentes");
            return;
        }
    }

    // --- Restrições para ADMINISTRADOR ---
    if (cargo != null && cargo.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
    {
        if (path.Contains("homegerentes") || path.Contains("homesupervisores") ||
            path.Contains("hometrabalhadores") || path.Contains("painelcontrolo") ||
            path.EndsWith("departamento") || path.Contains("criar") ||
            path.Contains("checkinout"))
        {
            context.Response.Redirect("/HomeAdmin");
            return;
        }
    }

    await next();
});

// Redirecionar raiz para Login
app.MapGet("/", context =>
{
    context.Response.Redirect("/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();
app.MapControllers();

string wwwroot = app.Environment.WebRootPath;
Rotativa.AspNetCore.RotativaConfiguration.Setup(wwwroot, "rotativa");

app.Run();