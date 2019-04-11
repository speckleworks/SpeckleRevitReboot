using System;
using System.Collections.Generic;
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
    public override void UpdateSender(string args)
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var apiClient = new SpeckleApiClient( ( string ) client.account.RestApi ) { AuthToken = ( string ) client.account.Token };

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = ( string ) client._id,
        loading = true,
        loadingBlurb = "Starting to do stuff..."
      } ) );

    }
  }
}
