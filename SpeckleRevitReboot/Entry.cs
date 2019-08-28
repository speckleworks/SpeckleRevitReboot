using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpeckleRevit.UI;
using SpeckleUiBase;

// IGNORE THIS COMMENT

namespace SpeckleRevit
{
  public class SpecklePlugin : IExternalApplication
  {
    public Result OnShutdown( UIControlledApplication application )
    {
      return Result.Succeeded;
    }

    public Result OnStartup( UIControlledApplication application )
    {
      var SpecklePanel = application.CreateRibbonPanel( "Speckle" );
      var SpeckleButton = SpecklePanel.AddItem( new PushButtonData( "Speckle", "Speckle Revit", typeof( SpecklePlugin ).Assembly.Location, "SpeckleRevit.SpeckleRevitCommand" ) ) as PushButton;

      if ( SpeckleButton != null )
      {
        string path = typeof( SpecklePlugin ).Assembly.Location;
        SpeckleButton.Image = LoadPngImgSource( "SpeckleRevit.Assets.speckle16.png", path );
        SpeckleButton.LargeImage = LoadPngImgSource( "SpeckleRevit.Assets.speckle32.png", path );
        SpeckleButton.ToolTip = "Speckle";

        SpeckleButton.SetContextualHelp( new ContextualHelp( ContextualHelpType.Url, "https://speckle.works" ) );
      }

      return Result.Succeeded;
    }

    private ImageSource LoadPngImgSource( string sourceName, string path )
    {
      try
      {
        // Assembly & Stream
        var assembly = Assembly.LoadFrom( Path.Combine( path ) );
        var icon = assembly.GetManifestResourceStream( sourceName );

        // Decoder
        PngBitmapDecoder m_decoder = new PngBitmapDecoder( icon, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default );

        // Source
        ImageSource m_source = m_decoder.Frames[ 0 ];
        return ( m_source );
      }
      catch { }
      // Fail
      return null;
    }
  }

  [Transaction( TransactionMode.Manual )]
  public class SpeckleRevitCommand : IExternalCommand
  {
    public static bool Launched = false;
    public static SpeckleUiWindow SpeckleWindow;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      if ( !Launched )
      {
        // Create a new speckle binding instance
        var bindings = new SpeckleUiBindingsRevit( commandData.Application );

        // Create an external event handler to raise actions
        var eventHandler = ExternalEvent.Create( new SpeckleRevitExternalEventHandler( bindings ) );

        // Give it to our bindings so we can actually do stuff with revit
        bindings.SetExecutorAndInit( eventHandler );

        // Initialise the window
#if DEBUGLOCAL
        SpeckleWindow = new SpeckleUiWindow( bindings, @"http://10.211.55.2:8080/#/" );
#else
        SpeckleWindow = new SpeckleUiWindow( bindings ); // On release, default to the latest ci-ed version from https://appui.speckle.systems
#endif
        var helper = new System.Windows.Interop.WindowInteropHelper( SpeckleWindow );
        helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

        // TODO: find a way to set the parent/owner of the speckle window so it minimises/maximises etc. together with the revit window.
        SpeckleWindow.Show();
        Launched = true;
      }

      SpeckleWindow.Show();
      SpeckleWindow.Focus();

      return Result.Succeeded;
    }
  }

  /// <summary>
  /// Speckle custom event invoker. Has a queue of actions that, in theory, this things should iterate through. 
  /// Actions are added to the queue from the ui bindings (mostly) and then raised. 
  /// </summary>
  public class SpeckleRevitExternalEventHandler : IExternalEventHandler
  {

    public SpeckleUiBindingsRevit myBindings { get; set; }
    public bool Running = false;

    public SpeckleRevitExternalEventHandler( SpeckleUiBindingsRevit _uiBindings )
    {
      myBindings = _uiBindings;
    }

    public void Execute( UIApplication app )
    {
      Debug.WriteLine( "Current queue len is: " + myBindings.Queue.Count );
      if ( Running ) return; // queue will run itself through

      Running = true;
      try
      {
        myBindings.Queue[ 0 ]();
      }
      catch ( Exception e )
      {
        Debug.WriteLine( e.Message );
      }

      myBindings.Queue.RemoveAt( 0 );
      Running = false;

      if ( myBindings.Queue.Count != 0 )
        myBindings.Executor.Raise();
    }

    public string GetName( )
    {
      return "SpeckleSmackleSpockle";
    }
  }

}
