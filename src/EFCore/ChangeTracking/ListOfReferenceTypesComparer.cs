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
        nameof(Compare), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(object), typeof(Func<object, object, bool>)])!;

    private static readonly MethodInfo TypedCompareMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(TypedCompare), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(object), typeof(Func<TElement, TElement, bool>)])!;

    private static readonly MethodInfo GetHashCodeMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(GetHashCode), BindingFlags.Static | BindingFlags.NonPublic, [typeof(IEnumerable), typeof(Func<object, int>)])!;

    private static readonly MethodInfo TypedGetHashCodeMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(TypedGetHashCode), BindingFlags.Static | BindingFlags.NonPublic, [typeof(IEnumerable), typeof(Func<TElement, int>)])!;

    private static readonly MethodInfo SnapshotMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(Snapshot), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(Func<object, object>)])!;

    private static readonly MethodInfo TypedSnapshotMethod = typeof(ListOfReferenceTypesComparer<TConcreteList, TElement>).GetMethod(
        nameof(TypedSnapshot), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(Func<TElement, TElement>)])!;

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

        // nested collection case - TElement of element comparer is typed as object
        // but TElement of (this) list comparer is likely something else so we are using untyped overload of Compare
        if (elementComparer is ValueComparer<object>)
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

        // special case for object[] { 1, 2, 3 }
        // TElement of element comparer is int, so its equals method is typed as Func<int, int, bool>
        // which doesn't fit the expected Func<object, object, bool>
        // rewriting the inner lambda parameters to objects and cast to proper type in the body so that semantics are the same
        if (typeof(TElement) == typeof(object))
        {
            var newInnerPrm1 = Expression.Parameter(typeof(object), "a");
            var newInnerPrm2 = Expression.Parameter(typeof(object), "b");

            var newEqualsExpressionBody = ReplacingExpressionVisitor.Replace(
                elementComparer.EqualsExpression.Parameters,
                [Expression.Convert(newInnerPrm1, elementComparer.Type), Expression.Convert(newInnerPrm2, elementComparer.Type)],
                elementComparer.EqualsExpression.Body);

            return Expression.Lambda<Func<object?, object?, bool>>(
                Expression.Call(
                    TypedCompareMethod,
                    prm1,
                    prm2,
                    Expression.Lambda(
                        newEqualsExpressionBody,
                        newInnerPrm1,
                        newInnerPrm2)),
                prm1,
                prm2);
        }

        // what's left is either collection of scalars or collection of collections of (nullable) value types
        // for scalars, TElement of element comparer and list comparer match
        // for collection of value types, element comparer is typed as, say, IEnumerable<int> whereas TElement of list comparer
        // is something like int[] - those are still compatible with the typed overload
        // Func<IEnumerable<int>, IEnumerable<int>, bool> is a valid argument for Func<int[], int[], bool>
        return Expression.Lambda<Func<object?, object?, bool>>(
            Expression.Call(
                TypedCompareMethod,
                prm1,
                prm2,
                elementComparer.EqualsExpression),
            prm1,
            prm2);
    }

    private static Expression<Func<object, int>> GetHashCodeLambda(ValueComparer elementComparer)
    {
        var prm = Expression.Parameter(typeof(object), "o");

        if (elementComparer is ValueComparer<object>)
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

        if (typeof(TElement) == typeof(object))
        {
            var newInnerPrm = Expression.Parameter(typeof(object), "o");

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

        return Expression.Lambda<Func<object, int>>(
            Expression.Call(
                TypedGetHashCodeMethod,
                Expression.Convert(
                    prm,
                    typeof(IEnumerable)),
                    elementComparer.HashCodeExpression),
            prm);
    }

    private static Expression<Func<object, object>> SnapshotLambda(ValueComparer elementComparer)
    {
        var prm = Expression.Parameter(typeof(object), "source");

        if (elementComparer is ValueComparer<object>)
        {
            return Expression.Lambda<Func<object, object>>(
                Expression.Call(
                    SnapshotMethod,
                    prm,
                    elementComparer.SnapshotExpression),
                prm);
        }

        if (typeof(TElement) == typeof(object))
        {
            var newInnerPrm = Expression.Parameter(typeof(object), "source");

            var newInnerBody = ReplacingExpressionVisitor.Replace(
                elementComparer.SnapshotExpression.Parameters[0],
                Expression.Convert(newInnerPrm, elementComparer.Type),
                elementComparer.SnapshotExpression.Body);

            // note we need to also convert the result of inner lambda back to object
            return Expression.Lambda<Func<object, object>>(
                Expression.Call(
                    SnapshotMethod,
                    prm,
                    Expression.Lambda(
                        Expression.Convert(
                            newInnerBody,
                            typeof(object)),
                        newInnerPrm)),
                prm);
        }

        // special case for for list of (nullable) value types
        // TElement of the element comparer is typed as say, IEnumerable<int>
        // while the TElement of the list comparer would be something like int[]
        // this makes snapshot lambdas incompatible - we can't use Func<IEnumerable<int>, IEnumerable<int>>
        // as argument to a method expecting Func<int[], int[]>
        // to make it work we need to adjust the return type to Func<int[], IEnumerable<int>>
        if (elementComparer.GetType() is Type { IsGenericType: true } genericType
            && genericType.GetGenericTypeDefinition() is Type genericTypeDefinition
            && (genericTypeDefinition == typeof(ListOfValueTypesComparer<,>)
                || genericTypeDefinition == typeof(ListOfNullableValueTypesComparer<,>)))
        {
            var result = Expression.Lambda<Func<object, object>>(
                Expression.Call(
                   TypedSnapshotMethod,
                    prm,
                    Expression.Lambda(
                        Expression.Convert(
                            elementComparer.SnapshotExpression.Body,
                            typeof(TElement)),
                        elementComparer.SnapshotExpression.Parameters)),
                prm);

            return result;
        }

        Check.DebugAssert(
            elementComparer is ValueComparer<TElement>,
            "Element comparer should be typed as ValueComparer<TElement> at this point.");

        // only thing left is collection of scalars (since we handled ListOfValueTypesComparer
        // and ListOfNullableValueTypesComparer above) - TElement of element comparer and list comparer
        // are the same so no comparibility issues between funcs
        return Expression.Lambda<Func<object, object>>(
            Expression.Call(
                TypedSnapshotMethod,
                prm,
                elementComparer.SnapshotExpression),
            prm);
    }

    private static bool Compare(object? a, object? b, Func<object?, object?, bool> elementCompare)
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

    private static bool TypedCompare(object? a, object? b, Func<TElement?, TElement?, bool> elementCompare)
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

    private static int GetHashCode(IEnumerable source, Func<object?, int> elementGetHashCode)
    {
        var hash = new HashCode();

        foreach (var el in source)
        {
            hash.Add(el == null ? 0 : elementGetHashCode(el));
        }

        return hash.ToHashCode();
    }

    private static int TypedGetHashCode(IEnumerable source, Func<TElement?, int> elementGetHashCode)
    {
        var hash = new HashCode();

        foreach (var el in source)
        {
            hash.Add(el == null ? 0 : elementGetHashCode((TElement?)el));
        }

        return hash.ToHashCode();
    }

    private static IList<TElement?> Snapshot(object source, Func<object?, object?> elementSnapshot)
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
                snapshot[i] = instance == null ? null : (TElement?)elementSnapshot(instance);
            }

            return snapshot;
        }
        else
        {
            var snapshot = IsReadOnly ? new List<TElement?>() : (IList<TElement?>)Activator.CreateInstance<TConcreteList>()!;
            foreach (var e in sourceList)
            {
                snapshot.Add(e == null ? null : (TElement?)elementSnapshot(e));
            }

            return IsReadOnly
                ? (IList<TElement?>)Activator.CreateInstance(typeof(TConcreteList), snapshot)!
                : snapshot;
        }
    }

    private static IList<TElement?> TypedSnapshot(object source, Func<TElement?, TElement?> elementSnapshot)
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
