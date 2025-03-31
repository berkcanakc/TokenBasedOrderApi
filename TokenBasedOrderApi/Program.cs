using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();

string? currentToken = null;
DateTime tokenExpiresAt = DateTime.MinValue;
int tokenRequestCount = 0;
int tokenUsageCount = 0;
DateTime tokenWindowStart = DateTime.MinValue;
const int tokenLimit = 5;
const int tokenWindowSeconds = 3600; // Token yaşam süresi (saniye)
const int tokenUsageLimit = 5;

// Token üreten endpoint
app.MapPost("/get-token", () =>
{
    if (tokenWindowStart == DateTime.MinValue || DateTime.UtcNow > tokenWindowStart.AddSeconds(tokenWindowSeconds))
    {
        tokenWindowStart = DateTime.UtcNow;
        tokenRequestCount = 0;
        tokenUsageCount = 0;
        currentToken = null;
        tokenExpiresAt = DateTime.MinValue;
    }

    if (tokenRequestCount >= tokenLimit)
    {
        return Results.Json(new { error = "Saatlik token istek limiti aşıldı. Lütfen daha sonra tekrar deneyiniz." }, statusCode: 429);
    }

    if (!string.IsNullOrEmpty(currentToken) && DateTime.UtcNow < tokenExpiresAt)
    {
        if (tokenUsageCount >= tokenUsageLimit)
        {
            return Results.Json(new { error = "Token kullanım limiti doldu. Yeni token almak için mevcut token'ın süresinin dolması beklenmelidir." }, statusCode: 403);
        }
        return Results.BadRequest(new { error = "Aktif bir token zaten mevcut. Yeni token almak için mevcut token'ın süresinin dolması veya kullanım limitine ulaşması beklenmelidir." });
    }

    tokenRequestCount++;
    tokenUsageCount = 0;
    currentToken = Guid.NewGuid().ToString();
    tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenWindowSeconds - 5);

    return Results.Ok(new
    {
        token_type = "Bearer",
        access_token = currentToken,
        expires_in = (int)(tokenExpiresAt - DateTime.UtcNow).TotalSeconds
    });
});

// Token durumu sorgulama endpointi
app.MapGet("/token-status", () =>
{
    
    var remainingTime = (int)Math.Max((tokenWindowStart.AddSeconds(tokenWindowSeconds) - DateTime.UtcNow).TotalSeconds, 0);

    return Results.Ok(new
    {
        
        token_window_resets_in_seconds = remainingTime,
        token_active = !string.IsNullOrEmpty(currentToken) && DateTime.UtcNow < tokenExpiresAt && tokenUsageCount < tokenUsageLimit,
        token = currentToken,
        token_expires_in_seconds = (int)(tokenExpiresAt - DateTime.UtcNow).TotalSeconds,
        token_usage_count = tokenUsageCount
    });
});

//  Case için  oluşturulan apide statik verileri tanımlıyoruz
var staticOrders = new List<Order>
{
    new Order { Id = 1, ProductId = 101, Quantity = 1, OrderDate = DateTime.Now.AddMinutes(-5) },
    new Order { Id = 2, ProductId = 102, Quantity = 2, OrderDate = DateTime.Now.AddMinutes(-10) },
    new Order { Id = 3, ProductId = 103, Quantity = 3, OrderDate = DateTime.Now.AddMinutes(-15) },
    new Order { Id = 4, ProductId = 104, Quantity = 1, OrderDate = DateTime.Now.AddMinutes(-20) },
    new Order { Id = 5, ProductId = 105, Quantity = 4, OrderDate = DateTime.Now.AddMinutes(-25) },
    new Order { Id = 6, ProductId = 106, Quantity = 2, OrderDate = DateTime.Now.AddMinutes(-30) },
    new Order { Id = 7, ProductId = 107, Quantity = 1, OrderDate = DateTime.Now.AddMinutes(-35) },
    new Order { Id = 8, ProductId = 108, Quantity = 5, OrderDate = DateTime.Now.AddMinutes(-40) },
    new Order { Id = 9, ProductId = 109, Quantity = 3, OrderDate = DateTime.Now.AddMinutes(-45) },
    new Order { Id = 10, ProductId = 110, Quantity = 4, OrderDate = DateTime.Now.AddMinutes(-50) },
    new Order { Id = 11, ProductId = 111, Quantity = 1, OrderDate = DateTime.Now.AddMinutes(-55) },
    new Order { Id = 12, ProductId = 112, Quantity = 2, OrderDate = DateTime.Now.AddMinutes(-60) },
    new Order { Id = 13, ProductId = 113, Quantity = 3, OrderDate = DateTime.Now.AddMinutes(-65) },
    new Order { Id = 14, ProductId = 114, Quantity = 1, OrderDate = DateTime.Now.AddMinutes(-70) },
    new Order { Id = 15, ProductId = 115, Quantity = 5, OrderDate = DateTime.Now.AddMinutes(-75) },
    new Order { Id = 16, ProductId = 116, Quantity = 4, OrderDate = DateTime.Now.AddMinutes(-80) },
    new Order { Id = 17, ProductId = 117, Quantity = 2, OrderDate = DateTime.Now.AddMinutes(-85) },
    new Order { Id = 18, ProductId = 118, Quantity = 3, OrderDate = DateTime.Now.AddMinutes(-90) },
    new Order { Id = 19, ProductId = 119, Quantity = 1, OrderDate = DateTime.Now.AddMinutes(-95) },
    new Order { Id = 20, ProductId = 120, Quantity = 2, OrderDate = DateTime.Now.AddMinutes(-100) }
};

//Siparişleri döndüren endpoint (Token kontrolü ile)
app.MapGet("/orders", (HttpRequest request) =>
{
    var authHeader = request.Headers[HeaderNames.Authorization].ToString();

    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.Json(new { error = "Geçerli bir Authorization header bulunamadı. Lütfen 'Bearer <token>' formatında gönderin." }, statusCode: 401);

    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        return Results.Json(new { error = "Geçerli bir Authorization header bulunamadı. Lütfen 'Bearer <token>' formatında gönderin." }, statusCode: 401);

    var token = authHeader.Substring("Bearer ".Length).Trim();

    if (token != currentToken)
        return Results.Json(new { error = "Gönderilen token geçerli değil." }, statusCode: 401);

    if (DateTime.UtcNow > tokenExpiresAt)
        return Results.Json(new { error = "Token süresi dolmuştur. Lütfen yeni bir token alınız." }, statusCode: 401);

    if (tokenUsageCount >= tokenUsageLimit)
        return Results.Json(new { error = "Token kullanım limiti dolmuştur. Yeni token almak için süresinin dolması beklenmelidir." }, statusCode: 403);

    tokenUsageCount++;
    return Results.Ok(staticOrders);
});

app.Run();

// Order Modeli
public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime OrderDate { get; set; }
}
