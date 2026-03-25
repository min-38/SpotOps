using System.Security.Claims;
using System.Text;

namespace SpotOps.Features.Payments;

public static class PaymentEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/reservations/{reservationId:guid}/payments")
            .WithTags("Payments")
            .RequireAuthorization();

        group.MapPost("/prepare", PrepareAsync);

        app.MapPost("/api/payments/portone/webhook", PortOneWebhookAsync)
            .WithTags("Payments")
            .AllowAnonymous();
    }

    private static async Task<IResult> PrepareAsync(
        Guid reservationId,
        PaymentService payments,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        var (response, error) = await payments.PrepareAsync(userId, reservationId, cancellationToken);
        if (error is not null)
            return Results.BadRequest(new { error });

        return Results.Json(response);
    }

    /// <summary>
    /// 포트원 V2 웹훅. 본문은 JSON 문자열 그대로 처리(시그니처 검증은 WebhookSecret + Standard Webhooks로 확장 가능).
    /// </summary>
    private static async Task<IResult> PortOneWebhookAsync(
        HttpRequest request,
        PaymentService payments,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        request.Body.Position = 0;

        await payments.ProcessPortOneWebhookAsync(body, cancellationToken);
        return Results.Ok();
    }
}
