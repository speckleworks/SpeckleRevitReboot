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
using SpeckleCore.Data;
using SpeckleRevit.Storage;
using SpeckleUiBase;

namespace SpeckleRevit
{
  public class ErrorEater : IFailuresPreprocessor
  {
    public FailureProcessingResult PreprocessFailures( FailuresAccessor failuresAccessor )
    {
      IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
      // Inside event handler, get all warnings
      failList = failuresAccessor.GetFailureMessages();
      foreach (FailureMessageAccessor failure in failList)
      {
        // check FailureDefinitionIds against ones that you want to dismiss, 
        //FailureDefinitionId failID = failure.GetFailureDefinitionId();
        // prevent Revit from showing Unenclosed room warnings
        //if (failID == BuiltInFailures.RoomFailures.RoomNotEnclosed)
        //{
        var t = failure.GetDescriptionText();
        var r = failure.GetDefaultResolutionCaption();

        Globals.ConversionErrors.Add(new SpeckleError { Message = t });
      }

      failuresAccessor.DeleteAllWarnings();
      return FailureProcessingResult.Continue;
    }
  }
}