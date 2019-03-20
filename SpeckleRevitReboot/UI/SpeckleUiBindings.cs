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
using SpeckleCore;
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

    public List<SpeckleStream> LocalState;

    public SpeckleUiBindingsRevit( UIApplication _RevitApp ) : base()
    {
      RevitApp = _RevitApp;
      Queue = new List<Action>();

      ClientListWrapper = new SpeckleClientsWrapper();
      LocalState = new List<SpeckleStream>();
      // TODO: read local state from file

      InjectRevitAppInKits();
      InjectStateInKits();

      RevitApp.ViewActivated += RevitApp_ViewActivated;
      RevitApp.Application.DocumentChanged += Application_DocumentChanged;
      RevitApp.Application.DocumentOpened += Application_DocumentOpened;
      RevitApp.Application.DocumentClosed += Application_DocumentClosed;
    }

    #region Kit injection utils

    /// <summary>
    /// Injects the Revit app in any speckle kit Initialiser class that has a 'RevitApp' property defined. This is need for creating revit elements from that assembly without having hard references on the ui library.
    /// </summary>
    public void InjectRevitAppInKits( )
    {
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies();
      foreach ( var ass in assemblies )
      {
        var types = ass.GetTypes();
        foreach ( var type in types )
        {
          if ( type.GetInterfaces().Contains( typeof( SpeckleCore.ISpeckleInitializer ) ) )
          {
            if ( type.GetProperties().Select( p => p.Name ).Contains( "RevitApp" ) )
            {
              type.GetProperty( "RevitApp" ).SetValue( null, RevitApp );
            }
          }
        }
      }
    }

    /// <summary>
    /// Injects the current lolcal state in any speckle kit initialiser class that has a "LocalRevitState" property defined. 
    /// This can then be used to determine what existing speckle baked objects exist in the current doc and either modify/delete whatever them in the conversion methods.
    /// </summary>
    public void InjectStateInKits( )
    {
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies();
      foreach ( var ass in assemblies )
      {
        var types = ass.GetTypes();
        foreach ( var type in types )
        {
          if ( type.GetInterfaces().Contains( typeof( SpeckleCore.ISpeckleInitializer ) ) )
          {
            if ( type.GetProperties().Select( p => p.Name ).Contains( "LocalRevitState" ) )
            {
              List<SpeckleStream> xxx = LocalState.Select( x => x ).ToList();
              type.GetProperty( "LocalRevitState" ).SetValue( null, xxx );
            }
          }
        }
      }
    }

    /// <summary>
    /// Injects a scale property to be used in conversion methods if needed.
    /// </summary>
    public void InjectScaleInKits( double scale )
    {
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies();
      foreach ( var ass in assemblies )
      {
        var types = ass.GetTypes();
        foreach ( var type in types )
        {
          if ( type.GetInterfaces().Contains( typeof( SpeckleCore.ISpeckleInitializer ) ) )
          {
            if ( type.GetProperties().Select( p => p.Name ).Contains( "RevitScale" ) )
            {
              type.GetProperty( "RevitScale" ).SetValue( null, scale );
            }
          }
        }
      }
    }

    #endregion

    #region app events
    private void RevitApp_ViewActivated( object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e )
    {
      if ( GetDocHash( e.Document ) != GetDocHash( e.PreviousActiveView.Document ) )
      {
        DispatchStoreActionUi( "flushClients" );
        DispatchStoreActionUi( "getExistingClients" );
        // TODO: Switch current local state to document
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
      // TODO: Get current local state from document
    }

    //TODO: Potential handler for detecting changes in the sender, etc.
    private void Application_DocumentChanged( object sender, Autodesk.Revit.DB.Events.DocumentChangedEventArgs e )
    {
      var transactionNames = e.GetTransactionNames();

      if ( transactionNames.Contains( "Speckle Bake" ) || transactionNames.Contains( "Speckle Delete" ) ) return;

      // TODO: Mark as modified the above elements in LocalState IF the transaction name is not speckle bake or speckle delete
      var modified = e.GetModifiedElementIds();
      var allStateObjects = ( from p in LocalState.SelectMany( s => s.Objects ) select p ).ToList();

      foreach ( var id in modified )
      {
        var elUniqueId = CurrentDoc.Document.GetElement( id ).UniqueId;
        var found = allStateObjects.FirstOrDefault( o => ( string ) o.Properties[ "revitUniqueId" ] == elUniqueId );
        if ( found != null ) found.Properties[ "userModified" ] = true;
      }
      return;
    }

    public void MarkAsModified( ) { }

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
