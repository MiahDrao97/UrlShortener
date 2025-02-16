using System.Reflection;

namespace UrlShortener.Tests;

/// <summary>
/// Use this attribute to use reflection on a field/property to call its Clear() method after each test
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ClearValuesAttribute : Attribute
{
    private const string _clearMethodName = nameof(ICollection<object>.Clear);

#pragma warning disable CA1822

    /// <summary>
    /// Clear a property
    /// </summary>
    /// <param name="property">Property to clear</param>
    /// <param name="instance">Object this property belongs to</param>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Clear(PropertyInfo property, object instance)
    {
        Type propertyType = property.PropertyType;
        MethodInfo? method = propertyType.GetMethod(_clearMethodName);

        if (method != null && method.GetParameters().Length == 0)
        {
            object? clearableInstance = property.GetValue(instance);
            method.Invoke(clearableInstance, null);
            return;
        }
        throw new InvalidOperationException($"No parameterless method '{_clearMethodName}()' exists on type {propertyType}.");
    }

    /// <summary>
    /// Clear a field
    /// </summary>
    /// <param name="field">Field to clear</param>
    /// <param name="instance">Object this field belongs to</param>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Clear(FieldInfo field, object instance)
    {
        Type fieldType = field.FieldType;
        MethodInfo? method = fieldType.GetMethod(_clearMethodName);

        if (method != null && method.GetParameters().Length == 0)
        {
            object? clearableInstance = field.GetValue(instance);
            method.Invoke(clearableInstance, null);
            return;
        }
        throw new InvalidOperationException($"No parameterless method '{_clearMethodName}()' exists on type {fieldType}.");
    }
}
