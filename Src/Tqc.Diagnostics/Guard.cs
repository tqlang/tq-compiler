namespace Tqc.Diagnostics;

public static class Guard
{
    /// <summary>
    /// Tries to execute `func`. If it emits a CompilationError, executes `@catch`
    /// and propagates the error.
    /// </summary>
    /// <param name="func">The action to try</param>
    /// <param name="catch">The action when catch</param>
    public static void Assert(Action func, Action<CompilationError> @catch)
    {
        try
        {
            func();
        }
        catch (CompilationError ex)
        {
            @catch.Invoke(ex);
            throw;
        }
    }
    /// <summary>
    /// Tries to execute `func`. If it emits a CompilationError, executes `@catch`
    /// and propagates the error.
    /// </summary>
    /// <param name="func">The action to try</param>
    /// <param name="catch">The action when catch</param>
    public static T Assert<T>(Func<T> func, Action<CompilationError> @catch)
    {
        try
        {
            return func();
        }
        catch (CompilationError ex)
        {
            @catch?.Invoke(ex);
            throw;
        }
    }

    /// <summary>
    /// Tries to execute `func`. If it emits a CompilationError, executes `@catch`
    /// and returns `default`.
    /// </summary>
    /// <param name="func">The action to try</param>
    /// <param name="catch">The action when catch</param>
    public static void Try(Action func, Action<CompilationError> @catch)
    {
        try
        {
            func();
        }
        catch (CompilationError ex)
        {
            @catch.Invoke(ex);
        }
    }
    /// <summary>
    /// Tries to execute `func`. If it emits a CompilationError, executes `@catch`
    /// and returns `default`.
    /// </summary>
    /// <param name="func">The action to try</param>
    /// <param name="catch">The action when catch</param>
    public static T Try<T>(Func<T> func, Func<CompilationError, T> @catch)
    {
        try
        {
            return func();
        }
        catch (CompilationError ex)
        {
            return @catch.Invoke(ex);
        }
    }
}
