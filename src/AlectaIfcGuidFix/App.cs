using System;
using System.Reflection;
using Autodesk.Revit.UI;

namespace Alecta.Revit
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Alecta";
            string panelName = "GUID";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // Fliken finns redan
            }

            RibbonPanel panel = null;
            foreach (var p in application.GetRibbonPanels(tabName))
            {
                if (p.Name == panelName)
                {
                    panel = p;
                    break;
                }
            }

            if (panel == null)
            {
                panel = application.CreateRibbonPanel(tabName, panelName);
            }

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData(
                "cmdCleanIfcGuid",
                "Rensa IFC\nGUID",
                thisAssemblyPath,
                "Alecta.Revit.CleanIfcCommand"
            )
            {
                ToolTip = "Välj en exporterad IFC-fil och ersätt dubblerade GlobalId med unika IFC-GUID:er."
            };

            panel.AddItem(buttonData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
