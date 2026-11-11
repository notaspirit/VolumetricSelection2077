using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.Core.Models;

public class Result
{
    public ResultStatus Status { get; set; }
    private string? _message;

    public string? Message
    {
        get => _message;
        private set => _message = value;
    }

    public Result()
    {
    }

    public Result(ResultStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }

    public void AddMessage(string message)
    {
        if (string.IsNullOrEmpty(_message))
        {
            _message = message;
        }
        else
        {
            _message += $"\n{message}";
        }
    }

    public void ClearMessage()
    {
        _message = null;
    }
}

public class Result<T> : Result
{
    public T? Data { get; set; }
}