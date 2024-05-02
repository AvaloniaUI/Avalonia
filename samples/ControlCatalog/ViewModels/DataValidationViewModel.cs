using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using MiniMvvm;

namespace ControlCatalog.ViewModels;

public class DataValidationViewModel : ViewModelBase
{
    private string? _DataAnnotationsSample;

    [Required]
    [EmailAddress]
    [MinLength(5)]
    public string? DataAnnotationsSample
    {
        get => _DataAnnotationsSample;
        set => RaiseAndSetIfChanged(ref _DataAnnotationsSample, value);
    }

    public Func<object, object> Converter { get; } = new Func<object, object>(o =>
    {
        return $"Error: {o}";
    });


    private string? _ExceptionInsideSetterSample;

    public string? ExceptionInsideSetterSample
    {
        get => _ExceptionInsideSetterSample;
        set
        {
            if (value is null || value.Length < 5)
                throw new ArgumentOutOfRangeException(nameof(value), "Give me 5 or more letter please :-)");

            RaiseAndSetIfChanged(ref _ExceptionInsideSetterSample, value);
        }
    }

    public Func<object, object> ExceptionConverter { get; } = new Func<object, object>(o =>
    {
        return o is Exception ex ? $"Huh, there was an Exception: {ex.Message}" : "Something went really wrong!";
    });
}
