using System.Net;

namespace GoogleAuthentication.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Continue pipeline
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred."); // Log detailed error

                await HandleExceptionAsync(context);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "text/html";

            // Minimal safe error message to user
            string errorPage = @"
                <html>
                <head>
                    <title>Error</title>
                    <style>
                        body { font-family: Arial; background-color: #f4f4f4; color: #333; text-align: center; padding-top: 100px; }
                        .error-box { background: white; display: inline-block; padding: 30px; border-radius: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.1); }
                        h1 { color: #dc3545; }
                        a { color: #007bff; text-decoration: none; }
                        a:hover { text-decoration: underline; }
                    </style>
                </head>
                <body>
                    <div class='error-box'>
                        <h1>Something went wrong</h1>
                        <p>We’re sorry, but an unexpected error occurred.</p>
                        <p><a href='/'>Return to Home</a></p>
                    </div>
                </body>
                </html>";

            await context.Response.WriteAsync(errorPage);
        }
    }
}
