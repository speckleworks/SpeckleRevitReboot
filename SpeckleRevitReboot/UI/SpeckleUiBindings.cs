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
    #region clients
    public override void AddSender( ) { }
    public override void AddReceiver( ) { }
    public override void RemoveSender( ) { }
    public override void RemoveReceier( ) { }
    #endregion

    public override void BakeReceiver( )
    {
      throw new NotImplementedException();
    }

    public override void AddObjectsToSender( )
    {
      throw new NotImplementedException();
    }

    public override void RemoveObjectsFromSender( )
    {
      throw new NotImplementedException();
    }

    public override string GetApplicationHostName( )
    {
      return "Revit";
    }
  }
}
