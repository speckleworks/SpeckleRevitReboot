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

  }
}
