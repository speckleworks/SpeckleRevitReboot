using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json;

namespace SpeckleUiBase
{
  public abstract class SpeckleUIBindings
  {
    public ChromiumWebBrowser Browser { get; set; }
    public List<dynamic> myClients;

    public SpeckleUIBindings( )
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
      myClients = new List<dynamic>();
    }

    public void NotifyUi( dynamic eventInfo )
    {
      var script = string.Format( "window.EventBus.$emit({0})", JsonConvert.SerializeObject( eventInfo ) );
      Browser.GetMainFrame().EvaluateScriptAsync( script );
    }

    public void ShowDev( )
    {
      Browser.ShowDevTools();
    }

    public string GetAccounts( )
    {
      return JsonConvert.SerializeObject( SpeckleCore.LocalContext.GetAllAccounts() );
    }

    public abstract string GetApplicationHostName( );

    public abstract string GetFileName( );
    public abstract string GetDocumentId( );
    public abstract string GetDocumentLocation( );

    public abstract string GetFileClients( );

    /// <summary>
    /// TODO: Adds a sender and persits the info to the host file
    /// </summary>
    public abstract void AddSender( string args );
    /// <summary>
    /// TODO: Adds a receiver and persits the info to the host file
    /// </summary>
    public abstract void AddReceiver( string args );
    /// <summary>
    /// TODO
    /// </summary>
    public abstract void RemoveSender( string args );
    /// <summary>
    /// TODO
    /// </summary>
    public abstract void RemoveReceiver( string args );

    public abstract void BakeReceiver( string args );
    public abstract void AddObjectsToSender( string args );
    public abstract void RemoveObjectsFromSender( string args );
  }

  public class ClientWrapper
  {
    public dynamic Account { get; set; }
    public dynamic Stream { get; set; }
  }
}
