using System;

namespace MUSIC.STREAMING.WEBSITE.Core.DTOs;

/// <summary>
/// Generic Result class for handling operation outcomes without throwing exceptions
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public ResultType Type { get; private set; }

    private Result() { }

    public static Result<T> Success(T data)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data,
            Type = ResultType.Success
        };
    }

    public static Result<T> Failure(string error, ResultType type = ResultType.BadRequest)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Error = error,
            Type = type
        };
    }

    public static Result<T> NotFound(string error = "Không tìm thấy")
    {
        return Failure(error, ResultType.NotFound);
    }

    public static Result<T> Unauthorized(string error = "Không có quyền truy cập")
    {
        return Failure(error, ResultType.Unauthorized);
    }

    public static Result<T> Forbidden(string error = "Không có quyền thực hiện")
    {
        return Failure(error, ResultType.Forbidden);
    }

    public static Result<T> BadRequest(string error = "Yêu cầu không hợp lệ")
    {
        return Failure(error, ResultType.BadRequest);
    }
}

/// <summary>
/// Non-generic Result class for void operations
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public string? Message { get; private set; }
    public string? Error { get; private set; }
    public ResultType Type { get; private set; }

    private Result() { }

    public static Result Success(string? message = null)
    {
        return new Result
        {
            IsSuccess = true,
            Message = message,
            Type = ResultType.Success
        };
    }

    public static Result Failure(string error, ResultType type = ResultType.BadRequest)
    {
        return new Result
        {
            IsSuccess = false,
            Error = error,
            Type = type
        };
    }

    public static Result NotFound(string error = "Không tìm thấy")
    {
        return Failure(error, ResultType.NotFound);
    }

    public static Result Unauthorized(string error = "Không có quyền truy cập")
    {
        return Failure(error, ResultType.Unauthorized);
    }

    public static Result Forbidden(string error = "Không có quyền thực hiện")
    {
        return Failure(error, ResultType.Forbidden);
    }
}

public enum ResultType
{
    Success,
    BadRequest,
    NotFound,
    Unauthorized,
    Forbidden
}
