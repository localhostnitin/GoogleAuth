using GoogleAuthentication.Data;
using GoogleAuthentication.Middleware;
using GoogleAuthentication.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Add services BEFORE building the app
builder.Services.AddControllersWithViews();

// Add EF Core
builder.Services.AddDbContext<ApplicationDbContext>(opts =>opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();

// Google Authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
    options.SlidingExpiration = true;
    options.LoginPath = "/Login/Login";
    options.LogoutPath = "/Home/Logout";
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    options.ClientId = config["Authentication:Google:ClientId"]??"";
    options.ClientSecret = config["Authentication:Google:ClientSecret"]??"";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    // Save Google user to DB on first login
    options.Events.OnCreatingTicket = async ctx =>
    {
        var db = ctx.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

        var email = ctx.Principal.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = ctx.Principal.FindFirstValue(ClaimTypes.Name)
               ?? ctx.Principal.FindFirstValue("name")
               ?? ctx.Principal.FindFirstValue("given_name") + " " +
                  ctx.Principal.FindFirstValue("family_name");
        var provider = ctx.Scheme.Name ?? "Google";
        var providerKey = ctx.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                          ctx.Principal.FindFirstValue("sub");

        if (string.IsNullOrEmpty(providerKey) || string.IsNullOrEmpty(email))
            return;

        if (!string.IsNullOrEmpty(email))
        {
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser == null)
            {
                db.Users.Add(new GoogleAuthentication.Models.ApplicationUser
                {
                    Name = name ?? email,
                    Email = email,
                    Provider = provider,
                    CreatedOn = DateTime.Now,
                    ProviderKey = providerKey,
                    LoginTime = DateTime.Now
                });
            }
            else
            {
                // Optional: update last login time
                existingUser.LoginTime = DateTime.Now;
            }
            // Add login record
            db.LoginHistories.Add(new LoginHistory
            {
                UserEmail = email,
                Provider = provider,
                ActionType = "Login",
                IpAddress = ctx.HttpContext.Connection.RemoteIpAddress?.ToString(),
                ActionTime = DateTime.Now
            });
            await db.SaveChangesAsync();
        }
    };
});

builder.Services.AddAuthorization();

//Build the app only after all services are registered
var app = builder.Build();

//Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
//// Use custom error handler before other middleware
//app.UseMiddleware<ErrorHandlingMiddleware>();

//// handle 404 (page not found)
//app.UseStatusCodePages(async context =>
//{
//    if (context.HttpContext.Response.StatusCode == 404)
//    {
//        context.HttpContext.Response.ContentType = "text/html";
//        await context.HttpContext.Response.WriteAsync(@"
//            <html><body style='font-family:Arial;text-align:center;padding-top:100px;'>
//            <h2>404 - Page Not Found</h2>
//            <p>The page you are looking for might have been removed or is temporarily unavailable.</p>
//            <a href='/'>Go Back Home</a>
//            </body></html>");
//    }
//});
app.UseRouting();
app.UseSession();

app.UseAuthentication();

app.UseAuthorization();
// Optional global cache-prevent middleware (also use ResponseCache attribute on controllers)
app.Use(async (context, next) =>
{
    // always add no-cache headers (useful), browser may still show BF cached copy in some cases
    context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Login}/{id?}");
app.Run();
