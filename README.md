A secure ASP.NET Core 8 MVC application demonstrating Google OAuth 2.0 authentication, custom login/logout handling, session management, and a modern responsive UI using Bootstrap 5.

🚀 Features
🔐 Google OAuth 2.0 Login Integration
👤 User Session and Profile Display
📋 User Login History Tracking
💾 SQL Server Integration for User Details
🧭 Custom Middleware for Global Error Handling
📱 Fully Responsive Bootstrap 5 Layout
🚫 Redirect to Login when unauthorized

Add package :

Install-Package Microsoft.AspNetCore.Authentication.Google
Install-Package Microsoft.EntityFrameworkCore
Install-Package Microsoft.EntityFrameworkCore.Tools
Install-Package Microsoft.EntityFrameworkCore.SqlServer



Go to Google Cloud Console

👉 https://console.cloud.google.com/apis/credentials

Find your OAuth 2.0 Client ID (the same one you used for your app).


⚙️ Configuration
appsettings.json

Add your Google credentials:

"Authentication": {
  "Google": {
    "ClientId": "769681577729-44b4b898r3gfgquuh3c87b1emusnh8cr.apps.googleusercontent.com",
    "ClientSecret": "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"
  }
}


🧠 Program.cs Configuration


 Google Authentication setup
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
    options.ClientId = config["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = config["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;

    // Save Google user info to DB
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
                existingUser.LoginTime = DateTime.Now;
            }

            // Add login history record
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


| Tool / Service                  | Purpose                                        |
| ------------------------------- | ---------------------------------------------- |
| .NET 8 SDK                       | Core framework                                 |
| ASP.NET Core MVC                 | Web app architecture                           |
| Google Cloud Console (OAuth 2.0) | Authentication provider                        |
| Bootstrap 5                      | Responsive front-end UI                        |
| Entity Framework Core / ADO.NET  | Database operations                            |
| Render / Azure / Railway         | Free hosting with HTTPS                        |
| ChatGPT (GPT-5)                 | Code generation, UI & documentation assistance |
| GitHub                          | Source control & deployment                    |



🧑‍💻 Author
Nitin Kumar
🚀 .NET Developer
📧 nitinkumar@example.com
