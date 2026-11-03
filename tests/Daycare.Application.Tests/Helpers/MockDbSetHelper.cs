using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;

namespace Daycare.Application.Tests.Helpers;

public static class MockDbSetHelper
{
    public static DbSet<T> CreateMockDbSet<T>(IEnumerable<T> source) where T : class
    {
        var data = source as List<T> ?? source.ToList();
        var queryable = new TestAsyncEnumerable<T>(data);
        var dbSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

        var iQueryable = (IQueryable<T>)queryable;
        ((IQueryable<T>)dbSet).Provider.Returns(iQueryable.Provider);
        ((IQueryable<T>)dbSet).Expression.Returns(iQueryable.Expression);
        ((IQueryable<T>)dbSet).ElementType.Returns(iQueryable.ElementType);
        ((IQueryable<T>)dbSet).GetEnumerator().Returns(_ => iQueryable.GetEnumerator());
        ((IAsyncEnumerable<T>)dbSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
            .Returns(c => queryable.GetAsyncEnumerator(c.Arg<CancellationToken>()));

        dbSet.When(d => d.Add(Arg.Any<T>())).Do(c => data.Add(c.Arg<T>()));

        return dbSet;
    }
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression) =>
        new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult);
        if (expectedResultType.IsGenericType && expectedResultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = expectedResultType.GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethods()
                .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(resultType)
                .Invoke(_inner, [expression]);

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [executionResult])!;
        }

        return Execute<TResult>(expression);
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    private readonly IQueryProvider _baseProvider;

    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
    {
        _baseProvider = ((IQueryable)new EnumerableQuery<T>(enumerable)).Provider;
    }

    public TestAsyncEnumerable(Expression expression) : base(expression)
    {
        _baseProvider = ((IQueryable)new EnumerableQuery<T>(expression)).Provider;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(_baseProvider);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(_inner.MoveNext());
}
