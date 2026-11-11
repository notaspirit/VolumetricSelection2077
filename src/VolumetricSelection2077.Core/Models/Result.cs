using VolumetricSelection2077.Core.Enums;

namespace VolumetricSelection2077.Core.Models;

public class Result
{
    public ResultStatus Status { get; set; }
    public string? Message { get; set; }
}

public class Result<T> : Result
{
    public T? Data { get; set; }
}