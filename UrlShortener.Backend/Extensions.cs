using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace UrlShortener.Backend;

/// <summary>
/// All extensions in one place
/// </summary>
public static class Extensions
{
    public static Task<IActionResult> ConvertAsync<T>(this Task<T> task) where T : IConvertToActionResult
    {
        ArgumentNullException.ThrowIfNull(task);
        return ConvertAsyncCore(task);
    }

    private static async Task<IActionResult> ConvertAsyncCore<T>(Task<T> task) where T : IConvertToActionResult
    {
        return (await task).Convert();
    }
}
