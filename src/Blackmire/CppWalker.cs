using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Blackmire
{
  public abstract class CppWalker : CSharpSyntaxWalker
  {
    protected readonly CSharpCompilation compilation;
    protected readonly SemanticModel model;
    protected readonly ConversionSettings settings;

    protected CppWalker(CSharpCompilation compilation, SemanticModel model, ConversionSettings settings)
    {
      if (compilation == null) throw new ArgumentNullException("compilation");
      if (model == null) throw new ArgumentNullException("model");
      if (settings == null) throw new ArgumentNullException("settings");
      this.compilation = compilation;
      this.model = model;
      this.settings = settings;
    }

    /// <summary>
    /// Implementors need to return entire text contents.
    /// </summary>
    /// <returns></returns>
    public abstract override string ToString();

    /// <summary>
    /// Given a particular type, this creates an argument type. For example, for a type
    /// <c>int</c> the argument type is also <c>int</c>, for a type
    /// <c>System.String</c> the argument type is <c>const string&amp;</c>, etc.
    /// </summary>
    protected string ArgumentTypeFor(ITypeSymbol type)
    {
      string s = type.ToCppType();

      // only strings, nullables and specials are byref
      switch (type.SpecialType)
      {
        case SpecialType.System_String:
        case SpecialType.System_Nullable_T:
        case SpecialType.None:
          return "const " + s + "&";
        default:
          return s;
      }
    }
  }
}