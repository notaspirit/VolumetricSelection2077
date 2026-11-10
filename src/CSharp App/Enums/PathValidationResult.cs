namespace VolumetricSelection2077.App.Enums;

public enum PathValidationResult
{
    InvalidCharacters,
    Empty,
    Relative,
    Drive,
    LeadingOrTrailingSpace,
    TooLong,
    Valid
}