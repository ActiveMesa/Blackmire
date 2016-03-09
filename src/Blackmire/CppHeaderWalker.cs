using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackmire
{
  public class CppHeaderWalker : CppWalker
  {
    private readonly CodeBuilder cb = new CodeBuilder();
    /// <summary>
    /// Initialized to some default to prevent stupid errors.
    /// </summary>
    private TypeCodeBuilder tcb = new TypeCodeBuilder();

    public enum CppVisibility
    {
      Private,
      Protected,
      Public
    }

    private Dictionary<ClassDeclarationSyntax, CppVisibility> lastVisibility =
      new Dictionary<ClassDeclarationSyntax, CppVisibility>();

    public override void VisitUsingDirective(UsingDirectiveSyntax node)
    {

      // these could theoretically be acquired by indexing the GAC or something
      var namespaces = new List<String>
      {
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.Text",
        "System.Linq"
      };
      string whatAreWeUsing = node.Name.ToString();
      if (!namespaces.Contains(whatAreWeUsing))
      {
        var parts = whatAreWeUsing.Split('.');
        cb.AppendIndent().Append("using namespace ").Append(string.Join("::", parts)).AppendLine(";");
      }
    }

    public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
      var name = node.Identifier.Text;
      cb.Append("enum class ").AppendLine(name)
        .Scope(() =>
        {
          base.VisitEnumDeclaration(node);
        });

      // now let's generate ostream& operator <<
      cb.AppendLine("std::ostream& operator<<(std::ostream& os, const " + name + " obj)")
        .Scope(() =>
        {
          cb.AppendLineWithIndent("switch (obj)")
            .Scope(() =>
            {
              foreach (var enumCase in node.Members)
              {
                string caseName = enumCase.Identifier.Text;
                // todo: string output needs to parse camelCase => "camel case"
                cb.AppendWithIndent("case " + name + "::" + caseName + ": ")
                  .AppendLine("os << \"" + Regex.Replace(caseName, @"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))", " $1").ToLowerInvariant() + "\"; break;");
              }
            });
          cb.AppendLineWithIndent("return os;");
        });
    }

    public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
    {
      cb.AppendWithIndent(node.Identifier.Text).AppendLine(",");
    }

    public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
      var parts = node.Name.ToString().Split('.');
      cb.AppendIndent();
      foreach (var part in parts)
      {
        cb.Append("namespace ").Append(part).Append(" { ");
      }
      cb.AppendLine();

      base.VisitNamespaceDeclaration(node);

      cb.AppendIndent();
      foreach (var part in parts)
        cb.Append("} /* " + part + "*/ ");
      cb.AppendLine();
    }

    public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
      if (node.Body != null && node.Parent is BasePropertyDeclarationSyntax)
      {
        var z = model.GetDeclaredSymbol(node.Parent as PropertyDeclarationSyntax);

        cb.AppendWithIndent(z.Type.ToCppType())
          .Append(" ")
          .Append(settings.GetPrefix)
          .Append(z.Name)
          .AppendLine("() const");
      }
      base.VisitAccessorDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
      bool parentIsInterface = node.Parent is InterfaceDeclarationSyntax;

      var s = model.GetDeclaredSymbol(node);
      var builder = tcb.GetBuilderFor(s.DeclaredAccessibility);

      builder.AppendIndent();

      // check if function has template arguments
      if (s.IsGenericMethod)
      {
        builder.Append("template <");

        int len = s.TypeArguments.Length;
        for (int i = 0; i < len; ++i)
        {
          builder.Append("typename ");
          builder.Append(s.TypeArguments[i].Name);
          if (i + 1 != len)
            builder.Append(", ");
        }

        builder.Append("> ");
      }

      builder.Append("virtual ", parentIsInterface || s.IsVirtual);
      builder.Append("static ", s.IsStatic);

      builder.Append(node.ReturnType.ToString());
      builder.Append(" ");
      builder.Append(node.Identifier.ToString());
      builder.Append("(");
      var pars = node.ParameterList.Parameters;
      for (int i = 0; i < pars.Count; ++i)
      {
        var p = pars[i];
        var symbol = model.GetDeclaredSymbol(p);
        builder.Append(ArgumentTypeFor(symbol.Type)).Append(" ").Append(p.Identifier.ToString());
        if (i + 1 < pars.Count)
          builder.Append(", ");
      }
      builder.Append(")");
      builder.Append(" = 0", parentIsInterface);
      builder.AppendLine(";");
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
      // todo: ensure this is same visibility
      var z = model.GetDeclaredSymbol(node);

      var builder = tcb.GetBuilderFor(z.DeclaredAccessibility);

      builder.AppendWithIndent("__declspec(property(");
      CodeBuilder getBuilder = null, setBuilder = null;
      if (z.GetMethod != null)
      {
        builder.Append("get=Get").Append(z.Name);

        getBuilder = new CodeBuilder(builder.IndentValue);
        getBuilder.AppendWithIndent(z.Type.ToCppType())
          .Append(" ") // return by value
          .Append("Get")
          .Append(z.Name)
          .AppendLine("() const;");
      }
      if (z.SetMethod != null)
      {
        if (z.GetMethod != null) builder.Append(",");
        builder.Append("put=Set").Append(z.Name);

        setBuilder = new CodeBuilder(builder.IndentValue);
        setBuilder.AppendWithIndent("void ")
          .Append("Set")
          .Append(z.Name)
          .Append("(")
          .Append(z.Type.ToCppType())
          .AppendLine(");");
      }

      builder.Append(")) ")
        .Append(z.Type.ToCppType())
        .Append(" ")
        .Append(z.Name)
        .AppendLine(";");

      if (getBuilder != null)
        builder.Append(getBuilder.ToString());

      if (setBuilder != null)
        builder.Append(setBuilder.ToString());
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
      VisitTypeDeclaration(node);
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
      VisitTypeDeclaration(node);
    }

    private void VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
      tcb = new TypeCodeBuilder();
      tcb.Top.AppendIndent();

      if (node.TypeParameterList != null &&
            node.TypeParameterList.Parameters.Count > 0)
      {
        tcb.Top.Append("template <");
        var ps = node.TypeParameterList.Parameters;
        for (int i = 0; i < ps.Count; ++i)
        {
          var p = ps[i];
          tcb.Top.Append("typename ").Append(p.Identifier.ToString());
          if (i + 1 != ps.Count)
            tcb.Top.Append(", ");
        }
        tcb.Top.Append("> ");
      }

      tcb.Top.Append("class " + node.Identifier);

      // here comes the tricky part: base class declarations
      if (node.BaseList != null)
      {
        var types = node.BaseList.Types;
        for (int i = 0; i < types.Count; ++i)
        {
          var t = types[i];
          if (i == 0) tcb.Top.Append(" : ");
          tcb.Top.Append("public ").Append(t.Type.ToString());
          if (i + 1 != types.Count)
            tcb.Top.Append(", ");
        }
      }

      tcb.Top.AppendLine(" { ");

      if (node is ClassDeclarationSyntax)
      {
        var cds = (ClassDeclarationSyntax)node;
        base.VisitClassDeclaration(cds);

        if (cds.HasInitializableMembers(model) && !cds.HasDefaultConstructor())
        {
          tcb.Public.AppendLineWithIndent(cds.Identifier + "();");
        }
      }
      else if (node is InterfaceDeclarationSyntax)
      {
        var ids = (InterfaceDeclarationSyntax)node;
        base.VisitInterfaceDeclaration(ids);
      }
      tcb.Bottom.AppendLineWithIndent("};");

      // flush tcb to cb
      cb.Append(tcb.ToString());
    }

    public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
      foreach (var v in node.Declaration.Variables)
      {
        var z = model.GetDeclaredSymbol(v) as IFieldSymbol;
        if (z != null)
        {
          var builder = tcb.GetBuilderFor(z.DeclaredAccessibility);
          var cppType = z.Type.ToCppType();
          builder.AppendIndent();
          if (z.IsConst || z.IsReadOnly) builder.Append("const ");
          if (z.IsStatic) builder.Append("static ");
          builder.Append(cppType).Append(" ").Append(v.Identifier.ToString());

          // since this is C++11, we can add init for primitive numeric types
          var defType = z.Type.GetDefaultValue();
          if (defType != null)
          {
            builder.Append(" = ").Append(z.Type.GetDefaultValue());
          }

          builder.AppendLine(";");
        }
      }
    }

    public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
      var z = model.GetDeclaredSymbol(node);
      var builder = tcb.GetBuilderFor(z.DeclaredAccessibility);
      builder.AppendWithIndent(node.Identifier.Text).Append("(");
      var pars = node.ParameterList.Parameters.ToList();
      for (int i = 0; i < pars.Count; ++i)
      {
        var p = pars[i];
        var c = model.GetDeclaredSymbol(p);
        builder.Append(ArgumentTypeFor(c.Type)).Append(" ").Append(p.Identifier.ToString());
        if (i + 1 < pars.Count)
          builder.Append(", ");
      }
      builder.AppendLine(");");
      base.VisitConstructorDeclaration(node);
    }

    public CppHeaderWalker(CSharpCompilation compilation, SemanticModel model, ConversionSettings settings)
      : base(compilation, model, settings)
    {
    }

    public override string ToString()
    {
      return cb.ToString();
    }
  }
}