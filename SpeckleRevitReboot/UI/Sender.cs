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
    public override void UpdateSender( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var apiClient = new SpeckleApiClient( ( string ) client.account.RestApi ) { AuthToken = ( string ) client.account.Token };

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = ( string ) client._id,
        loading = true,
        loadingBlurb = "Starting to do stuff..."
      } ) );

      var convertedObjects = new List<SpeckleObject>();

      int i = 0;
      foreach ( var obj in client.objects )
      {
        NotifyUi( "update-client", JsonConvert.SerializeObject( new
        {
          _id = ( string ) client._id,
          loading = true,
          isLoadingIndeterminate = false,
          loadingProgress = 1f * i++ / client.objects.Count * 100,
          loadingBlurb = string.Format( "Converting objects: {0} / {1}", i, client.objects.Count )
        } ) );


        var revitElement = CurrentDoc.Document.GetElement( ( string ) obj.id );
        var conversionResult = SpeckleCore.Converter.Serialise( revitElement );
        // TODO: Handle potential multiple results.
        // ie, a floor being split in two by a big fat shaft.
        convertedObjects.Add( conversionResult );
      }

      var persistedObjects = apiClient.ObjectCreateAsync( convertedObjects ).Result.Resources;
      var myStream = new SpeckleStream() { Objects = new List<SpeckleObject>() };

      myStream.Objects.AddRange( persistedObjects.Select( obj => new SpecklePlaceholder() { _id = obj._id } ) );

      var baseProps = new Dictionary<string, object>();
      baseProps[ "units" ] = CurrentDoc.Document.GetUnits().ToString();

      myStream.BaseProperties = baseProps;

      var response = apiClient.StreamUpdateAsync( ( string ) client.streamId, myStream ).Result;

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = ( string ) client._id,
        loading = false,
        loadingBlurb = "Done doing stuff..."
      } ) );
      // TODO: Everything
      //client.objects.id holds all the objects' uniqueids re revit elements
      //so then basically we, in theory, should just do:
      // SpeckleCore.Converter.Serialise( element ) and shit goes forward from there. 
      // ie, we get back our converted objects, and start sending them to the server, etc. etc. 

    }
  }
}