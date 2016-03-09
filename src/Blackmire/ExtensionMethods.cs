using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blackmire
{
  public static class ExtensionMethods
  {
    public static bool HasInitializableMembers(this ClassDeclarationSyntax c, SemanticModel sm)
    {
      return false; // todo: figure out how to detect this.x=x type initializers; ignore for now
      return c.ChildNodes().OfType<FieldDeclarationSyntax>()
        .Any(f => f.RequiresInitialization(sm));
    }

    public static bool RequiresInitialization(this FieldDeclarationSyntax f, SemanticModel sm)
    {
      foreach (var field in f.Declaration.Variables)
      {
        var s = (IFieldSymbol)sm.GetDeclaredSymbol(field);
        if (!s.IsStatic && !s.Type.IsReferenceType)
          return true;
      }
      return false;
    }

    public static bool HasDefaultConstructor(this ClassDeclarationSyntax c)
    {
      return c.ChildNodes().OfType<ConstructorDeclarationSyntax>()
        .Any(s => !s.ParameterList.Parameters.Any());
    }

    public static string KnownGenericType(this string type)
    {
      var parts = type.Split('`');
      if (parts.Length < 2) return type;
      switch (parts[0])
      {
        case "List":
          return "std::vector";
        case "Dictionary":
          return "std::map";
        case "Nullable":
          return "boost::optional";
        default:
          return parts[0]; // maybe user-defined or something
      }
    }

    public static bool Some<T>(this IEnumerable<T> self)
    {
      return self != null && !self.Any();
    }

    /// <summary>
    /// Returns the default C++ value for a specific .NET type.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string GetDefaultValue(this ITypeSymbol s)
    {
      switch (s.TypeKind)
      {
        case TypeKind.Unknown:
          break;
        case TypeKind.Array:
        {
          return null;
            return "nullptr"; // not strictly correct
        }
        case TypeKind.Class:
          break;
        case TypeKind.Delegate:
          break;
        case TypeKind.Dynamic:
          break;
        case TypeKind.Enum:
          break;
        case TypeKind.Error:
          break;
        case TypeKind.Interface:
          break;
        case TypeKind.Module:
          break;
        case TypeKind.Pointer:
          break;
        case TypeKind.Struct:
          break;
        case TypeKind.TypeParameter:
          break;
        case TypeKind.Submission:
          break;
      }

      switch (s.SpecialType)
      {
        case SpecialType.None:
          break;
        case SpecialType.System_Object:
          return "void*";
        case SpecialType.System_Enum:
          break;
        case SpecialType.System_MulticastDelegate:
          break;
        case SpecialType.System_Delegate:
          break;
        case SpecialType.System_ValueType:
          break;
        case SpecialType.System_Void:
          break;
        case SpecialType.System_Boolean:
          break;
        case SpecialType.System_Char:
          return "''";
        case SpecialType.System_SByte:
          break;
        case SpecialType.System_Byte:
          break;
        case SpecialType.System_Int16:
          break;
        case SpecialType.System_UInt16:
          break;
        case SpecialType.System_Int32:
          return "0";
        case SpecialType.System_UInt32:
          break;
        case SpecialType.System_Int64:
          break;
        case SpecialType.System_UInt64:
          break;
        case SpecialType.System_Decimal:
          break;
        case SpecialType.System_Single:
          return "0.0f";
        case SpecialType.System_Double:
          return "0.0";
        case SpecialType.System_String:
          return null;
        case SpecialType.System_IntPtr:
          return "nullptr";
        case SpecialType.System_UIntPtr:
          return "nullptr";
        case SpecialType.System_Array:
          return "nullptr";
        case SpecialType.System_Collections_IEnumerable:
          break;
        case SpecialType.System_Collections_Generic_IEnumerable_T:
          break;
        case SpecialType.System_Collections_Generic_IList_T:
          break;
        case SpecialType.System_Collections_Generic_ICollection_T:
          break;
        case SpecialType.System_Collections_IEnumerator:
          break;
        case SpecialType.System_Collections_Generic_IEnumerator_T:
          break;
        case SpecialType.System_Collections_Generic_IReadOnlyList_T:
          break;
        case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
          break;
        case SpecialType.System_Nullable_T:
          break;
        case SpecialType.System_DateTime:
          break;
        case SpecialType.System_Runtime_CompilerServices_IsVolatile:
          break;
        case SpecialType.System_IDisposable:
          break;
        case SpecialType.System_TypedReference:
          break;
        case SpecialType.System_ArgIterator:
          break;
        case SpecialType.System_RuntimeArgumentHandle:
          break;
        case SpecialType.System_RuntimeFieldHandle:
          break;
        case SpecialType.System_RuntimeMethodHandle:
          break;
        case SpecialType.System_RuntimeTypeHandle:
          break;
        case SpecialType.System_IAsyncResult:
          break;
        case SpecialType.System_AsyncCallback:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return null;
    }

    public static string ToCppType(this ITypeSymbol s)
    {
      switch (s.TypeKind)
      {
        case TypeKind.Array:
          {
            var a = s as IArrayTypeSymbol;
            if (a != null)
              return $"std::vector<{a.ElementType.ToCppType()}>";
            else
            {
              // no idea what kind of an array this is
              return "std::vector<boost::any>";
            }
          }
      }

      switch (s.SpecialType)
      {
        case SpecialType.None:
          {
            string metaName = s.MetadataName;
            string knownGeneric = metaName.KnownGenericType();
            if (knownGeneric != metaName)
            {
              // this is a generic type, so...
              var sb = new StringBuilder();
              sb.Append(knownGeneric).Append("<");

              var nts = s as INamedTypeSymbol;
              if (nts != null)
              {
                for (int i = 0; i < nts.TypeArguments.Length; ++i)
                {
                  var arg = nts.TypeArguments[i];
                  sb.Append(arg.ToCppType());
                  if (i + 1 != nts.TypeArguments.Length)
                    sb.Append(", ");
                }
              }

              sb.Append(">");
              return sb.ToString();
            }
            if (s.IsReferenceType)
            {
              switch (s.MetadataName)
              {
                case "DateTime":
                  return "boost::date";
                case "NullPointerException":
                  return "std::invalid_argument";
                case "StringBuilder":
                  return "std::ostringstream";
                case "ArrayList":
                  return "std::vector<boost::any>"; // highly inefficient
                default:
                  return $"std::shared_ptr<{metaName}>";
              }

            }
            return metaName;
          }
        case SpecialType.System_Object:
          break;
        case SpecialType.System_Enum:
          break;
        case SpecialType.System_MulticastDelegate:
          break;
        case SpecialType.System_Delegate:
          break;
        case SpecialType.System_ValueType:
          break;
        case SpecialType.System_Void:
          return "void";
        case SpecialType.System_Boolean:
          return "bool";
        case SpecialType.System_Char:
          return "char";
        case SpecialType.System_SByte:
          return "int8_t";
        case SpecialType.System_Byte:
          return "uint8_t";
        case SpecialType.System_Int16:
          return "int16_t";
        case SpecialType.System_UInt16:
          return "uint16_t";
        case SpecialType.System_Int32:
          return "int32_t";
        case SpecialType.System_UInt32:
          return "uint32_t";
        case SpecialType.System_Int64:
          return "int64_t";
        case SpecialType.System_UInt64:
          return "uint64_t";
        case SpecialType.System_Decimal:
          return "/* Decimal types not supported */ double";
        case SpecialType.System_Single:
          return "float";
        case SpecialType.System_Double:
          return "double";
        case SpecialType.System_String:
          return "std::string";
        case SpecialType.System_IntPtr:
          return "int *";
        case SpecialType.System_UIntPtr:
          return "unsigned int *";
        case SpecialType.System_Array:
          break;
        case SpecialType.System_Collections_IEnumerable:
          break;
        case SpecialType.System_Collections_Generic_IEnumerable_T:
          break;
        case SpecialType.System_Collections_Generic_IList_T:
          break;
        case SpecialType.System_Collections_Generic_ICollection_T:
          break;
        case SpecialType.System_Collections_IEnumerator:
          break;
        case SpecialType.System_Collections_Generic_IEnumerator_T:
          break;
        case SpecialType.System_Collections_Generic_IReadOnlyList_T:
          break;
        case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
          break;
        case SpecialType.System_Nullable_T:
          return "boost::optional<>";
        case SpecialType.System_DateTime:
          break;
        case SpecialType.System_Runtime_CompilerServices_IsVolatile:
          break;
        case SpecialType.System_IDisposable:
          break;
        case SpecialType.System_TypedReference:
          break;
        case SpecialType.System_ArgIterator:
          break;
        case SpecialType.System_RuntimeArgumentHandle:
          break;
        case SpecialType.System_RuntimeFieldHandle:
          break;
        case SpecialType.System_RuntimeMethodHandle:
          break;
        case SpecialType.System_RuntimeTypeHandle:
          break;
        case SpecialType.System_IAsyncResult:
          break;
        case SpecialType.System_AsyncCallback:
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
      return s.Name;
    }
  }
}