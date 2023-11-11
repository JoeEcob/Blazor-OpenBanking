using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spendy.Data;
using Spendy.Data.Loaders;
using TrueLayer.API;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
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

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
