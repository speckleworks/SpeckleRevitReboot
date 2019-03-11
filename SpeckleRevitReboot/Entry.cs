using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpeckleRevitReboot.UI;
using SpeckleUiBase;

namespace SpeckleRevitReboot
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
      var SpeckleButton = SpecklePanel.AddItem( new PushButtonData( "Speckle", "Speckle Revit", typeof( SpecklePlugin ).Assembly.Location, "SpeckleRevitReboot.SpeckleRevit" ) ) as PushButton;

      SpeckleButton.SetContextualHelp( new ContextualHelp( ContextualHelpType.Url, "https://speckle.works" ) );

      return Result.Succeeded;
    }
  }

  [Transaction( TransactionMode.Manual )]
  public class SpeckleRevit : IExternalCommand
  {
    public static bool Launched = false;
    public static SpeckleUiWindow SpeckleWindow;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      if ( !Launched )
      {
        SpeckleWindow = new SpeckleUiWindow( new SpeckleUiBindingsRevit() );
        SpeckleWindow.Show();
        Launched = true;
      }

      SpeckleWindow.Show();
      SpeckleWindow.Focus();

      return Result.Succeeded;
    }
  }

}
