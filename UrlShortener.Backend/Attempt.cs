using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace UrlShortener.Backend;

public sealed class Attempt
{
    public static Attempt Ok { get; } = new();

    private Attempt()
    {
        Success = true;
    }

    public Attempt(Err err)
    {
        Success = false;
        Err = err;
    }

    [MemberNotNullWhen(false, nameof(Err))]
    public bool Success { get; }

    public Err? Err { get; }

    public T Match<T>(Func<T> ifOk, Func<Err, T> ifErr)
    {
        if (Success)
        {
            return ifOk();
        }
        return ifErr(Err);
    }

    public static Attempt From<T>(
        Attempt<T> other,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        return other.Match<Attempt>(
            static _ => Ok,
            err => new Err(err, filePath, memberName, lineNumber));
    }

    public static Attempt From(
        Attempt other,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        return other.Match<Attempt>(
            static () => Ok,
            err => new Err(err, filePath, memberName, lineNumber));
    }

    public static implicit operator Attempt(Err err)
    {
        return new(err);
    }
}

public sealed class Attempt<T>
{
    private readonly bool _success;
    private readonly object? _result;

    public Attempt(T ok)
    {
        _success = true;
        _result = ok;
    }

    public Attempt(Err err)
    {
        _success = false;
        _result = err;
    }

    public TResult Match<TResult>(Func<T, TResult> ifOk, Func<Err, TResult> ifErr)
    {
        if (IsSuccess(out T? result))
        {
            return ifOk(result);
        }
        return ifErr(Err);
    }

    public Err? Err
    {
        get
        {
            if (_success)
            {
                return null;
            }
            return (Err)_result!;
        }
    }

    [MemberNotNullWhen(false, nameof(Err))]
    public bool IsSuccess([MaybeNullWhen(false)] out T result)
    {
        if (_success)
        {
            result = (T)_result!;
            return true;
        }
#pragma warning disable CS8775
        result = default!;
        return false;
#pragma warning restore CS8775
    }

    public static Attempt<T> FromError(
        Attempt other,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        return other.Match<Attempt<T>>(
            static () => throw new InvalidOperationException($"Expected error result, not ok."),
            err => new Err(err, filePath, memberName, lineNumber)); // captures stack trace
    }

    public static Attempt<T> FromError<TOther>(
        Attempt<TOther> other,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        return other.Match<Attempt<T>>(
            static _ => throw new InvalidOperationException($"Expected error result, not ok."),
            err => new Err(err, filePath, memberName, lineNumber)); // captures stack trace
    }

    public static implicit operator Attempt<T>(T ok)
    {
        return new(ok);
    }

    public static implicit operator Attempt<T>(Err err)
    {
        return new(err);
    }
}
