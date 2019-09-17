using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleRevit
{
  public static class Globals
  {

    private static Dictionary<string, Category> _categories { get; set; }

    public static Dictionary<string, Category> GetCategories(Document doc)
    {
      if (_categories == null)
      {
        _categories = new Dictionary<string, Category>();
        foreach (Category category in doc.Settings.Categories)
        {
          _categories.Add(category.Name, category);
        }
      }
      return _categories;
    }

    public static List<string> GetCategoryNames(Document doc)
    {
      return GetCategories(doc).Keys.OrderBy(x => x).ToList();
    }

    public static List<string> GetParameterNames(Document doc)
    {
      var els = new FilteredElementCollector(doc)
      .WhereElementIsNotElementType()
      .WhereElementIsViewIndependent()
      .ToElements();

      List<string> parameters = new List<string>();

      foreach(var e in els)
      {
        foreach (Parameter p in e.Parameters)
        {
          if (!parameters.Contains(p.Definition.Name))
            parameters.Add(p.Definition.Name);
        }
      }
      parameters = parameters.OrderBy(x => x).ToList();
      return parameters;
    }

  }
}
