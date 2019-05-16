using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleRevit.Storage;

namespace SpeckleRevit.UI
{
  public partial class SpeckleUiBindingsRevit
  {
    // TODO: Orchestration
    // Create buckets, send sequentially, notify ui re upload progress
    // NOTE: Problems with local context and cache: we seem to not sucesffuly pass through it
    // perhaps we're not storing the right sent object (localcontext.addsentobject)
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
          var revitElement = CurrentDoc.Document.GetElement( (string) obj.properties[ "revitUniqueId" ] );

          var conversionResult = SpeckleCore.Converter.Serialise( revitElement );
          var byteCount = Converter.getBytes( conversionResult ).Length;
          currentBucketSize += byteCount;

          if( byteCount > 2e6 )
          {
            // TODO: Handle fat objects
            var problemId = revitElement.Id;
          }

          convertedObjects.Add( conversionResult );

          if( currentBucketSize > 5e5 || i >= client.objects.Count ) // aim for roughly 500kb uncompressed
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

    public override void AddSelectionToSender( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );

      var selectionIds = CurrentDoc.Selection.GetElementIds().Select( id => CurrentDoc.Document.GetElement( id ).UniqueId );

      // LOCAL STATE management
      var spkObjectsToAdd = selectionIds.Select( id =>
      {
        var temp = new SpeckleObject();
        temp.Properties[ "revitUniqueId" ] = id;
        temp.Properties[ "__type" ] = "Sent Object";
        return temp;
      } );

      var myStream = LocalState.FirstOrDefault( st => st.StreamId == (string) client.streamId );
      var added = 0;
      foreach( var obj in spkObjectsToAdd )
      {
        var ind = myStream.Objects.FindIndex( o => (string) o.Properties[ "revitUniqueId" ] == (string) obj.Properties[ "revitUniqueId" ] );
        if( ind == -1 )
        {
          myStream.Objects.Add( obj );
          added++;
        }
      }

      var myClient = ClientListWrapper.clients.FirstOrDefault( cl => (string) cl._id == (string) client._id );
      myClient.objects = JsonConvert.DeserializeObject<dynamic>( JsonConvert.SerializeObject( myStream.Objects ) );

      // Persist state and clients to revit file
      Queue.Add( new Action( () =>
      {
        using( Transaction t = new Transaction( CurrentDoc.Document, "Adding Speckle Receiver" ) )
        {
          t.Start();
          SpeckleStateManager.WriteState( CurrentDoc.Document, LocalState );
          SpeckleClientsStorageManager.WriteClients( CurrentDoc.Document, ClientListWrapper );
          t.Commit();
        }
      } ) );
      Executor.Raise();

      if( added != 0 )
        NotifyUi( "update-client", JsonConvert.SerializeObject( new
        {
          _id = client._id,
          expired = true,
          objects = myClient.objects,
          message = String.Format( "You have added {0} objects from this sender.", added )
        } ) );
      //throw new NotImplementedException();
    }

    public override void RemoveSelectionFromSender( string args )
    {
      var client = JsonConvert.DeserializeObject<dynamic>( args );
      var myStream = LocalState.FirstOrDefault( st => st.StreamId == (string) client.streamId );
      var myClient = ClientListWrapper.clients.FirstOrDefault( cl => (string) cl._id == (string) client._id );

      var selectionIds = CurrentDoc.Selection.GetElementIds().Select( id => CurrentDoc.Document.GetElement( id ).UniqueId );
      var removed = 0;
      foreach( var revitUniqueId in selectionIds )
      {
        var index = myStream.Objects.FindIndex( o => (string) o.Properties[ "revitUniqueId" ] == revitUniqueId );
        if( index == -1 ) continue;
        myStream.Objects.RemoveAt( index );
        removed++;
      }

      myClient.objects = JsonConvert.DeserializeObject<dynamic>( JsonConvert.SerializeObject( myStream.Objects ) );

      // Persist state and clients to revit file
      Queue.Add( new Action( () =>
      {
        using( Transaction t = new Transaction( CurrentDoc.Document, "Adding Speckle Receiver" ) )
        {
          t.Start();
          SpeckleStateManager.WriteState( CurrentDoc.Document, LocalState );
          SpeckleClientsStorageManager.WriteClients( CurrentDoc.Document, ClientListWrapper );
          t.Commit();
        }
      } ) );
      Executor.Raise();

      if( removed != 0 )
        NotifyUi( "update-client", JsonConvert.SerializeObject( new
        {
          _id = client._id,
          expired = true,
          objects = myClient.objects,
          message = String.Format( "You have removed {0} objects from this sender.", removed )
        } ) );
    }
  }
}