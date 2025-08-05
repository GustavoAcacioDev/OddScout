using OddScout.Application.Common.Models;
using System.Text.Json;

namespace odd_scout.Middleware;

public class ApiResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiResponseMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiResponseMiddleware(RequestDelegate next, ILogger<ApiResponseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Pular middleware para endpoints que não são API (Swagger, etc.)
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Capturar a resposta original
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            // Se houve erro HTTP, não modificar a resposta
            if (context.Response.StatusCode >= 400)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                await memoryStream.CopyToAsync(originalBodyStream);
                return;
            }

            // Ler o conteúdo da resposta
            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            // Se resposta vazia, não modificar
            if (string.IsNullOrEmpty(responseBody))
            {
                context.Response.Body = originalBodyStream;
                return;
            }

            // Tentar deserializar e encapsular
            var wrappedResponse = await WrapResponse(responseBody, context);

            // Escrever resposta encapsulada
            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json";

            var wrappedJson = JsonSerializer.Serialize(wrappedResponse, _jsonOptions);
            await context.Response.WriteAsync(wrappedJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ApiResponseMiddleware");

            // Em caso de erro, retornar resposta original
            context.Response.Body = originalBodyStream;
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }

    private Task<object> WrapResponse(string responseBody, HttpContext context)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            // Verificar se já é uma resposta encapsulada (evitar dupla encapsulação)
            if (IsAlreadyWrapped(root))
            {
                return Task.FromResult(JsonSerializer.Deserialize<object>(responseBody, _jsonOptions)!);
            }

            // Determinar tipo de resposta baseado na estrutura
            var responseType = DetermineResponseType(root);

            var result = responseType switch
            {
                ResponseType.PagedList => WrapPagedListResponse(root),
                ResponseType.SimpleList => WrapSimpleListResponse(root),
                ResponseType.SingleObject => WrapSingleObjectResponse(root),
                _ => WrapSingleObjectResponse(root)
            };

            return Task.FromResult(result);
        }
        catch (JsonException)
        {
            // Se não for JSON válido, encapsular como string
            return Task.FromResult<object>(new ApiResponse<string>(responseBody));
        }
    }

    private static bool IsAlreadyWrapped(JsonElement root)
    {
        // Verificar se já tem as propriedades do ApiResponse
        return root.ValueKind == JsonValueKind.Object &&
               (root.TryGetProperty("isSuccess", out _) ||
                root.TryGetProperty("errors", out _) ||
                (root.TryGetProperty("value", out _) && root.TryGetProperty("isSuccess", out _)) ||
                (root.TryGetProperty("items", out _) && root.TryGetProperty("total", out _)));
    }

    private static ResponseType DetermineResponseType(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return ResponseType.SingleObject;
        }

        // Verificar se é PagedResult (tem items, pageNumber, totalCount)
        if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array &&
            root.TryGetProperty("pageNumber", out _) &&
            root.TryGetProperty("totalCount", out _))
        {
            return ResponseType.PagedList;
        }

        // Verificar se é lista simples (array no root ou objeto com array como única propriedade relevante)
        if (root.ValueKind == JsonValueKind.Array)
        {
            return ResponseType.SimpleList;
        }

        // Verificar se objeto tem propriedades que indicam lista
        var hasListIndicators = false;
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array &&
                (property.Name.EndsWith("s") || property.Name.Contains("list", StringComparison.OrdinalIgnoreCase)))
            {
                hasListIndicators = true;
                break;
            }
        }

        return hasListIndicators ? ResponseType.SimpleList : ResponseType.SingleObject;
    }

    private static object WrapPagedListResponse(JsonElement root)
    {
        var items = root.GetProperty("items");
        var pageNumber = root.TryGetProperty("pageNumber", out var pageNumProp) ? pageNumProp.GetInt32() : 1;
        var totalCount = root.TryGetProperty("totalCount", out var totalProp) ? totalProp.GetInt32() : 0;

        return new
        {
            total = totalCount,
            pageIndex = pageNumber,
            items = JsonSerializer.Deserialize<object[]>(items.GetRawText()) ?? Array.Empty<object>(),
            isSuccess = true,
            hasWarnings = false,
            errors = Array.Empty<string>(),
            warnings = Array.Empty<string>()
        };
    }

    private static object WrapSimpleListResponse(JsonElement root)
    {
        object[] items;
        int total;

        if (root.ValueKind == JsonValueKind.Array)
        {
            items = JsonSerializer.Deserialize<object[]>(root.GetRawText()) ?? Array.Empty<object>();
            total = items.Length;
        }
        else
        {
            // Procurar a propriedade que contém o array
            var arrayProperty = root.EnumerateObject()
                .FirstOrDefault(p => p.Value.ValueKind == JsonValueKind.Array);

            if (arrayProperty.Value.ValueKind == JsonValueKind.Array)
            {
                items = JsonSerializer.Deserialize<object[]>(arrayProperty.Value.GetRawText()) ?? Array.Empty<object>();
                total = items.Length;
            }
            else
            {
                // Fallback: tratar como objeto único
                return WrapSingleObjectResponse(root);
            }
        }

        return new
        {
            total,
            pageIndex = 0,
            items,
            isSuccess = true,
            hasWarnings = false,
            errors = Array.Empty<string>(),
            warnings = Array.Empty<string>()
        };
    }

    private static object WrapSingleObjectResponse(JsonElement root)
    {
        var value = JsonSerializer.Deserialize<object>(root.GetRawText());

        return new
        {
            value,
            isSuccess = true,
            hasWarnings = false,
            errors = Array.Empty<string>(),
            warnings = Array.Empty<string>()
        };
    }

    private enum ResponseType
    {
        SingleObject,
        SimpleList,
        PagedList
    }
}

// Extension method para registrar o middleware
public static class ApiResponseMiddlewareExtensions
{
    public static IApplicationBuilder UseApiResponseWrapping(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiResponseMiddleware>();
    }
}