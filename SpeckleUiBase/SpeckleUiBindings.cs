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

    public SpeckleUIBindings( )
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
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

    /// <summary>
    /// TODO: Adds a sender and persits the info to the host file
    /// </summary>
    public abstract void AddSender( );
    /// <summary>
    /// TODO: Adds a receiver and persits the info to the host file
    /// </summary>
    public abstract void AddReceiver( );
    /// <summary>
    /// TODO
    /// </summary>
    public abstract void RemoveSender( );
    /// <summary>
    /// TODO
    /// </summary>
    public abstract void RemoveReceier( );

    public abstract void BakeReceiver( );
    public abstract void AddObjectsToSender( );
    public abstract void RemoveObjectsFromSender( );
  }
}
