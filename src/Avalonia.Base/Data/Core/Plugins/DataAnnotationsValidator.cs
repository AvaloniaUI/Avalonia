using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Avalonia.Data.Core.Plugins;

[RequiresUnreferencedCode(TrimmingMessages.DataValidationPluginRequiresUnreferencedCodeMessage)]
internal class DataAnnotationsValidator : MemberDataValidator
{
    private readonly ValidationContext _context;
    private readonly string _memberName;

    public DataAnnotationsValidator(object source, string memberName) : base(source)
    {
        _context = new(source) { MemberName = memberName };
        _memberName = memberName;
    }

    public override bool RaisesEvents => false;

    public override Exception? GetDataValidationError()
    {
        if (!TryGetSource(out var source) || !TryGetValue(source, _memberName, out var value))
            return null;

        var errors = new List<ValidationResult>();
        return !Validator.TryValidateProperty(value, _context, errors) ? 
            CreateException(errors) : null;
    }

    protected override void Subscribe(object source)
    {
        // DataAnnotations do not provide change notifications.
    }

    protected override void Unsubscribe(object source)
    {
        // DataAnnotations do not provide change notifications.
    }

    private static Exception CreateException(IList<ValidationResult> errors)
    {
        if (errors.Count == 1)
        {
            return new DataValidationException(errors[0].ErrorMessage);
        }
        else
        {
            return new AggregateException(
                errors.Select(x => new DataValidationException(x.ErrorMessage)));
        }
    }

    private static bool TryGetValue(object source, string memberName, out object? value)
    {
        var properties = TypeDescriptor.GetProperties(source.GetType());

        foreach (PropertyDescriptor p in properties)
        {
            if (p.Name == memberName)
            {
                value = p.GetValue(source);
                return true;
            }
        }

        value = null;
        return false;
    }
}
