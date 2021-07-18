using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Kendama
{
    public class KendamaInfo : GH_AssemblyInfo
    {
        public override string Name => "Kendama";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("2188B657-DA13-4AFB-993B-B2184409588B");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}