// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.EntityFrameworkCore.Query;

/// <summary>
/// TODO
/// </summary>
public class ExpressionTreeCodeGenerator : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, string> _parameterMap = new Dictionary<ParameterExpression, string>();
    private int _indent = 0;

    /// <summary>
    /// TODO
    /// </summary>
    public ExpressionTreeCodeGenerator()
    {
        Result = new StringBuilder();
        ParameterDeclarations = new List<string>();
    }

    /// <summary>
    /// TODO
    /// </summary>
    public StringBuilder Result { get; }

    /// <summary>
    /// TODO
    /// </summary>
    public List<string> ParameterDeclarations { get; }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitBinary(BinaryExpression binaryExpression)
    {
        AppendIndent();

        var topLevel = binaryExpression.NodeType switch
        {
            ExpressionType.Equal => "Expression.Equal(",
            _ => throw new InvalidOperationException("todo"),
        };

        Result.AppendLine(topLevel);

        _indent++;
        Visit(binaryExpression.Left);
        Result.AppendLine(",");
        Visit(binaryExpression.Right);
        Result.AppendLine(")");
        _indent--;

        return binaryExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitBlock(BlockExpression blockExpression)
    {
        IndentedAppendLine("Expression.Block(");

        if (blockExpression.Variables.Any())
        {
            IndentedAppendLine("[");
            _indent++;
            foreach (var variable in blockExpression.Variables)
            {

            }

            _indent--;
            IndentedAppendLine("]");
        }

        IndentedAppendLine("[");
        _indent++;
        foreach (var expresson in blockExpression.Expressions)
        {
            Visit(expresson);
            Result.AppendLine(",");
        }
        _indent--;
        IndentedAppendLine("]");

        return blockExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override CatchBlock VisitCatchBlock(CatchBlock node) => base.VisitCatchBlock(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitConditional(ConditionalExpression node) => base.VisitConditional(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
        IndentedAppendLine("Expression.Constant(");

        _indent++;
        IndentedAppendLine(constantExpression.Value?.ToString() ?? "null,");
        IndentedAppendLine($"typeof({constantExpression.Type})");
        _indent--;

        IndentedAppendLine(")");

        return constantExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitDebugInfo(DebugInfoExpression node) => base.VisitDebugInfo(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitDefault(DefaultExpression node) => base.VisitDefault(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitDynamic(DynamicExpression node) => base.VisitDynamic(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override ElementInit VisitElementInit(ElementInit node) => base.VisitElementInit(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitExtension(Expression extension)
    {
        if (extension is LiftableConstantExpression liftableConstantExpression)
        {
            // for clarity, optimize out liftable constant for now
            // might add this later to see if it makes the difference

            var comment = ExpressionPrinter.Print(liftableConstantExpression.ResolverExpression).Replace("\n", "").Replace("\r", "").Replace("    ", "");

            IndentedAppendLine("");
            IndentedAppendLine("// " + comment);
            Visit(liftableConstantExpression.OriginalExpression);
        }

        return extension;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitGoto(GotoExpression node) => base.VisitGoto(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitIndex(IndexExpression node) => base.VisitIndex(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitInvocation(InvocationExpression node) => base.VisitInvocation(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitLabel(LabelExpression node) => base.VisitLabel(node);

    /// <summary>
    /// TODO
    /// </summary>
    [return: NotNullIfNotNull("node")]
    protected override LabelTarget? VisitLabelTarget(LabelTarget? node) => base.VisitLabelTarget(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitLambda<T>(Expression<T> lambdaExpression)
    {
        IndentedAppendLine("Expression.Lambda(");
        _indent++;

        Visit(lambdaExpression.Body);

        foreach (var parameter in lambdaExpression.Parameters)
        {
            IndentedAppendLine(",");
            Visit(parameter);
        }
        _indent--;

        IndentedAppendLine(")");

        return lambdaExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitListInit(ListInitExpression node) => base.VisitListInit(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitLoop(LoopExpression node) => base.VisitLoop(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        AppendIndent();

        Visit(memberExpression.Expression);

        Result.AppendLine("." + memberExpression.Member.Name);

        return memberExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node) => base.VisitMemberAssignment(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override MemberBinding VisitMemberBinding(MemberBinding node) => base.VisitMemberBinding(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitMemberInit(MemberInitExpression node) => base.VisitMemberInit(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override MemberListBinding VisitMemberListBinding(MemberListBinding node) => base.VisitMemberListBinding(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node) => base.VisitMemberMemberBinding(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
    {
        IndentedAppendLine("Expression.Call(");
        _indent++;

        if (methodCallExpression.Object == null)
        {
            IndentedAppendLine("instance: null,");
        }
        else
        {
            Visit(methodCallExpression.Object);
            Result.AppendLine(",");
        }

        IndentedAppend($"methodName: \"{methodCallExpression.Method.Name}\"");
        if (methodCallExpression.Arguments.Any())
        {
            Result.AppendLine(",");
        }
        else
        {
            Result.AppendLine();
        }

        var first = true;
        foreach (var argument in methodCallExpression.Arguments)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                IndentedAppendLine(",");
            }

            Visit(argument);
        }

        _indent--;
        IndentedAppendLine(")");

        return methodCallExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitNew(NewExpression node) => base.VisitNew(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitNewArray(NewArrayExpression node) => base.VisitNewArray(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitParameter(ParameterExpression parameterExpression)
    {
        if (!_parameterMap.ContainsKey(parameterExpression))
        {
            var parameterName = $"parameter{ParameterDeclarations.Count}";
            var parameterDeclaration = $"var {parameterName} = Exprsssion.Parameter(typeof({parameterExpression.Type.Name}), {parameterExpression.Name ?? "prm" + ParameterDeclarations.Count}));";
            ParameterDeclarations.Add(parameterDeclaration);
            _parameterMap[parameterExpression] = parameterName;
        }

        IndentedAppendLine(_parameterMap[parameterExpression]);

        return parameterExpression;
    }

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node) => base.VisitRuntimeVariables(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitSwitch(SwitchExpression node) => base.VisitSwitch(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override SwitchCase VisitSwitchCase(SwitchCase node) => base.VisitSwitchCase(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitTry(TryExpression node) => base.VisitTry(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitTypeBinary(TypeBinaryExpression node) => base.VisitTypeBinary(node);

    /// <summary>
    /// TODO
    /// </summary>
    protected override Expression VisitUnary(UnaryExpression unaryExpression)
    {
        var result = unaryExpression.NodeType switch
        {
            ExpressionType.Convert => "Expression.Convert(",
            _ => throw new InvalidOperationException("todo"),
        };

        IndentedAppendLine(result);

        _indent++;
        Visit(unaryExpression.Operand);
        IndentedAppendLine(",");
        IndentedAppendLine($"typeof({unaryExpression.Type.Name})");
        _indent--;

        IndentedAppendLine(")");

        return unaryExpression;
    }

    private void IndentedAppend(string value)
    {
        AppendIndent();
        Result.Append(value);
    }

    private void IndentedAppendLine(string value)
    {
        AppendIndent();
        Result.AppendLine(value);
    }

    private void AppendIndent() => Result.Append(new string(' ', _indent * 4));
}
