using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spendy.Components;
using Spendy.Data;
using Spendy.Data.Loaders;
using TrueLayer.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<LiteDBDatastore>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<AccountLoader>();
builder.Services.AddSingleton<CreditCardLoader>();
builder.Services.AddSingleton<TransactionLoader>();
builder.Services.AddSingleton<CreditCardTransactionLoader>();
builder.Services.AddSingleton<TrueLayerAuth>();
builder.Services.AddSingleton<TrueLayerAPI>();
builder.Services.AddSingleton<ProviderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
