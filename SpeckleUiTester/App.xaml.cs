using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using SpeckleUiBase;

namespace SpeckleUiTester
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    private void Application_Startup( object sender, StartupEventArgs e )
    {
      var speckleUiWindow = new SpeckleUiWindow( new TestBindings() );
      speckleUiWindow.Show();
    }
  }

  /// <summary>
  /// Test bindings class, void
  /// </summary>
  public class TestBindings : SpeckleUIBindings
  {

    public TestBindings( ) : base()
    {
    }

    public override void AddObjectsToSender( string args )
    {
      throw new NotImplementedException();
    }

    public override void AddReceiver( string _args )
    {
      dynamic args = JsonConvert.DeserializeObject( _args );
      //var copy = args;
      myClients.Add( args );
    }

    public override void AddSender( string args )
    {
      throw new NotImplementedException();
    }

    public override void BakeReceiver( string args )
    {
      //throw new NotImplementedException();
    }

    public override string GetApplicationHostName( )
    {
      return "UI Tester";
    }

    public override string GetFileName( )
    {
      return "Somewhere in Memory. Not implemented :)";
    }

    public override string GetDocumentId( )
    {
      return "In memory testing!";
    }

    public override string GetDocumentLocation( )
    {
      return "RAM or SWAP";
    }


    public override string GetFileClients( )
    {
      return JsonConvert.SerializeObject( myClients );
    }


    public override void RemoveObjectsFromSender( string args )
    {
      throw new NotImplementedException();
    }

    public override void RemoveReceiver( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      try
      {
        var index = myClients.FindIndex( acc => acc._id == client._id );
        myClients.RemoveAt( index );
      }
      catch ( Exception e ) { }
    }

    public override void RemoveSender( string args )
    {
      throw new NotImplementedException();
    }
  }

}
