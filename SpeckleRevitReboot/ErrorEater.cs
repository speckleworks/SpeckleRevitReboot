using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SpeckleCore;
using SpeckleRevit.Storage;
using SpeckleUiBase;

namespace SpeckleRevit
{
  public class ErrorEater : IFailuresPreprocessor
  {
    // TODO: gracefully save them somewhere, or do something about them - collect them and show them in the ui somehow?
    public FailureProcessingResult PreprocessFailures( FailuresAccessor failuresAccessor )
    {
      //var fails = failuresAccessor.GetFailureMessages();

      //foreach(var f in fails)
      //{
      //}

      failuresAccessor.DeleteAllWarnings();
      return FailureProcessingResult.Continue;
    }
  }
}