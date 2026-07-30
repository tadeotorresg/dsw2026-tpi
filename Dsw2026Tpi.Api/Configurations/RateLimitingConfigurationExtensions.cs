using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;

namespace Dsw2026Tpi.Api.Configurations;

public static class RateLimitingConfigurationExtensions
{
    public const string AdminLogin = "AdminLogin";
    public const string PatientLogin = "PatientLogin";
    public const string AppointmentBooking = "AppointmentBooking";

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfiguration = configuration.GetSection("RateLimiting");
        var windowMinutes = rateLimitConfiguration.GetValue<int>("WindowMinutes");
        var queueLimit = rateLimitConfiguration.GetValue<int>("QueueLimit");
        var adminLoginLimit = rateLimitConfiguration.GetValue<int>("AdminLoginPermitLimit");
        var patientLoginLimit = rateLimitConfiguration.GetValue<int>("PatientLoginPermitLimit");
        var appointmentLimit = rateLimitConfiguration.GetValue<int>("AppointmentPermitLimit");
        var generalLimit = rateLimitConfiguration.GetValue<int>("GeneralPermitLimit");

        var window = TimeSpan.FromMinutes(windowMinutes);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (rejectedContext, cancellationToken) =>
            {
                var httpContext = rejectedContext.HttpContext;
                var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("RateLimiting");

                logger.LogWarning("Solicitud rechazada por Rate Limiting. Path: {Path}, IP: {IpAddress}", httpContext.Request.Path, GetIp(httpContext));

                var error = new ErrorResponse(nameof(ErrorCodes.TOO_MANY_REQUESTS), ErrorCodes.TOO_MANY_REQUESTS);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserOrIp(context),
                    factory: _ => CreateOptions(generalLimit, window, queueLimit)));

            options.AddPolicy(AdminLogin, 
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetIp(context),
                    factory: _ => CreateOptions(adminLoginLimit, window, queueLimit)));

            options.AddPolicy(PatientLogin,
                context =>RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetIp(context),
                    factory: _ => CreateOptions(patientLoginLimit, window, queueLimit)));

            options.AddPolicy(AppointmentBooking,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetUserOrIp(context),
                    factory: _ => CreateOptions(appointmentLimit, window, queueLimit)));
        });
        return services;
    }

    private static FixedWindowRateLimiterOptions CreateOptions(int permitLimit, TimeSpan window, int queueLimit)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = queueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };
    }

    private static string GetUserOrIp(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(context.User.Identity.Name))
        {
            return $"user:{context.User.Identity.Name}";
        }
        return $"ip:{GetIp(context)}";
    }

    private static string GetIp(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString()
               ?? "unknown";
    }
}
