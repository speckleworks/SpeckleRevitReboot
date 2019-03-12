using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using SpeckleUiBase;

namespace SpeckleRevitReboot.UI
{
  public partial class SpeckleUiBindingsRevit : SpeckleUIBindings
  {

    public UIApplication RevitApp;
    public List<Action> Queue;
    public ExternalEvent Executor;

    public SpeckleUiBindingsRevit( UIApplication _RevitApp ) : base()
    {
      RevitApp = _RevitApp;
      Queue = new List<Action>();

      // TODO: 
      // Scan the file for any existing clients and populate the MyClients list
      // Set event handlers for Document loading and unloading
    }

    #region clients
    public override void AddSender( string args ) { }
    public override void AddReceiver( string args ) { }
    public override void RemoveClient( string args ) { }
    #endregion

    public override void BakeReceiver( string args )
    {
      Queue.Add( new Action( ( ) =>
      {
        Debug.WriteLine( "Should bake: " + args );
      } ) );

      Executor.Raise();
      //throw new NotImplementedException();
    }

    public override void AddObjectsToSender( string args )
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromSender( string args )
    {
      throw new NotImplementedException();
    }

    public override string GetFileClients( )
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName( )
    {
      return "Revit";
    }

    public override string GetFileName( )
    {
      return "Somewhere in Revit. Not implemented :)";
    }

    public override string GetDocumentId( )
    {
      return "testing";
    }

    public override string GetDocumentLocation( )
    {
      return "where youve put it";
    }
  }
}
