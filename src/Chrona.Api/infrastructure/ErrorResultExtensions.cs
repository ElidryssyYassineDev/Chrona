using Chrona.Shared.Application;

namespace Chrona.Api.Infrastructure;

public static class ErrorResultExtensions
{
    // Enveloppe d'erreur de 14-api-design.md : 400 validation, 404 introuvable, 409 règle métier.
    public static IResult ToErrorResult(this Error error)
    {
        var body = new
        {
            error = new
            {
                code = error.Code,
                message = error.Message,
                details = error.Field is null
                    ? Array.Empty<object>()
                    : new object[] { new { field = error.Field, issue = error.Message } }
            }
        };

        return error.Kind switch
        {
            ErrorKind.Validation => Results.BadRequest(body),
            ErrorKind.NotFound => Results.NotFound(body),
            ErrorKind.Conflict => Results.Conflict(body),
            _ => Results.StatusCode(500)
        };
    }
}