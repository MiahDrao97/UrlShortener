using System.Reflection;

namespace UrlShortener.Tests;

/// <summary>
/// After each test, reset the decorated field or property
/// </summary>
/// <param name="reassignTo">(Optional) pass in value to reassign to</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ResetMeAttribute(object? reassignTo = null) : Attribute()
{
    /// <summary>
    /// If not null, then will reassign to this value rather than field/property's type's default
    /// </summary>
    public object? ReassignTo { get; init; } = reassignTo;

    /// <summary>
    /// If true, then will attempt to reset, using field/property's default constructor
    /// </summary>
    /// <remarks>
    /// If the field/property is an interface or abstract class, then this will be ignored
    /// </remarks>
    public bool ReassignToEmpty { get; init; }

    /// <summary>
    /// Reset a property
    /// </summary>
    /// <remarks>
    /// Throws <see cref="InvalidOperationException"/> if property cannot be written to
    /// </remarks>
    /// <param name="property">Property to reset</param>
    /// <param name="instance">Object this property belongs to</param>
    internal void Reset(PropertyInfo property, object instance)
    {
        if (!property.CanWrite)
        {
            throw new InvalidOperationException($"Cannot write to property '{property.Name}'.");
        }
        property.SetValue(instance, ReassignTo ?? GetDefaultOrEmptyValue(property.PropertyType));
    }

    /// <summary>
    /// Reset a field
    /// </summary>
    /// <param name="field">Field to reset</param>
    /// <param name="instance">Object this field belongs to</param>
    internal void Reset(FieldInfo field, object instance)
    {
        field.SetValue(instance, ReassignTo ?? GetDefaultOrEmptyValue(field.FieldType));
    }

    private object? GetDefaultOrEmptyValue(Type type)
    {
        if (ReassignToEmpty)
        {
            if (type.IsArray)
            {
                return Array.CreateInstance(type.GetGenericArguments()[0], 0);
            }
            return Activator.CreateInstance(type);
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}
