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
    // TODO: Orchestration
    // Create buckets, send sequentially, notify ui re upload progress
    public override void UpdateSender( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var apiClient = new SpeckleApiClient( (string) client.account.RestApi ) { AuthToken = (string) client.account.Token };

      var convertedObjects = new List<SpeckleObject>();
      var placeholders = new List<SpeckleObject>();

      int i = 0;
      long currentBucketSize = 0;

      foreach( var obj in client.objects )
      {
        NotifyUi( "update-client", JsonConvert.SerializeObject( new
        {
          _id = (string) client._id,
          loading = true,
          isLoadingIndeterminate = false,
          loadingProgress = 1f * i++ / client.objects.Count * 100,
          loadingBlurb = string.Format( "Converting and uploading objects: {0} / {1}", i, client.objects.Count )
        } ) );

        try
        {
          var revitElement = CurrentDoc.Document.GetElement( (string) obj.id );

          var conversionResult = SpeckleCore.Converter.Serialise( revitElement );
          var byteCount = Converter.getBytes( conversionResult ).Length;
          currentBucketSize += byteCount;

          if( byteCount > 2e6 )
          {
            // TODO: Handle fat objects
            var problemId = revitElement.Id;
          }

          convertedObjects.Add( conversionResult );

          if( currentBucketSize > 5e5 || i >= client.objects.Count) // aim for roughly 500kb uncompressed
          {
            LocalContext.PruneExistingObjects( convertedObjects, apiClient.BaseUrl );

            try
            {
              var chunkResponse = apiClient.ObjectCreateAsync( convertedObjects ).Result.Resources;
              int m = 0;
              foreach( var objConverted in convertedObjects )
              {
                objConverted._id = chunkResponse[ m++ ]._id;
                placeholders.Add( new SpecklePlaceholder() { _id = objConverted._id } );
                if( objConverted.Type != "Placeholder" ) LocalContext.AddSentObject( objConverted, apiClient.BaseUrl );
              }
            }
            catch( Exception e )
            {
              // TODO: Handle object creation error.
            }
            currentBucketSize = 0;
            convertedObjects = new List<SpeckleObject>(); // reset the chunkness
          }
        }
        catch( Exception e )
        {
          // TODO: Handle conversion error
        }
      }

      var myStream = new SpeckleStream() { Objects = placeholders };

      var ug = UnitUtils.GetUnitGroup( UnitType.UT_Length );
      var baseProps = new Dictionary<string, object>();

      // TODO: format units to something rational
      baseProps[ "units" ] = CurrentDoc.Document.GetUnits().ToString();
      baseProps[ "units_secondtry" ] = ug.ToString();

      myStream.BaseProperties = baseProps;

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = true,
        isLoadingIndeterminate = true,
        loadingBlurb = "Updating stream."
      } ) );

      var response = apiClient.StreamUpdateAsync( (string) client.streamId, myStream ).Result;

      NotifyUi( "update-client", JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = false,
        loadingBlurb = "Done sending."
      } ) );
    }
  }
}