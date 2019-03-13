using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using SpeckleRevitReboot.ClientStorage;
using SpeckleUiBase;

namespace SpeckleRevitReboot.UI
{
  public partial class SpeckleUiBindingsRevit : SpeckleUIBindings
  {

    public UIApplication RevitApp;
    public UIDocument CurrentDoc { get => RevitApp.ActiveUIDocument; }
    public List<Action> Queue;
    public ExternalEvent Executor;

    public SpeckleClientsWrapper myClientList;

    public SpeckleUiBindingsRevit( UIApplication _RevitApp ) : base()
    {
      RevitApp = _RevitApp;
      Queue = new List<Action>();
      myClientList = new SpeckleClientsWrapper();
      // TODO: 
      // Scan the file for any existing clients and populate the MyClients list
      // Set event handlers for Document loading and unloading
    }

    #region clients
    public override void AddSender( string args )
    {
      //TODO
    }

    public override void AddReceiver( string args )
    {
      //TODO
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      myClientList.clients.Add( client );

      Queue.Add( new Action( ( ) =>
      {

        using ( Transaction t = new Transaction( CurrentDoc.Document, "adding receiver" ) )
        {
          t.Start();
          SpeckleClientsStorage.WriteClients( CurrentDoc.Document, myClientList );
          t.Commit();
        }

      } ) );
      Executor.Raise();
    }

    public override void RemoveClient( string args ) { }
    #endregion

    public override void BakeReceiver( string args )
    {
      Queue.Add( new Action( ( ) =>
      {
        Debug.WriteLine( "Should bake client: " + args );
      } ) );

      Executor.Raise();
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
      var myReadClients = SpeckleClientsStorage.ReadClients( CurrentDoc.Document );
      if ( myReadClients == null )
        myReadClients = new SpeckleClientsWrapper();
      return JsonConvert.SerializeObject( myReadClients.clients );
    }

    public override string GetApplicationHostName( )
    {
      return "Revit";
    }

    public override string GetFileName( )
    {
      return CurrentDoc.Document.Title;
    }

    public override string GetDocumentId( )
    {
      // TODO: return proper id?
      return CurrentDoc.Document.PathName + CurrentDoc.Document.Title;
    }

    public override string GetDocumentLocation( )
    {
      return CurrentDoc.Document.PathName;
    }
  }
}
