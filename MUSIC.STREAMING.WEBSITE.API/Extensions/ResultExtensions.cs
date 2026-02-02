using Microsoft.AspNetCore.Mvc;
using MUSIC.STREAMING.WEBSITE.Core.DTOs;

namespace MUSIC.STREAMING.WEBSITE.API.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Convert Result to IActionResult
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Data);
        }

        return result.Type switch
        {
            ResultType.NotFound => new NotFoundObjectResult(new { Message = result.Error }),
            ResultType.Unauthorized => new UnauthorizedObjectResult(new { Message = result.Error }),
            ResultType.Forbidden => new ObjectResult(new { Message = result.Error }) { StatusCode = 403 },
            _ => new BadRequestObjectResult(new { Message = result.Error })
        };
    }

    /// <summary>
    /// Convert Result to IActionResult with custom success response
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T?, object> successMapper)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(successMapper(result.Data));
        }

        return result.Type switch
        {
            ResultType.NotFound => new NotFoundObjectResult(new { Message = result.Error }),
            ResultType.Unauthorized => new UnauthorizedObjectResult(new { Message = result.Error }),
            ResultType.Forbidden => new ObjectResult(new { Message = result.Error }) { StatusCode = 403 },
            _ => new BadRequestObjectResult(new { Message = result.Error })
        };
    }

    /// <summary>
    /// Convert non-generic Result to IActionResult
    /// </summary>
    public static IActionResult ToActionResult(this Result result, string? successMessage = null)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(new { Message = result.Message ?? successMessage ?? "Thành công" });
        }

        return result.Type switch
        {
            ResultType.NotFound => new NotFoundObjectResult(new { Message = result.Error }),
            ResultType.Unauthorized => new UnauthorizedObjectResult(new { Message = result.Error }),
            ResultType.Forbidden => new ObjectResult(new { Message = result.Error }) { StatusCode = 403 },
            _ => new BadRequestObjectResult(new { Message = result.Error })
        };
    }
}
