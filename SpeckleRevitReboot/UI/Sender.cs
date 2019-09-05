using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using SpeckleCore;
using SpeckleRevit.Storage;
using SpeckleUiBase;

namespace SpeckleRevit.UI
{
  public partial class SpeckleUiBindingsRevit
  {
    public override void AddSender(string args)
    {
      var client = JsonConvert.DeserializeObject<dynamic>(args);
      ClientListWrapper.clients.Add(client);

      // TODO: Add stream to LocalState (do we actually need to??? hm...).
      var myStream = new SpeckleStream() { StreamId = (string)client.streamId, Objects = new List<SpeckleObject>() };

      //foreach( dynamic obj in client.objects )
      //{
      //  var SpkObj = new SpeckleObject() { };
      //  SpkObj.Properties[ "revitUniqueId" ] = obj.id.ToString();
      //  SpkObj.Properties[ "__type" ] = "Sent Object";
      //  myStream.Objects.Add( SpkObj );
      //}

      LocalState.Add(myStream);

      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Adding Speckle Sender"))
        {
          t.Start();
          SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
          SpeckleClientsStorageManager.WriteClients(CurrentDoc.Document, ClientListWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();

      AddSelectionToSender(args);
    }

    public override void UpdateSender(string args)
    {
      var client = JsonConvert.DeserializeObject<dynamic>(args);
      var index = ClientListWrapper.clients.FindIndex( cl => (string) cl._id == (string) client._id );
      ClientListWrapper.clients[index] = client;

      var myStream = LocalState.FirstOrDefault(st => st.StreamId == (string) client.streamId);
      myStream.Name = (string)client.name;

      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Update Speckle Sender"))
        {
          t.Start();
          SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
          SpeckleClientsStorageManager.WriteClients(CurrentDoc.Document, ClientListWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();

      AddSelectionToSender(args);
    }

    // NOTE: This is actually triggered when clicking "Push!"
    // TODO: Orchestration
    // Create buckets, send sequentially, notify ui re upload progress
    // NOTE: Problems with local context and cache: we seem to not sucesffuly pass through it
    // perhaps we're not storing the right sent object (localcontext.addsentobject)
    public override void PushSender( string args )
    {
      var client =  JsonConvert.DeserializeObject<dynamic>( args );

      //if it's a category or property filter we need to refresh the list of objects
      //if it's a selection filter just use the objects that were stored previously
      ISelectionFilter filter = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(client.filter), GetFilterType(client.filter.Type.ToString()));
      IEnumerable<SpeckleObject> objects = new List<SpeckleObject>();
      if (filter.Name != "Selection")
        objects = GetSelectionFilterObjects(filter, client._id.ToString(), client.streamId.ToString());
      else
        objects = client.objects;

      var apiClient = new SpeckleApiClient( (string) client.account.RestApi ) { AuthToken = (string) client.account.Token };

      var convertedObjects = new List<SpeckleObject>();
      var placeholders = new List<SpeckleObject>();

      var units = CurrentDoc.Document.GetUnits().GetFormatOptions( UnitType.UT_Length ).DisplayUnits.ToString().ToLower().Replace( "dut_", "" );
      InjectScaleInKits( GetScale( units ) ); // this is used for feet to sane units conversion.

      int i = 0;
      long currentBucketSize = 0;
      var errors = "";
      var failedSend = 0;
      var failedConvert = 0;
      foreach( var obj in objects)
      {
        NotifyUi( "update-client",  JsonConvert.SerializeObject( new
        {
          _id = (string) client._id,
          loading = true,
          isLoadingIndeterminate = false,
          loadingProgress = 1f * i++ / objects.Count() * 100,
          loadingBlurb = string.Format( "Converting and uploading objects: {0} / {1}", i, objects.Count() )
        } ) );

        try
        {
          var revitElement = CurrentDoc.Document.GetElement( (string) obj.Properties[ "revitUniqueId" ] );

          var conversionResult = SpeckleCore.Converter.Serialise( new List<object>() { revitElement } );
          var byteCount = Converter.getBytes( conversionResult ).Length;
          currentBucketSize += byteCount;

          if( byteCount > 2e6 )
          {
            var problemId = revitElement.Id;
          }

          convertedObjects.AddRange( conversionResult );

          if( currentBucketSize > 5e5 || i >= objects.Count() ) // aim for roughly 500kb uncompressed
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
              failedSend += convertedObjects.Count;
              NotifyUi( "update-client",  JsonConvert.SerializeObject( new
              {
                _id = (string) client._id,
                errors = "Failed to send " + failedSend + " objects."
              } ) );
            }
            currentBucketSize = 0;
            convertedObjects = new List<SpeckleObject>(); // reset the chunkness
          }
        }
        catch( Exception e )
        {
          failedConvert++;
          NotifyUi( "update-client",  JsonConvert.SerializeObject( new
          {
            _id = (string) client._id,
            errors = "Failed to convert " + failedConvert + " objects."
          } ) );
        }
      }

      if( failedConvert > 0 ) errors += String.Format( "Failed to convert a total of {0} objects. ", failedConvert );
      if( failedSend > 0 ) errors += String.Format( "Failed to send a total of {0} objects. ", failedSend );

      var myStream = new SpeckleStream() { Objects = placeholders };

      var ug = UnitUtils.GetUnitGroup( UnitType.UT_Length );
      var baseProps = new Dictionary<string, object>();

      baseProps[ "units" ] = units;

      baseProps[ "unitsDictionary" ] = GetAndClearUnitDictionary();

      myStream.BaseProperties = baseProps;
      //myStream.BaseProperties =  JsonConvert.SerializeObject(baseProps);

      NotifyUi( "update-client",  JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = true,
        isLoadingIndeterminate = true,
        loadingBlurb = "Updating stream."
      } ) );

      var response = apiClient.StreamUpdateAsync( (string) client.streamId, myStream ).Result;

      NotifyUi( "update-client",  JsonConvert.SerializeObject( new
      {
        _id = (string) client._id,
        loading = false,
        loadingBlurb = "Done sending.",
        errors
      } ) );

    }

    public override void AddSelectionToSender( string args )
    {
      var client =  JsonConvert.DeserializeObject<dynamic>( args );
      
      ISelectionFilter filter = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(client.filter), GetFilterType(client.filter.Type.ToString()));

      GetSelectionFilterObjects(filter, client._id.ToString(), client.streamId.ToString());
    }

    public override void RemoveSelectionFromSender( string args )
    {
      //var client =  JsonConvert.DeserializeObject<dynamic>( args );
      //var myStream = LocalState.FirstOrDefault( st => st.StreamId == (string) client.streamId );
      //var myClient = ClientListWrapper.clients.FirstOrDefault( cl => (string) cl._id == (string) client._id );

      //var selectionIds = CurrentDoc.Selection.GetElementIds().Select( id => CurrentDoc.Document.GetElement( id ).UniqueId );
      //var removed = 0;
      //foreach( var revitUniqueId in selectionIds )
      //{
      //  var index = myStream.Objects.FindIndex( o => (string) o.Properties[ "revitUniqueId" ] == revitUniqueId );
      //  if( index == -1 ) continue;
      //  myStream.Objects.RemoveAt( index );
      //  removed++;
      //}

      //myClient.objects =  JsonConvert.DeserializeObject<dynamic>(  JsonConvert.SerializeObject( myStream.Objects ) );

      //// Persist state and clients to revit file
      //Queue.Add( new Action( () =>
      //{
      //  using( Transaction t = new Transaction( CurrentDoc.Document, "Adding Speckle Receiver" ) )
      //  {
      //    t.Start();
      //    SpeckleStateManager.WriteState( CurrentDoc.Document, LocalState );
      //    SpeckleClientsStorageManager.WriteClients( CurrentDoc.Document, ClientListWrapper );
      //    t.Commit();
      //  }
      //} ) );
      //Executor.Raise();

      //if( removed != 0 )
      //  NotifyUi( "update-client",  JsonConvert.SerializeObject( new
      //  {
      //    _id = client._id,
      //    expired = true,
      //    objects = myClient.objects,
      //    message = String.Format( "You have removed {0} objects from this sender.", removed )
      //  } ) );
      throw new NotImplementedException();
    }

    #region private methods

    private Type GetFilterType(string typeString)
    {
      Assembly ass = typeof(ISelectionFilter).Assembly;
      return ass.GetType(typeString);
    }

    /// <summary>
    /// Given the filter in use by a stream returns the document elements that match it
    /// </summary>
    /// <returns></returns>
    private IEnumerable<SpeckleObject> GetSelectionFilterObjects(ISelectionFilter filter, string clientId, string streamId)
    {
      var doc = CurrentDoc.Document;
      IEnumerable<SpeckleObject> objects = new List<SpeckleObject>();

      var selectionIds = new List<string>();

      if (filter.Name == "Selection")
      {
        selectionIds = CurrentDoc.Selection.GetElementIds().Select(id => doc.GetElement(id).UniqueId).ToList();
      }
      else if (filter.Name == "Category")
      {
        var catFilter = filter as ListSelectionFilter;
        var bics = new List<BuiltInCategory>();
        var categories = Globals.GetCategories(doc);
        IList<ElementFilter> elementFilters = new List<ElementFilter>();

        foreach (var cat in catFilter.Selection)
        {
          elementFilters.Add(new ElementCategoryFilter(categories[cat].Id));
        }
        LogicalOrFilter categoryFilter = new LogicalOrFilter(elementFilters);

        selectionIds = new FilteredElementCollector(doc)
          .WhereElementIsNotElementType()
          .WherePasses(categoryFilter)
          .Select(x=>x.UniqueId).ToList();

      }
      else if (filter.Name == "Property")
      {
        try
        {
          var propFilter = filter as PropertySelectionFilter;
          selectionIds = new FilteredElementCollector(doc)
            .WhereElementIsNotElementType()
            .Where(fi => fi.LookupParameter(propFilter.PropertyName) !=null && fi.LookupParameter(propFilter.PropertyName).AsValueString() == propFilter.PropertyValue).Select(x => x.UniqueId).ToList();
        }
        catch { }
      }

      // LOCAL STATE management
      objects = selectionIds.Select(id =>
      {
        var temp = new SpeckleObject();
        temp.Properties["revitUniqueId"] = id;
        temp.Properties["__type"] = "Sent Object";
        return temp;
      });


      var myStream = LocalState.FirstOrDefault(st => st.StreamId == streamId);

      myStream.Objects.Clear();
      myStream.Objects.AddRange(objects);

      var myClient = ClientListWrapper.clients.FirstOrDefault(cl => (string)cl._id == (string)clientId);
      myClient.objects = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(myStream.Objects));

      // Persist state and clients to revit file
      Queue.Add(new Action(() =>
      {
        using (Transaction t = new Transaction(CurrentDoc.Document, "Update local storage"))
        {
          t.Start();
          SpeckleStateManager.WriteState(CurrentDoc.Document, LocalState);
          SpeckleClientsStorageManager.WriteClients(CurrentDoc.Document, ClientListWrapper);
          t.Commit();
        }
      }));
      Executor.Raise();
      var plural = objects.Count() == 1 ? "" : "s";
      if (objects.Count() != 0)
        NotifyUi("update-client", JsonConvert.SerializeObject(new
        {
          _id = clientId,
          expired = true,
          objects = myClient.objects,
          message = $"You have added {objects.Count()} object{plural} to this sender."
        }));

      return objects;
    }
    #endregion

  }
}