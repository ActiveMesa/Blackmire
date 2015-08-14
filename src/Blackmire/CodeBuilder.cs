using System;
using System.Diagnostics;
using System.Text;

namespace Blackmire
{
  public class CodeBuilder
  {
    StringBuilder sb = new StringBuilder();

    private const int IndentSize = 2;
    private int indent;

    public CodeBuilder(int indent = 0)
    {
      this.indent = indent;
    }

    public CodeBuilder AppendIndent()
    {
      sb.Append(string.Empty.PadRight(indent*IndentSize));
      return this;
    }

    public CodeBuilder AppendLine()
    {
      sb.AppendLine();
      return this;
    }

    public CodeBuilder AppendLineWithIndent(string value = "")
    {
      AppendIndent();
      return AppendLine(value);
    }

    public CodeBuilder AppendLine(string value)
    {
      sb.AppendLine(value);
      return this;
    }

    public CodeBuilder AppendWithIndent(string value)
    {
      AppendIndent();
      return Append(value);
    }

    public CodeBuilder Append(string value)
    {
      sb.Append(value);
      return this;
    }

    public CodeBuilder Append(string value, bool condition)
    {
      if (condition) Append(value);
      return this;
    }

    public override string ToString()
    {
      return sb.ToString();
    }

    public void Scope(Action a)
    {
      AppendLineWithIndent("{");
      indent++;
      a();
      indent--;
      AppendLineWithIndent("}");
    }

    public void Indent(Action a = null)
    {
      if (a == null) indent++;
      else
      {
        indent++;
        a();
        indent--;
      }
    }

    public int IndentValue
    {
      get { return indent; }
    }

    public void Unindent()
    {
      Debug.Assert(indent > 0);
      indent--;
    }
  }
}