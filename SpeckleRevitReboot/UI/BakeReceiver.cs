using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using SpeckleCore;

namespace SpeckleRevit.UI
{
  public partial class SpeckleUiBindingsRevit
  {
    public override void BakeReceiver( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var apiClient = new SpeckleApiClient( ( string ) client.account.RestApi ) { AuthToken = ( string ) client.account.Token };

      SpeckleStream stream = apiClient.StreamGetAsync( ( string ) client.streamId, "" ).Result.Resource;
      LocalContext.GetCachedObjects( stream.Objects, ( string ) client.account.RestApi );

      var payload = stream.Objects.Where( o => o.Type == "Placeholder" ).Select( obj => obj._id ).ToArray();

      Queue.Add( new Action( ( ) =>
      {
        var cl = new SpeckleApiClient();
        cl.BaseUrl = "https://hestia.speckle.works/api";
        var response = cl.ObjectGetAsync( "5c8a68b0ea7270430b9e064c" ).Result;
        var obj = response.Resource;

        using ( var t = new Transaction( CurrentDoc.Document, "Spk Grid" ) )
        {
          t.Start();
          SpeckleCore.Converter.Deserialise( obj );
          t.Commit();
        }

        Debug.WriteLine( "Should bake client: " + args );
      } ) );
      Executor.Raise();
    }
  }
}
