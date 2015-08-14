﻿using System;
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
        .WithReferences(MetadataReference.CreateFromAssembly(typeof (System.Int32).Assembly))
        .AddSyntaxTrees(tree);
      var model = compilation.GetSemanticModel(tree);
      var settings = new ConversionSettings();
      var hw = new CppHeaderWalker(compilation, model, settings);
      hw.Visit(tree.GetRoot());
      HeaderBox.Text = hw.ToString();

      var iw = new CppImplWalker(compilation, model, settings);
      iw.Visit(tree.GetRoot());
      CppBox.Text = iw.ToString();
    }
  }
}
