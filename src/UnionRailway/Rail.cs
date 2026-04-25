namespace UnionRailway;

/// <summary>
/// Core railway result type for UnionRailway.
/// Represents either a successful value of type <typeparamref name="T"/> or a <see cref="UnionError"/>.
/// </summary>
#if NET11_0_OR_GREATER
public readonly union Rail<T>(T, UnionError)
{
    /// <summary>Gets whether the rail contains a success value.</summary>
    public readonly bool IsSuccess => Value is T and not UnionError;

    /// <summary>Gets whether the rail contains an error.</summary>
    public readonly bool IsError => Value is UnionError;

    /// <summary>Gets the error when present; otherwise <see langword="null"/>.</summary>
    public readonly UnionError? Error => Value is UnionError error ? error : default(UnionError?);

    /// <summary>Gets whether the rail is the default, uninitialized value.</summary>
    public readonly bool IsDefault => Value is null;

    /// <summary>Attempts to read the success value.</summary>
    public readonly bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        if (Value is T found and not UnionError)
        {
            value = found;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>Attempts to read the error value.</summary>
    public readonly bool TryGetError(out UnionError? value)
    {
        if (Value is UnionError error)
        {
            value = error;
            return true;
        }

        value = default(UnionError?);
        return false;
    }

    /// <summary>Deconstructs the rail into success and error slots.</summary>
    public readonly void Deconstruct([MaybeNull] out T value, out UnionError? error)
    {
        value = Value is T found and not UnionError ? found : default;
        error = Value is UnionError foundError ? foundError : default(UnionError?);
    }

    public override readonly string ToString() => Value?.ToString() ?? $"{nameof(Rail<>)}(default)";
}
#else
[System.Runtime.CompilerServices.Union]
public readonly struct Rail<T> : IEquatable<Rail<T>>, System.Runtime.CompilerServices.IUnion
{
    private readonly T? success;
    private readonly UnionError error;
    private readonly byte kind;

    /// <summary>Gets the underlying union value.</summary>
    public object? Value => kind switch
    {
        1 => success,
        2 => error,
        _ => null
    };

    /// <summary>Gets whether the rail contains a success value.</summary>
    public bool IsSuccess => kind == 1;

    /// <summary>Gets whether the rail contains an error.</summary>
    public bool IsError => kind == 2;

    /// <summary>Gets the error when present; otherwise <see langword="null"/>.</summary>
    public UnionError? Error => kind == 2 ? error : default(UnionError?);

    /// <summary>Gets whether the rail is the default, uninitialized value.</summary>
    public bool IsDefault => kind == 0;

    public Rail(T value)
    {
        success = value;
        error = default;
        kind = 1;
    }

    public Rail(UnionError error)
    {
        success = default;
        this.error = error;
        kind = 2;
    }

    public static implicit operator Rail<T>(T value) => new(value);

    public static implicit operator Rail<T>(UnionError error) => new(error);

    public static implicit operator Rail<T>(UnionError.NotFound error) => new((UnionError)error);

    public static implicit operator Rail<T>(UnionError.Conflict error) => new((UnionError)error);

    public static implicit operator Rail<T>(UnionError.Unauthorized error) => new((UnionError)error);

    public static implicit operator Rail<T>(UnionError.Forbidden error) => new((UnionError)error);

    public static implicit operator Rail<T>(UnionError.Validation error) => new((UnionError)error);

    public static implicit operator Rail<T>(UnionError.SystemFailure error) => new((UnionError)error);

    /// <summary>Attempts to read the success value.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = success;
        return kind == 1;
    }

    /// <summary>Attempts to read the error value.</summary>
    public bool TryGetError(out UnionError? value)
    {
        if (kind == 2)
        {
            value = error;
            return true;
        }

        value = default(UnionError?);
        return false;
    }

    /// <summary>Deconstructs the rail into success and error slots.</summary>
    public void Deconstruct([MaybeNull] out T value, out UnionError? error)
    {
        value = success;
        error = kind == 2 ? this.error : default(UnionError?);
    }

    public bool Equals(Rail<T> other)
    {
        return kind == other.kind && kind switch
        {
            0 => true,
            1 => EqualityComparer<T?>.Default.Equals(success, other.success),
            2 => error.Equals(other.error),
            _ => false
        };
    }

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is Rail<T> other && Equals(other);

    public override int GetHashCode() => kind switch
    {
        1 => HashCode.Combine(kind, success),
        2 => HashCode.Combine(kind, error),
        _ => 0
    };

    public override string ToString() => Value?.ToString() ?? $"{nameof(Rail<>)}(default)";

    public static bool operator ==(Rail<T> left, Rail<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Rail<T> left, Rail<T> right)
    {
        return !(left == right);
    }
}
#endif
