using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
    public override void AddObjectsToSender( )
    {
      throw new NotImplementedException();
    }

    public override void AddReceiver( )
    {
      throw new NotImplementedException();
    }

    public override void AddSender( )
    {
      throw new NotImplementedException();
    }

    public override void BakeReceiver( )
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName( )
    {
      return "UI Tester";
    }

    public override void RemoveObjectsFromSender( )
    {
      throw new NotImplementedException();
    }

    public override void RemoveReceier( )
    {
      throw new NotImplementedException();
    }

    public override void RemoveSender( )
    {
      throw new NotImplementedException();
    }
  }
}
