using System.ComponentModel;

namespace Blackmire
{
  public class ConversionSettings
  {
    public string GetPrefix = "Get";
    public string SetPrefix = "Set";
    public PropertyGenerationStyle PropertyGenerationStyle = PropertyGenerationStyle.GettersAndSetters;
  }

  public enum PropertyGenerationStyle
  {
    [Description("Getters and Setters")]
    GettersAndSetters,
    [Description("__declspec(property)")]
    DeclspecProperty
  }
}