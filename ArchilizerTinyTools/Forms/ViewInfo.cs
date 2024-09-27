using Autodesk.Revit.DB;

namespace ArchilizerTinyTools.Forms
{
    /// <summary>
    /// Represents information about a view.
    /// </summary>
    public class ViewInfo
    {
        /// <summary>
        /// Gets or sets the name of the view.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Gets or sets the type of the view.
        /// </summary>
        public ViewType ViewType { get; set; } = ViewType.Undefined;

        /// <summary>
        /// Gets or sets the ID of the view.
        /// </summary>
        public ElementId Id { get; set; } = null;

        /// <summary>
        /// Gets or sets the sheet number associated with the view.
        /// </summary>
        public string SheetNumber { get; set; } = null;

        /// <summary>
        /// Gets or sets the sheet name associated with the view.
        /// </summary>
        public string SheetName { get; set; } = null;

        /// <summary>
        /// Gets or sets the reason for the view.
        /// </summary>
        public string Reason { get; set; } = null;

        // Basic constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewInfo"/> class with the specified name, view type, and ID.
        /// </summary>
        /// <param name="name">The name of the view.</param>
        /// <param name="viewType">The type of the view.</param>
        /// <param name="id">The ID of the view.</param>
        public ViewInfo(string name, ViewType viewType, ElementId id)
        {
            Name = name;
            ViewType = viewType;
            Id = id;
        }

        // Constructor chaining (calling the base constructor)
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewInfo"/> class with the specified name, view type, ID, sheet number, and sheet name.
        /// </summary>
        /// <param name="name">The name of the view.</param>
        /// <param name="viewType">The type of the view.</param>
        /// <param name="id">The ID of the view.</param>
        /// <param name="sheetNumber">The sheet number associated with the view.</param>
        /// <param name="sheetName">The sheet name associated with the view.</param>
        public ViewInfo(string name, ViewType viewType, ElementId id, string sheetNumber, string sheetName)
            : this(name, viewType, id)
        {
            SheetNumber = sheetNumber;
            SheetName = sheetName;
        }

        // Constructor chaining with reason
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewInfo"/> class with the specified name, view type, ID, sheet number, sheet name, and reason.
        /// </summary>
        /// <param name="name">The name of the view.</param>
        /// <param name="viewType">The type of the view.</param>
        /// <param name="id">The ID of the view.</param>
        /// <param name="sheetNumber">The sheet number associated with the view.</param>
        /// <param name="sheetName">The sheet name associated with the view.</param>
        /// <param name="reason">The reason for the view.</param>
        public ViewInfo(string name, ViewType viewType, ElementId id, string sheetNumber, string sheetName, string reason)
            : this(name, viewType, id, sheetNumber, sheetName)
        {
            Reason = reason;
        }

        // Override the ToString method to return a string representation of the object
        /// <summary>
        /// Returns a string that represents the current <see cref="ViewInfo"/> object.
        /// </summary>
        /// <returns>A string that represents the current <see cref="ViewInfo"/> object.</returns>
        public override string ToString()
        {
            return $"{Name} ({ViewType}) - Sheet: {SheetNumber}, {SheetName}, Reason: {Reason}";
        }
    }

    //public class ViewInfo
    //{
    //    public string Name { get; set; }
    //    public ViewType ViewType { get; set; }
    //    public ElementId Id { get; set; }
    //    public string SheetNumber { get; set; }
    //    public string SheetName { get; set; }
    //    public string Reason { get; set; }
    //    public ViewInfo(string name, ViewType viewType, ElementId id)
    //    {
    //        Name = name;
    //        ViewType = viewType;
    //        Id = id;
    //    }

    //    // View Type, Sheet Number,Sheet Name,View Name
    //    public ViewInfo(string name, ViewType viewType, ElementId id, string sheetNumber, string sheetName)
    //    {
    //        Name = name;
    //        ViewType = viewType;
    //        Id = id;
    //        SheetNumber = sheetNumber;
    //        SheetName = sheetName;
    //    }

    //    // View Type, Sheet Number,Sheet Name,View Name,REASON
    //    public ViewInfo(string name, ViewType viewType, ElementId id, string sheetNumber, string sheetName, string reason)
    //    {
    //        Name = name;
    //        ViewType = viewType;
    //        Id = id;
    //        SheetNumber = sheetNumber;
    //        SheetName = sheetName;
    //        Reason = reason;
    //    }
    //}
}