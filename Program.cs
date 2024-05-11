using System;
using SELENE_STUDIO.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using SELENE_STUDIO.Helper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//DbContext for Identity
builder.Services.AddDbContext<LogAppDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    );

builder.Services.AddIdentity<RegUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;

    // Lockout settings
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Lockout duration
    options.Lockout.MaxFailedAccessAttempts = 5; // Max number of failed login attempts before lockout
}).AddEntityFrameworkStores<LogAppDbContext>().AddDefaultTokenProviders();

// Configure email sender using Gmail SMTP
builder.Services.AddSingleton<IEmailSender, GmailEmailSender>();

builder.Services.AddScoped<ITokenRepository, TokenRepository>();

builder.Services.AddScoped<OrderCancellationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/User/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseStaticFiles();



app.UseAuthentication();

var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<LogAppDbContext>();
//context.Database.EnsureCreated();
context.Database.EnsureDeleted();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var orderCancellationService = services.GetRequiredService<OrderCancellationService>();
    await DataHelper.ManageDataAsync(scope.ServiceProvider);
    orderCancellationService.ExecuteScheduledTask();
}

app.Run();