using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Win32;

namespace Blackmire
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();

      InputBox.Text = @"namespace Foo.Bar
{

class Person
{
  int ssn;

  public string Name { get; private set; }

  void foo()
  {
    int x = 0;
  }

}

enum Color
{
  Red,
  Green,
  LightBlue
}

class Color<T>
{
  static Color<T> Red = new Color<T>();
  public T a,r,g,b;
}

}
";
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
      var ofd = new OpenFileDialog();
      if (ofd.ShowDialog() ?? false)
      {
        var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(ofd.FileName);
        ProcessSolution(solution);
      }
    }

    private async void ProcessSolution(Solution solution)
    {
      foreach (var project in solution.Projects.Where(p => p.Language.Equals("C#")))
      {
        foreach (var doc in project.Documents)
        {
          var tree = await doc.GetSyntaxTreeAsync();
          var root = (CompilationUnitSyntax)tree.GetRoot();
          
          foreach (var e in root.Members)
          {
            var nds = e as NamespaceDeclarationSyntax;
            if (nds != null)
            {
              
            }
          }
        }
      }
    }

    private void InputChanged(object sender, TextChangedEventArgs e)
    {
#if RELEASE
      try
      {
#endif
        ProcessInput(sender);
#if RELEASE
      }
      catch (Exception ex)
      {
        HeaderBox.Text = "Sorry, we cannot process your input (yet)." + Environment.NewLine +
                         "Please check that the C# input is valid.";
        CppBox.Text = ex.ToString();
      }
#endif
    }

    private void ProcessInput(object sender)
    {
      var tree = CSharpSyntaxTree.ParseText(((TextBox) sender).Text);
      var compilation = CSharpCompilation.Create("blackmire")
        .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
        .WithReferences(MetadataReference.CreateFromFile(typeof (Int32).Assembly.Location))
        .AddSyntaxTrees(tree);
      var model = compilation.GetSemanticModel(tree, true);
      var settings = new ConversionSettings();
      var hw = new CppHeaderWalker(compilation, model, settings);
      hw.Visit(tree.GetRoot());
      HeaderBox.Text = hw.ToString();

      var iw = new CppImplWalker(compilation, model, settings);
      iw.Visit(tree.GetRoot());
      CppBox.Text = iw.ToString();
    }

    private void InputDrop(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
        if (files != null && files.Any())
        {
          var theFile = files[0];
          ((TextBox) sender).Text = File.ReadAllText(theFile);
        }
      }
    }

    private void InputDragOver(object sender, DragEventArgs e)
    {
      e.Handled = true;
    }
  }
}
