#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Windows.Markup;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion

namespace ArchilizerTinyTools
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            // 1. Create ribbon tab
            string tabName = "Revit Testing";
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }

            //// 2. Create ribbon panel 
            RibbonPanel panel = Utils.CreateRibbonPanel(app, tabName, "Archilizer");

            //// 3. Create button data instances
            //// 4. Create buttons
            PushButtonData btnData1 = Cmd_ViewToSheets.GetButtonData();
            PushButton myButton1 = panel.AddItem(btnData1) as PushButton;

            PushButtonData btnData2 = Cmd_CreateKeyPlanRegions.GetButtonData();
            PushButton myButton2 = panel.AddItem(btnData2) as PushButton;

            PushButtonData btnData3 = Cmd_SheetRegionVisibility.GetButtonData();
            PushButton myButton3 = panel.AddItem(btnData3) as PushButton;

            // NOTE:
            // To create a new tool, copy lines 35 and 39 and rename the variables to "btnData3" and "myButton3". 
            // Change the name of the tool in the arguments of line 

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }


    }
}
