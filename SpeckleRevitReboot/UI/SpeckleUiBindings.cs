using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleUiBase;

namespace SpeckleRevitReboot.UI
{
  public partial class SpeckleUiBindingsRevit : SpeckleUIBindings
  {

    public SpeckleUiBindingsRevit( ) : base()
    {
      // TODO: 
      // Scan the file for any existing clients and populate the MyClients list
    }

    #region clients
    public override void AddSender( string args ) { }
    public override void AddReceiver( string args ) { }
    public override void RemoveSender( string args ) { }
    public override void RemoveReceiver( string args ) { }
    #endregion

    public override void BakeReceiver( string args )
    {
      throw new NotImplementedException();
    }

    public override void AddObjectsToSender( string args )
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromSender( string args )
    {
      throw new NotImplementedException();
    }

    public override string GetFileClients( )
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName( )
    {
      return "Revit";
    }

    public override string GetFileName( )
    {
      return "Somewhere in Revit. Not implemented :)";
    }
  }
}
