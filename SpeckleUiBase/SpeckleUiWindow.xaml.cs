using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CefSharp;

namespace SpeckleUiBase
{
  /// <summary>
  /// Interaction logic for SpeckleUiWindow.xaml
  /// </summary>
  public partial class SpeckleUiWindow : Window
  {
    public SpeckleUiWindow( SpeckleUIBindings baseBindings )
    {
      InitializeComponent();

      baseBindings.Browser = Browser;

      Browser.RegisterAsyncJsObject( "UiBindings", baseBindings );

      Browser.Address = @"http://10.211.55.2:8080/";
    }

    // Note: Dynamo ships with cefsharp too, so we need to be careful around initialising cefsharp.
    private void InitializeCef( )
    {
      if ( Cef.IsInitialized ) return;

      Cef.EnableHighDPISupport();

      var assemblyLocation = Assembly.GetExecutingAssembly().Location;
      var assemblyPath = System.IO.Path.GetDirectoryName( assemblyLocation );
      var pathSubprocess = System.IO.Path.Combine( assemblyPath, "CefSharp.BrowserSubprocess.exe" );
      var settings = new CefSettings
      {
        BrowserSubprocessPath = pathSubprocess
      };

      Cef.Initialize( settings );
    }

    // Hides the window rather than closing it, to prevent the browser from going haywire.
    protected override void OnClosing( CancelEventArgs e )
    {
      this.Hide();
      e.Cancel = true;
    }
  }
}
