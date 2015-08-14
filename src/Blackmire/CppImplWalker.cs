using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackmire
{
  public class CppImplWalker : CppWalker
  {
    private readonly CodeBuilder cb = new CodeBuilder();

    public CppImplWalker(CSharpCompilation compilation, SemanticModel model, ConversionSettings settings) : base(compilation, model, settings)
    {
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      
    }

    public override void VisitInitializerExpression(InitializerExpressionSyntax node)
    {
      base.VisitInitializerExpression(node);
    }

    public override void VisitExpressionStatement(ExpressionStatementSyntax node)
    {
      // hack: Console.WriteLine
      var s = node.Expression.GetText().ToString().Trim();
      if (s.StartsWith("Console.WriteLine"))
      {
        var ise = node.Expression as InvocationExpressionSyntax;
        var args = ise.ArgumentList;
        cb.AppendWithIndent("cout << ")
          .Append(args.GetText().ToString().Trim('(', ')').Replace(" + ", " << ").Replace("+", " << "))
          .AppendLine(" << endl;");
      }
      else
      {
        base.VisitExpressionStatement(node);
      }
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
      // generate empty ctor if necessary
      if (node.HasInitializableMembers(model) && !node.HasDefaultConstructor())
      {
        cb.AppendWithIndent(node.Identifier.ToString())
          .Append("::")
          .Append(node.Identifier.ToString())
          .AppendLine("() :");

        //cb.Indent(() =>
        //{
        //  AppendFieldInitializers(node);
        //});

        cb.AppendLine("{}").AppendLine();
      }

      // generate static initialization
      foreach (var fields in node.ChildNodes().OfType<FieldDeclarationSyntax>())
      {
        foreach (var v in fields.Declaration.Variables)
        {
          var z = (IFieldSymbol)model.GetDeclaredSymbol(v);
          if (z.IsStatic && v.Initializer != null)
          {
            cb.AppendWithIndent("extern ")
              .Append(node.Identifier.ToString())
              .Append("::")
              .Append(v.Identifier.ToString())
              .Append(" = ")
              .Append(v.Initializer.Value.ToString())
              .AppendLine(";").AppendLine();
          }
        }
      }

      base.VisitClassDeclaration(node);
    }

    [Obsolete("Needs to be rewritten; no need to use this for field defaults.")]
    private void AppendFieldInitializers(ClassDeclarationSyntax node)
    {
      foreach (var fields in node.ChildNodes().OfType<FieldDeclarationSyntax>()
        .Where(f => f.RequiresInitialization(model)))
      {
        var vs = new List<Tuple<VariableDeclaratorSyntax, IFieldSymbol>>();
        foreach (var v in fields.Declaration.Variables)
        {
          var z = (IFieldSymbol) model.GetDeclaredSymbol(v);
          if (!z.IsStatic)
          {
            vs.Add(new Tuple<VariableDeclaratorSyntax, IFieldSymbol>(v,z));
          }
        }
        for (int i = 0; i < vs.Count; ++i)
        {
          var v = vs[i].Item1;
          var symbol = vs[i].Item2;
          cb.AppendWithIndent(v.Identifier.ToString()).Append("(");
          // if the field has an initer, use that instead
          if (v.Initializer != null)
          {
            cb.Append(v.Initializer.Value.ToString());
          }
          else
          {
            cb.Append(symbol.Type.GetDefaultValue());
          }
          cb.Append(")");
          if (i + 1 != vs.Count)
            cb.Append(",");
          cb.AppendLine();
        }
      }
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
      cb.AppendIndent()
        .Append(node.Identifier.ToString())
        .Append("::")
        .Append(node.Identifier.ToString())
        .Append("(");

      if (node.ParameterList != null)
      {
        var pars = node.ParameterList.Parameters.ToList();
        for (int i = 0; i < pars.Count; ++i)
        {
          var p = pars[i];
          var z = model.GetDeclaredSymbol(p);
          cb.Append(ArgumentTypeFor(z.Type))
            .Append(" ")
            .Append(p.Identifier.Text);
          if (i + 1 != pars.Count)
            cb.Append(", ");
        }
      }

      cb.Append(")");

      var parent = node.Parent as ClassDeclarationSyntax;
      if (parent != null && parent.HasInitializableMembers(model))
      {
        cb.AppendLine(" :");
        cb.Indent(() => AppendFieldInitializers(parent));
      }
      else
      {
        cb.AppendLine();
      }
  
      cb.AppendLineWithIndent("{");
      cb.Indent(() => base.VisitConstructorDeclaration(node));
      cb.AppendLineWithIndent("}");
    }

    public override void VisitEmptyStatement(EmptyStatementSyntax node)
    {
      cb.Append(";");
    }

    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {
      
    }

    public override string ToString()
    {
      return cb.ToString();
    }

    public override void VisitParameter(ParameterSyntax node)
    {
      // nothing here, is this correct?
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
      cb.Append(node.Identifier.Text);
    }

    public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
      Visit(node.Expression);
      cb.Append("->");
      Visit(node.Name);
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
      cb.AppendIndent();
      Visit(node.Left);

      cb.Append(" = ");

      Visit(node.Right);

      cb.AppendLine(";");
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
      cb.Append(node.ToString());
    }

    public override void VisitReturnStatement(ReturnStatementSyntax node)
    {
      cb.AppendWithIndent("return ");
      base.VisitReturnStatement(node);
      cb.AppendLine(";");
    }

    public override void VisitLiteralExpression(LiteralExpressionSyntax node)
    {
      if (node.Kind() == SyntaxKind.StringLiteralExpression)
        cb.Append("std::string(").Append(node.ToString()).Append(")");
      else 
        cb.Append(node.ToString());
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
      bool parentIsInterface = node.Parent is InterfaceDeclarationSyntax;

      var s = model.GetDeclaredSymbol(node);
      
      cb.AppendIndent();

      // check if function has template arguments
      if (s.IsGenericMethod)
      {
        cb.Append("template <");

        int len = s.TypeArguments.Length;
        for (int i = 0; i < len; ++i)
        {
          cb.Append("typename ");
          cb.Append(s.TypeArguments[i].Name);
          if (i + 1 != len)
            cb.Append(", ");
        }

        cb.Append("> ");
      }

      //cb.Append("virtual ", parentIsInterface || s.IsVirtual);
      cb.Append(node.ReturnType.ToString());
      cb.Append(" ");
      
      // get owner name, eh? this might not be a class, though
      var parent = node.Parent as ClassDeclarationSyntax;
      cb.Append(parent.Identifier.Text).Append("::");
      cb.Append(node.Identifier.ToString());
      cb.Append("(");
      var pars = node.ParameterList.Parameters;
      for (int i = 0; i < pars.Count; ++i)
      {
        var p = pars[i];
        var symbol = model.GetDeclaredSymbol(p);
        cb.Append(ArgumentTypeFor(symbol.Type)).Append(" ").Append(p.Identifier.ToString());
        if (i + 1 < pars.Count)
          cb.Append(", ");
      }
      cb.AppendLine(")"); // end of parameter block, body follows

      cb.Scope(() =>
      {
        base.VisitMethodDeclaration(node);
      });
    }
    
    
  }
}