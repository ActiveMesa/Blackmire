using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Blackmire
{
  public class TypeCodeBuilder
  {
    public CodeBuilder Top = new CodeBuilder();
    public CodeBuilder Private = new CodeBuilder(1);
    public CodeBuilder Protected = new CodeBuilder(1);
    public CodeBuilder Public = new CodeBuilder(1);
    public CodeBuilder Bottom = new CodeBuilder();

    public CodeBuilder GetBuilderFor(Accessibility accessibility)
    {
      switch (accessibility)
      {
        case Accessibility.NotApplicable:
          throw new Exception("visibility not applicable!");
        case Accessibility.Private:
          return Private;
        case Accessibility.ProtectedAndInternal: // requires bucketloads of friend classes?
        case Accessibility.Protected:
          return Protected;
        case Accessibility.Internal:
          break;
        case Accessibility.ProtectedOrInternal:
          return Protected;
      }
      return Public;
    }

    public override string ToString()
    {
      var sb = new StringBuilder();
      sb.Append(Top);

      var pri = Private.ToString();
      if (pri.Length > 0)
      {
        sb.AppendLine("private:");
        sb.Append(pri);
      }

      var pro = Protected.ToString();
      if (pro.Length > 0)
      {
        sb.AppendLine("protected:");
        sb.Append(pro);
      }

      var pub = Public.ToString();
      if (pub.Length > 0)
      {
        sb.AppendLine("public:");
        sb.Append(pub);
      }

      sb.Append(Bottom);
      return sb.ToString();
    }
  }
}