// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.EntityFrameworkCore.ChangeTracking;

/// <summary>
///     A <see cref="ValueComparer{T}" /> for lists of primitive items. The list can be typed as <see cref="IEnumerable{T}" />,
///     but can only be used with instances that implement <see cref="IList{T}" />.
/// </summary>
/// <remarks>
///     <para>
///         This comparer should be used when the element of the comparer is typed as <see cref="object" />.
///     </para>
///     <para>
///         See <see href="https://aka.ms/efcore-docs-value-comparers">EF Core value comparers</see> for more information and examples.
///     </para>
/// </remarks>
/// <typeparam name="TConcreteList">The collection type to create an index of, if needed.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
public sealed class ListOfReferenceTypesComparer<TConcreteList, TElement> : ValueComparer<object>, IInfrastructure<ValueComparer>
    where TElement : class
{
    private static readonly bool IsArray = typeof(TConcreteList).IsArray;

    private static readonly bool IsReadOnly = IsArray
        || (typeof(TConcreteList).IsGenericType
            && typeof(TConcreteList).GetGenericTypeDefinition() == typeof(ReadOnlyCollection<>));

    private static readonly MethodInfo CompareMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(Compare), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(object), typeof(Func<TElement, TElement, bool>)])!;

    private static readonly MethodInfo GetHashCodeMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(GetHashCode), BindingFlags.Static | BindingFlags.NonPublic, [typeof(IEnumerable), typeof(Func<TElement, int>)])!;

    private static readonly MethodInfo SnapshotMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(Snapshot), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(Func<TElement, TElement>)])!;

    /// <summary>
    ///     Creates a new instance of the list comparer.
    /// </summary>
    /// <param name="elementComparer">The comparer to use for comparing elements.</param>
    public ListOfReferenceTypesComparer(ValueComparer elementComparer)
        : base(
            CompareLambda(elementComparer),
            GetHashCodeLambda(elementComparer),
            SnapshotLambda(elementComparer))
        => ElementComparer = elementComparer;

    /// <summary>
    ///     The comparer to use for comparing elements.
    /// </summary>
    public ValueComparer ElementComparer { get; }

    ValueComparer IInfrastructure<ValueComparer>.Instance
        => ElementComparer;

    private static Expression<Func<object?, object?, bool>> CompareLambda(ValueComparer elementComparer)
    {
        var prm1 = Expression.Parameter(typeof(object), "a");
        var prm2 = Expression.Parameter(typeof(object), "b");

        // we check the compatibility of expected equals lambda signature Func<TElement, TElement, bool>
        // vs method we actually get from the element comparer, which would be typed to its generic argument
        // if the expected is assignable from actual we can just do simple call...
        var expectedSignature = typeof(Func<TElement, TElement, bool>);
        var actualSignature = typeof(Func<,,>).MakeGenericType(elementComparer.Type, elementComparer.Type, typeof(bool));
        if (expectedSignature.IsAssignableFrom(actualSignature))
        {
            return Expression.Lambda<Func<object?, object?, bool>>(
                Expression.Call(
                    CompareMethod,
                    prm1,
                    prm2,
                    elementComparer.EqualsExpression),
                prm1,
                prm2);
        }

        // ...otherwise we need to rewrite the actual lambda (as we can't change the expected signature)
        // in that case we are rewriting the inner lambda parameters to TElement and cast to the element comparer
        // type argument in the body, so that semantics of the element comparison func don't change
        var newInnerPrm1 = Expression.Parameter(typeof(TElement), "a");
        var newInnerPrm2 = Expression.Parameter(typeof(TElement), "b");

        var newEqualsExpressionBody = ReplacingExpressionVisitor.Replace(
            elementComparer.EqualsExpression.Parameters,
            [Expression.Convert(newInnerPrm1, elementComparer.Type), Expression.Convert(newInnerPrm2, elementComparer.Type)],
            elementComparer.EqualsExpression.Body);

        return Expression.Lambda<Func<object?, object?, bool>>(
            Expression.Call(
                CompareMethod,
                prm1,
                prm2,
                Expression.Lambda(
                    newEqualsExpressionBody,
                    newInnerPrm1,
                    newInnerPrm2)),
            prm1,
            prm2);
    }

    private static Expression<Func<object, int>> GetHashCodeLambda(ValueComparer elementComparer)
    {
        var prm = Expression.Parameter(typeof(object), "o");

        var expectedSignature = typeof(Func<TElement, int>);
        var actualSignature = typeof(Func<,>).MakeGenericType(elementComparer.Type, typeof(int));
        if (expectedSignature.IsAssignableFrom(actualSignature))
        {
            return Expression.Lambda<Func<object, int>>(
                Expression.Call(
                    GetHashCodeMethod,
                    Expression.Convert(
                        prm,
                        typeof(IEnumerable)),
                        elementComparer.HashCodeExpression),
                prm);
        }

        var newInnerPrm = Expression.Parameter(typeof(TElement), "o");

        var newInnerBody = ReplacingExpressionVisitor.Replace(
            elementComparer.HashCodeExpression.Parameters[0],
            Expression.Convert(newInnerPrm, elementComparer.Type),
            elementComparer.HashCodeExpression.Body);

        return Expression.Lambda<Func<object, int>>(
            Expression.Call(
                GetHashCodeMethod,
                Expression.Convert(
                    prm,
                    typeof(IEnumerable)),
                Expression.Lambda(
                    newInnerBody,
                    newInnerPrm)),
            prm);
    }

    private static Expression<Func<object, object>> SnapshotLambda(ValueComparer elementComparer)
    {
        var prm = Expression.Parameter(typeof(object), "source");

        var expectedSignature = typeof(Func<TElement, TElement>);
        var actualSignature = typeof(Func<,>).MakeGenericType(elementComparer.Type, elementComparer.Type);
        if (expectedSignature.IsAssignableFrom(actualSignature))
        {
            return Expression.Lambda<Func<object, object>>(
                Expression.Call(
                    SnapshotMethod,
                    prm,
                    elementComparer.SnapshotExpression),
                prm);
        }

        var newInnerPrm = Expression.Parameter(typeof(TElement), "source");

        var newInnerBody = ReplacingExpressionVisitor.Replace(
            elementComparer.SnapshotExpression.Parameters[0],
            Expression.Convert(newInnerPrm, elementComparer.Type),
            elementComparer.SnapshotExpression.Body);

        // note we need to also convert the result of inner lambda back to TElement
        return Expression.Lambda<Func<object, object>>(
            Expression.Call(
                SnapshotMethod,
                prm,
                Expression.Lambda(
                    Expression.Convert(
                        newInnerBody,
                        typeof(TElement)),
                    newInnerPrm)),
            prm);
    }

    private static bool Compare(object? a, object? b, Func<TElement?, TElement?, bool> elementCompare)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null)
        {
            return b is null;
        }

        if (b is null)
        {
            return false;
        }

        if (a is IList<TElement?> aList && b is IList<TElement?> bList)
        {
            if (aList.Count != bList.Count)
            {
                return false;
            }

            for (var i = 0; i < aList.Count; i++)
            {
                var (el1, el2) = (aList[i], bList[i]);
                if (el1 is null)
                {
                    if (el2 is null)
                    {
                        continue;
                    }

                    return false;
                }

                if (el2 is null)
                {
                    return false;
                }

                if (!elementCompare(el1, el2))
                {
                    return false;
                }
            }

            return true;
        }

        throw new InvalidOperationException(
            CoreStrings.BadListType(
                (a is IList<TElement?> ? b : a).GetType().ShortDisplayName(),
                typeof(IList<>).MakeGenericType(typeof(TElement)).ShortDisplayName()));
    }

    private static int GetHashCode(IEnumerable source, Func<TElement?, int> elementGetHashCode)
    {
        var hash = new HashCode();

        foreach (var el in source)
        {
            hash.Add(el == null ? 0 : elementGetHashCode((TElement?)el));
        }

        return hash.ToHashCode();
    }

    private static IList<TElement?> Snapshot(object source, Func<TElement?, TElement?> elementSnapshot)
    {
        if (source is not IList<TElement?> sourceList)
        {
            throw new InvalidOperationException(
                CoreStrings.BadListType(
                    source.GetType().ShortDisplayName(),
                    typeof(IList<>).MakeGenericType(typeof(TElement)).ShortDisplayName()));
        }

        if (IsArray)
        {
            var snapshot = new TElement?[sourceList.Count];
            for (var i = 0; i < sourceList.Count; i++)
            {
                var instance = sourceList[i];
                snapshot[i] = instance == null ? null : elementSnapshot(instance);
            }

            return snapshot;
        }
        else
        {
            var snapshot = IsReadOnly ? new List<TElement?>() : (IList<TElement?>)Activator.CreateInstance<TConcreteList>()!;
            foreach (var e in sourceList)
            {
                snapshot.Add(e == null ? null : elementSnapshot(e));
            }

            return IsReadOnly
                ? (IList<TElement?>)Activator.CreateInstance(typeof(TConcreteList), snapshot)!
                : snapshot;
        }
    }
}
