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

      var previousStream = LocalState.FirstOrDefault( s => s.StreamId == ( string ) client.streamId );
      var stream = apiClient.StreamGetAsync( ( string ) client.streamId, "" ).Result.Resource;

      InjectScaleInKits( GetScale( stream ) );

      // If it's the first time we bake this stream, create a local shadow copy
      if ( previousStream == null )
      {
        previousStream = new SpeckleStream() { StreamId = stream.StreamId, Objects = new List<SpeckleObject>() };
        LocalState.Add( previousStream );
      }

      LocalContext.GetCachedObjects( stream.Objects, ( string ) client.account.RestApi );
      var payload = stream.Objects.Where( o => o.Type == "Placeholder" ).Select( obj => obj._id ).ToArray();

      // TODO: Orchestrate & save in cache afterwards!
      var objects = apiClient.ObjectGetBulkAsync( payload, "" ).Result.Resources;

      foreach ( var obj in objects )
      {
        stream.Objects[ stream.Objects.FindIndex( o => o._id == obj._id ) ] = obj;
      }

      var (toDelete, ToAddOrMod) = DiffStreamStates( previousStream, stream );

      // DELETION OF OLD OBJECTS
      if ( toDelete.Count() > 0 )
      {
        Queue.Add( new Action( ( ) =>
        {
          using ( Transaction t = new Transaction( CurrentDoc.Document, "Speckle Delete" ) )
          {
            t.Start();
            foreach ( var obj in toDelete )
            {
              var myObj = previousStream.Objects.FirstOrDefault( o => o._id == obj._id );
              if ( myObj != null )
              {
                var elem = CurrentDoc.Document.GetElement( myObj.Properties[ "revitUniqueId" ] as string );
                CurrentDoc.Document.Delete( elem.Id );
              }
            }
            t.Commit();
          }
        } ) );
        Executor.Raise();
      }

      // ADD/MOD/LEAVE ALONE EXISTING OBJECTS 
      Queue.Add( new Action( ( ) =>
      {
        using ( var t = new Transaction( CurrentDoc.Document, "Speckle Bake" ) )
        {
          t.Start();

          var tempList = new List<SpeckleObject>();
          for ( int i = 0; i < ToAddOrMod.Count; i++ )
          {
            var res = SpeckleCore.Converter.Deserialise( ToAddOrMod[ i ] );

            var myObject = new SpeckleObject() { Properties = new Dictionary<string, object>() };
            myObject._id = ToAddOrMod[ i ]._id;
            myObject.ApplicationId = ToAddOrMod[ i ].ApplicationId;
            myObject.Properties[ "revitUniqueId" ] = ( ( Element ) res ).UniqueId;
            myObject.Properties[ "revitId" ] = ( ( Element ) res ).Id.ToString();
            myObject.Properties[ "userModified" ] = false;

            tempList.Add( myObject );
          }

          previousStream.Objects = tempList;
          InjectStateInKits();
          
          // TODO: Save state in doc

          t.Commit();
        }
      } ) );

      Executor.Raise();
    }

    private (List<SpeckleObject>, List<SpeckleObject>) DiffStreamStates( SpeckleStream Old, SpeckleStream New )
    {
      var ToDelete = Old.Objects.Where( obj =>
      {
        var appIdMatch = New.Objects.FirstOrDefault( x => x.ApplicationId == obj.ApplicationId );
        var idMatch = New.Objects.FirstOrDefault( x => x._id == obj._id );
        return ( appIdMatch == null ) && ( idMatch == null );
      } ).ToList();

      var ToModOrAdd = New.Objects;
      return (ToDelete, ToModOrAdd);
    }

    private double GetScale( SpeckleStream stream )
    {
      var units = ( ( string ) stream.BaseProperties.units ).ToLower();

      // TODO: Check unit scales properly
      switch ( units )
      {
        case "kilometers":
          return 3.2808399 * 1000;

        case "meters":
          return 3.2808399;

        case "centimeters":
          return 0.032808399;

        case "millimiters":
          return 0.0032808399;

        case "miles":
          return 5280;

        case "feet":
          return 1;

        case "inches":
          return 0.0833333;

        default:
          return 3.2808399;
      };
    }

  }

}
