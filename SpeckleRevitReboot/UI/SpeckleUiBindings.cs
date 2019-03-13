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
using SpeckleRevit.ClientStorage;
using SpeckleUiBase;

namespace SpeckleRevit.UI
{
  public partial class SpeckleUiBindingsRevit : SpeckleUIBindings
  {
    public static UIApplication RevitApp;
    public static UIDocument CurrentDoc { get => RevitApp.ActiveUIDocument; }
    
    /// <summary>
    /// Stores the actions for the ExternalEvent handler
    /// </summary>
    public List<Action> Queue;

    public ExternalEvent Executor;

    /// <summary>
    /// Holds the current project's clients
    /// </summary>
    public SpeckleClientsWrapper ClientListWrapper;

    public SpeckleUiBindingsRevit( UIApplication _RevitApp ) : base()
    {
      RevitApp = _RevitApp;
      Queue = new List<Action>();
      ClientListWrapper = new SpeckleClientsWrapper();

      RevitApp.ViewActivated += RevitApp_ViewActivated;
      RevitApp.Application.DocumentChanged += Application_DocumentChanged;
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;
    }

    #region app events
    private void RevitApp_ViewActivated( object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e )
    {
      if ( GetDocHash( e.Document ) != GetDocHash( e.PreviousActiveView.Document ) )
      {
        DispatchStoreActionUi( "flushClients" );
        DispatchStoreActionUi( "getExistingClients" );
      }
    }

    private void Application_DocumentClosed( object sender, Autodesk.Revit.DB.Events.DocumentClosedEventArgs e )
    {
      DispatchStoreActionUi( "flushClients" );
    }

    private void Application_DocumentOpened( object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e )
    {
      DispatchStoreActionUi( "flushClients" );
      DispatchStoreActionUi( "getExistingClients" );
    }

    //TODO: Potential handler for detecting changes in the sender, etc.
    private void Application_DocumentChanged( object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e )
    {
      return;
    }
    #endregion

    #region client add/remove + serialisation/deserialisation
    public override void AddSender( string args )
    {
      //TODO: Add sender
    }

    /// <summary>
    /// Adds a client receiver. Does not "bake" it.
    /// </summary>
    /// <param name="args"></param>
    public override void AddReceiver( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      ClientListWrapper.clients.Add( client );

      Queue.Add( new Action( ( ) =>
      {
        using ( Transaction t = new Transaction( CurrentDoc.Document, "Adding Speckle Receiver" ) )
        {
          t.Start();
          SpeckleClientsStorage.WriteClients( CurrentDoc.Document, ClientListWrapper );
          t.Commit();
        }
      } ) );
      Executor.Raise();
    }

    /// <summary>
    /// Deletes a client, and persists the information to the file.
    /// </summary>
    /// <param name="args"></param>
    public override void RemoveClient( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var index = ClientListWrapper.clients.FindIndex( cl => cl.clientId == client.clientId );

      if ( index == -1 ) return;

      ClientListWrapper.clients.RemoveAt( index );
      Queue.Add( new Action( ( ) =>
      {
        using ( Transaction t = new Transaction( CurrentDoc.Document, "Removing Speckle Client" ) )
        {
          t.Start();
          SpeckleClientsStorage.WriteClients( CurrentDoc.Document, ClientListWrapper );
          t.Commit();
        }
      } ) );
      Executor.Raise();
    }

    /// <summary>
    /// Gets the clients stored in this file, if any.
    /// </summary>
    /// <returns></returns>
    public override string GetFileClients( )
    {
      var myReadClients = SpeckleClientsStorage.ReadClients( CurrentDoc.Document );
      if ( myReadClients == null )
        myReadClients = new SpeckleClientsWrapper();

      // Set them up in the class so we're aware of them
      ClientListWrapper = myReadClients;

      return JsonConvert.SerializeObject( myReadClients.clients );
    }

    #endregion
    
    #region Client Actions
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
    #endregion

    #region document info
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
      return GetDocHash( CurrentDoc.Document );
      // NOTE: If project is copy pasted, it has the same unique id, so the below 
      // is not reliable
      //return CurrentDoc.Document.ProjectInformation.UniqueId;
    }

    private string GetDocHash( Document doc )
    {
      return SpeckleCore.Converter.getMd5Hash( doc.PathName + doc.Title );
    }

    public override string GetDocumentLocation( )
    {
      return CurrentDoc.Document.PathName;
    }
    #endregion
  }
}
