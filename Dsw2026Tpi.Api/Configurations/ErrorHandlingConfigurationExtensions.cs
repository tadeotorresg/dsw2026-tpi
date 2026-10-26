using Dsw2026Tpi.CrossCutting.Resources;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Dsw2026Tpi.CrossCutting.Models;

namespace Dsw2026Tpi.Api.Configurations;

    public static class ErrorHandlingConfigurationExtensions
    {
        public static IServiceCollection AddAppErrorHandling(
            this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressMapClientErrors = true;

                options.InvalidModelStateResponseFactory = context =>
                {
                    var error = new ErrorResponse(
                        nameof(ErrorCodes.VALIDATION_ERROR),
                        ErrorCodes.VALIDATION_ERROR);

                    foreach (var entry in context.ModelState)
                    {
                        if (entry.Value is null ||
                            entry.Value.Errors.Count == 0)
                        {
                            continue;
                        }

                        var field = entry.Key.TrimStart('$', '.');

                        field = string.IsNullOrWhiteSpace(field) ||
                                field == "request"
                            ? "body"
                            : JsonNamingPolicy.CamelCase.ConvertName(field);

                        var issue = field switch
                        {
                            "body" =>
                                "El cuerpo de la solicitud está vacío o no tiene un formato válido.",

                            "date" or "fromDate" or "toDate" =>
                                "La fecha no tiene un formato válido.",

                            _ =>
                                "El valor es obligatorio o no tiene un formato válido."
                        };

                        error.AddDetail(field, issue);
                    }

                    return new BadRequestObjectResult(error);
                };
            });

            return services;
        }

        public static IApplicationBuilder UseAppErrorHandling(
            this IApplicationBuilder app)
        {
            return app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;

                var error = response.StatusCode switch
                {
                    StatusCodes.Status400BadRequest => new ErrorResponse(
                        nameof(ErrorCodes.VALIDATION_ERROR),
                        ErrorCodes.VALIDATION_ERROR),

                    StatusCodes.Status401Unauthorized => new ErrorResponse(
                        nameof(ErrorCodes.AUTHENTICATION_FAILED),
                        "Debe iniciar sesión con un token válido."),

                    StatusCodes.Status403Forbidden => new ErrorResponse(
                        nameof(ErrorCodes.AUTHORIZATION_FAILED),
                        ErrorCodes.AUTHORIZATION_FAILED),

                    StatusCodes.Status404NotFound => new ErrorResponse(
                        nameof(ErrorCodes.ENTITY_NOTFOUND),
                        "El recurso o la ruta solicitada no existe."),

                    StatusCodes.Status405MethodNotAllowed => new ErrorResponse(
                        "METHOD_NOT_ALLOWED",
                        "El método HTTP no está permitido para esta ruta."),

                    StatusCodes.Status409Conflict => new ErrorResponse(
                        "CONFLICT",
                        "La operación presenta un conflicto con el estado actual."),

                    StatusCodes.Status415UnsupportedMediaType => new ErrorResponse(
                        "UNSUPPORTED_MEDIA_TYPE",
                        "El tipo de contenido enviado no está admitido."),

                    StatusCodes.Status429TooManyRequests => new ErrorResponse(
                        nameof(ErrorCodes.TOO_MANY_REQUESTS),
                        ErrorCodes.TOO_MANY_REQUESTS),

                    >= 500 => new ErrorResponse(
                        nameof(ErrorCodes.UNHANDLED_ERROR),
                        ErrorCodes.UNHANDLED_ERROR),

                    _ => new ErrorResponse(
                        "HTTP_ERROR",
                        "No se pudo procesar la solicitud.")
                };

                await response.WriteAsJsonAsync(error);
            });
        }
    }






