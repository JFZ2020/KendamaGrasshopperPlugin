using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Kendama
{
    public class KendamaComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public KendamaComponent()
          : base("Kendama", "Nickname",
            "Create Kendamas",
            "Kendama", "Main")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Heights","H","All Kendama heights",GH_ParamAccess.list);
            pManager.AddNumberParameter("Diameters","D","All Kendama diameters",GH_ParamAccess.list);
            pManager.AddNumberParameter("TamaDiameter","TD","Tama diameter",GH_ParamAccess.item);
            pManager.AddNumberParameter("BigCupDiameter","BCD","Big cup diameter",GH_ParamAccess.item);
            pManager.AddNumberParameter("SmallCupDiameter","SCD","Small cup diameter",GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("out", "out", "Solver report", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Ken", "K", "Ken part", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Tama", "T", "Tama part", GH_ParamAccess.item);
            pManager.AddGeometryParameter("Sarada", "S", "Sarada part", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var heights = new List<double>();
            var radiuses = new List<double>();
            double tamaRadius = 30;
            double bigCupRadius = 20;
            double smallCupRadius = 15;


            if(!DA.GetDataList(0, heights)) { return; }
            if (!DA.GetDataList(1, radiuses)) { return; }
            if (!DA.GetData(2, ref tamaRadius)) { return; }
            if (!DA.GetData(3, ref bigCupRadius)) { return; }
            if (!DA.GetData(4, ref smallCupRadius)) { return; }

            if (tamaRadius < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tama diameter cannot be 0 or less.");
                return;
            }

            var ken = new Object();
            var tama = new Object();
            var sarada = new Object();
            // SOLVER
            RunScript(heights, radiuses, tamaRadius, bigCupRadius, smallCupRadius,ref ken,ref tama, ref sarada);

            // SOLVER

            //DA.SetData(0, stats);
            DA.SetData(0, ken);
            DA.SetData(1, tama);
            DA.SetData(2, sarada);
            }


        private void RunScript(List<double> heights, List<double> radius, double tamaRadius, double saradaBigCupRadius, double saradaSmallCupRadius, ref object A, ref object B, ref object C)
        {
            double saradaMidRadius = 15;
            double saradaToTop = 2 * tamaRadius;
            double tamaBaseRadius = 15;
            double reduceEdgeRadius = 2.5;
            double reduceEdgeDistance = 7.5;





            #region Ken


            List<Rhino.Geometry.Point3d> points = new List<Rhino.Geometry.Point3d>();
            double increase = 0;
            foreach (var height in heights)
            {
                increase = increase + height;
                Point3d point = new Rhino.Geometry.Point3d(0, 0, increase);
                points.Add(point);
            }

            List<Rhino.Geometry.Circle> circles = new List<Rhino.Geometry.Circle>();
            for (var i = 0; i < points.Count; i++)
            {
                Circle circle = new Rhino.Geometry.Circle(points[i], radius[i]);
                circles.Add(circle);
            }

            List<Curve> crvs = new List<Curve>();
            foreach (Circle c in circles)
                crvs.Add(new ArcCurve(c));


            Brep[] bottomSurface = Brep.CreatePlanarBreps(crvs[0], 0.1);
            Brep[] topSurface = Brep.CreatePlanarBreps(crvs[crvs.Count - 1], 0.1);
            Brep[] brep = Brep.CreateFromLoft(crvs, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
            //D = brep;
            List<Rhino.Geometry.Brep> breps = new List<Rhino.Geometry.Brep>();
            breps.AddRange(brep);
            breps.AddRange(bottomSurface);
            breps.AddRange(topSurface);

            var Ken = Brep.MergeBreps(breps, 0.1);

            var topZ = circles[circles.Count - 1].Plane.Origin.Z;
            var saradaZ = topZ - saradaToTop;
            Point3d saradaPoint = new Point3d(0, 0, saradaZ);
            Plane planeSaradaXY = new Plane(saradaPoint, new Vector3d(0, 0, 1));
            Point3d saradaBigCupEdgePoint = new Point3d(saradaPoint.X - saradaBigCupRadius - reduceEdgeDistance, saradaPoint.Y, saradaPoint.Z);
            Point3d saradaSmallCupEdgePoint = new Point3d(saradaPoint.X + saradaSmallCupRadius + reduceEdgeDistance, saradaPoint.Y, saradaPoint.Z);

            Curve[] intersectionCurves;
            Point3d[] intersectionPoints;
            Rhino.Geometry.Intersect.Intersection.BrepPlane(Ken, planeSaradaXY, 0.1, out intersectionCurves, out intersectionPoints);

            var intersectCurve = Rhino.Geometry.Intersect.Intersection.CurvePlane(intersectionCurves[0], new Plane(new Point3d(0, 0, saradaZ), new Vector3d(1, 0, 0)), 0.1);
            Point3d intersectionPoint = intersectCurve[1].PointA;
            Point3d intersectionPoint2 = intersectCurve[0].PointA;
            var distance = intersectionPoint.DistanceTo(intersectionPoint2);
            Sphere sphereKen = new Rhino.Geometry.Sphere(intersectionPoint, 2);
            Brep brepSphereKen = Brep.CreateFromSphere(sphereKen);

            List<Rhino.Geometry.Brep> brepsSphereKen = new List<Rhino.Geometry.Brep>() { brepSphereKen, Ken };


            brep = Rhino.Geometry.Brep.CreateBooleanUnion(brepsSphereKen, 0.1);
            var ken = Brep.MergeBreps(brep, 0.1);

            Plane cylinderBasePlane = new Rhino.Geometry.Plane(intersectionPoint, new Vector3d(0, 1, 0));
            var cylinderBase = new Rhino.Geometry.Circle(cylinderBasePlane, 1);

            var cylinder = new Rhino.Geometry.Cylinder(cylinderBase, -distance - 1);
            Brep brepCylinderKen = Brep.CreateFromCylinder(cylinder, false, false);

            List<Rhino.Geometry.Brep> brepsCylinderKen = new List<Rhino.Geometry.Brep>() { ken, brepCylinderKen };
            brep = Rhino.Geometry.Brep.CreateBooleanUnion(brepsCylinderKen, 0.1);
            ken = Brep.MergeBreps(brep, 0.1);


            A = ken;


            #endregion





            #region Tama

            Sphere sphereTama = new Rhino.Geometry.Sphere(Point3d.Origin, tamaRadius);
            //var moveToBase = Rhino.Geometry.Transform.Translation(new Vector3d(0, 0, tamaRadius));
            //var moveToSarada = Rhino.Geometry.Transform.Translation(new Vector3d(0, 0, saradaPoint.Z));
            var brepTama = sphereTama.ToBrep();

            var cylinderBaseTama = new Rhino.Geometry.Circle(Plane.WorldXY, tamaBaseRadius);


            var cylinderTama = new Rhino.Geometry.Cylinder(cylinderBaseTama, -tamaRadius);
            Brep brepCylinderTama = Brep.CreateFromCylinder(cylinderTama, false, false);
            var tamaBaseSplit = brepTama.DuplicateBrep().Split(brepCylinderTama, 0.1);
            var brepTamaBase = tamaBaseSplit[1];
            var tamaBaseBB = brepTamaBase.GetBoundingBox(true);
            var tamaBaseClosestPoint = tamaBaseBB.ClosestPoint(Point3d.Origin);
            Sphere sphereTamaBase = new Rhino.Geometry.Sphere(tamaBaseClosestPoint, tamaBaseRadius);
            Brep brepSphereTamaBase = Brep.CreateFromSphere(sphereTamaBase);
            var brepsTama = Rhino.Geometry.Brep.CreateBooleanDifference(brepTama, brepSphereTamaBase, 0.1);
            var tama = Brep.MergeBreps(brepsTama, 0.1);



            Plane tamaCylinderPlane = new Plane(tamaBaseClosestPoint, Vector3d.ZAxis);
            var tamaCylinderBase = new Rhino.Geometry.Circle(tamaCylinderPlane, distance / 2);

            var tamaCylinder = new Rhino.Geometry.Cylinder(tamaCylinderBase, saradaToTop - (saradaMidRadius));
            brepCylinderTama = Brep.CreateFromCylinder(tamaCylinder, false, true);

            var tamaBrep = Rhino.Geometry.Brep.CreateBooleanDifference(tama, brepCylinderTama, 0.1);
            tama = Brep.MergeBreps(tamaBrep, 0.1);


            tamaCylinderPlane = new Plane(tamaBaseClosestPoint, Vector3d.ZAxis);
            tamaCylinderBase = new Rhino.Geometry.Circle(tamaCylinderPlane, 1);

            tamaCylinder = new Rhino.Geometry.Cylinder(tamaCylinderBase, tamaRadius * 2);
            brepCylinderTama = Brep.CreateFromCylinder(tamaCylinder, false, true);

            tamaBrep = Rhino.Geometry.Brep.CreateBooleanDifference(tama, brepCylinderTama, 0.1);
            tama = Brep.MergeBreps(tamaBrep, 0.1);

            B = tama;



            Plane cylinderBaseBigCupPlane = new Rhino.Geometry.Plane(Point3d.Origin, new Vector3d(1, 0, 0));
            var cylinderBaseBigCup = new Rhino.Geometry.Circle(cylinderBaseBigCupPlane, saradaBigCupRadius - reduceEdgeRadius);

            var cylinderBigCup = new Rhino.Geometry.Cylinder(cylinderBaseBigCup, tamaRadius);
            Brep brepCylinderBigCup = Brep.CreateFromCylinder(cylinderBigCup, false, false);
            var bigCupSarada = brepTama.Split(brepCylinderBigCup, 0.1);
            var brepBigCupSarada = bigCupSarada[1];

            var bigCupBB = brepBigCupSarada.GetBoundingBox(true);
            var bigCupBaseClosestPoint = bigCupBB.ClosestPoint(Point3d.Origin);

            var bigCupMoveToSarada = Rhino.Geometry.Transform.Translation(new Vector3d(saradaBigCupEdgePoint.X - bigCupBaseClosestPoint.X, saradaBigCupEdgePoint.Y - bigCupBaseClosestPoint.Y, saradaBigCupEdgePoint.Z - bigCupBaseClosestPoint.Z));
            var tamaBigCup = brepBigCupSarada.DuplicateBrep();
            tamaBigCup.Transform(bigCupMoveToSarada);
            List<Rhino.Geometry.Brep> brepTamaBigCup = new List<Rhino.Geometry.Brep>();
            brepTamaBigCup.Add(tamaBigCup);


            Plane cylinderBaseSmallCupPlane = new Rhino.Geometry.Plane(Point3d.Origin, new Vector3d(1, 0, 0));
            var cylinderBaseSmallCup = new Rhino.Geometry.Circle(cylinderBaseSmallCupPlane, saradaSmallCupRadius - reduceEdgeRadius);

            var cylinderSmallCup = new Rhino.Geometry.Cylinder(cylinderBaseSmallCup, -tamaRadius);
            Brep brepCylinderSmallCup = Brep.CreateFromCylinder(cylinderSmallCup, false, false);
            var smallCupSarada = brepTama.Split(brepCylinderSmallCup, 0.1);
            var brepSmallCupSarada = smallCupSarada[1];

            var smallCupBB = brepSmallCupSarada.GetBoundingBox(true);
            var smallCupBaseClosestPoint = smallCupBB.ClosestPoint(Point3d.Origin);

            var smallCupMoveToSarada = Rhino.Geometry.Transform.Translation(new Vector3d(saradaSmallCupEdgePoint.X - smallCupBaseClosestPoint.X, saradaSmallCupEdgePoint.Y - smallCupBaseClosestPoint.Y, saradaSmallCupEdgePoint.Z - smallCupBaseClosestPoint.Z));
            var tamaSmallCup = brepSmallCupSarada.DuplicateBrep();
            tamaSmallCup.Transform(smallCupMoveToSarada);
            List<Rhino.Geometry.Brep> brepTamaSmallCup = new List<Rhino.Geometry.Brep>();
            brepTamaSmallCup.Add(tamaSmallCup);


            //A = tamaSmallCup;


            #endregion





            #region Sarada;


            Plane planeSaradaYZ = new Plane(saradaPoint, new Vector3d(1, 0, 0));
            Circle saradaAxisCircle = new Rhino.Geometry.Circle(planeSaradaYZ, saradaMidRadius);

            Plane planeSaradaBigCupYZ = new Plane(new Point3d(saradaPoint.X - saradaBigCupRadius, saradaPoint.Y, saradaPoint.Z), new Vector3d(1, 0, 0));
            Circle saradaBigCupCircle = new Rhino.Geometry.Circle(planeSaradaBigCupYZ, saradaBigCupRadius);
            Plane planeSaradaBigCupEdgeYZ = new Plane(saradaBigCupEdgePoint, new Vector3d(1, 0, 0));
            Circle saradaBigCupEdgeCircle = new Rhino.Geometry.Circle(planeSaradaBigCupEdgeYZ, saradaBigCupRadius - reduceEdgeRadius);


            Plane planeSaradaSmallCupYZ = new Plane(new Point3d(saradaPoint.X + saradaSmallCupRadius, saradaPoint.Y, saradaPoint.Z), new Vector3d(1, 0, 0));
            Circle saradaSmallCupCircle = new Rhino.Geometry.Circle(planeSaradaSmallCupYZ, saradaSmallCupRadius);
            Plane planeSaradaSmallCupEdgeYZ = new Plane(saradaSmallCupEdgePoint, new Vector3d(1, 0, 0));
            Circle saradaSmallCupEdgeCircle = new Rhino.Geometry.Circle(planeSaradaSmallCupEdgeYZ, saradaSmallCupRadius - reduceEdgeRadius);

            List<Rhino.Geometry.Circle> saradaCircles = new List<Rhino.Geometry.Circle>();
            saradaCircles.Add(saradaSmallCupCircle);
            saradaCircles.Add(saradaAxisCircle);
            saradaCircles.Add(saradaBigCupCircle);

            List<Curve> saradaCrvs = new List<Curve>();
            foreach (Circle sc in saradaCircles)
                saradaCrvs.Add(new ArcCurve(sc));
            Brep[] saradaBrep = Brep.CreateFromLoft(saradaCrvs, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);


            List<Rhino.Geometry.Circle> saradaSmallCupEdgeCircles = new List<Rhino.Geometry.Circle>();
            saradaSmallCupEdgeCircles.Add(saradaSmallCupEdgeCircle);
            saradaSmallCupEdgeCircles.Add(saradaSmallCupCircle);

            List<Curve> saradaSmallCupEdgeCrvs = new List<Curve>();
            foreach (Circle sscec in saradaSmallCupEdgeCircles)
                saradaSmallCupEdgeCrvs.Add(new ArcCurve(sscec));
            Brep[] saradaSmallCupEdgeBrep = Brep.CreateFromLoft(saradaSmallCupEdgeCrvs, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);


            List<Rhino.Geometry.Circle> saradaBigCupEdgeCircles = new List<Rhino.Geometry.Circle>();
            saradaBigCupEdgeCircles.Add(saradaBigCupCircle);
            saradaBigCupEdgeCircles.Add(saradaBigCupEdgeCircle);

            List<Curve> saradaBigCupEdgeCrvs = new List<Curve>();
            foreach (Circle sbcec in saradaBigCupEdgeCircles)
                saradaBigCupEdgeCrvs.Add(new ArcCurve(sbcec));
            Brep[] saradaBigCupEdgeBrep = Brep.CreateFromLoft(saradaBigCupEdgeCrvs, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);

            List<Rhino.Geometry.Brep> saradaBreps = new List<Rhino.Geometry.Brep>();
            saradaBreps.AddRange(brepTamaSmallCup);
            saradaBreps.AddRange(saradaSmallCupEdgeBrep);
            saradaBreps.AddRange(saradaBrep);
            saradaBreps.AddRange(saradaBigCupEdgeBrep);
            saradaBreps.AddRange(brepTamaBigCup);

            var sarada = Brep.MergeBreps(saradaBreps, 0.1);
            var saradaKenIntersection = Rhino.Geometry.Brep.CreateBooleanDifference(sarada, Ken, 0.1);
            var saradaKen = Brep.MergeBreps(saradaKenIntersection, 0.1);
            saradaBrep = Rhino.Geometry.Brep.CreateBooleanDifference(sarada, saradaKen, 0.1);

            sarada = Brep.MergeBreps(saradaBrep, 0.1);
            saradaBrep = Rhino.Geometry.Brep.CreateBooleanDifference(sarada, brepSphereKen, 0.1);

            sarada = Brep.MergeBreps(saradaBrep, 0.1);
            cylinder = new Rhino.Geometry.Cylinder(cylinderBase, saradaMidRadius);
            Brep brepCylinderSarada = Brep.CreateFromCylinder(cylinder, false, false);
            saradaBrep = Rhino.Geometry.Brep.CreateBooleanDifference(sarada, brepCylinderSarada, 0.1);
            sarada = Brep.MergeBreps(saradaBrep, 0.1);


            C = sarada;


            #endregion
        }
            /// <summary>
            /// Provides an Icon for every component that will be visible in the User Interface.
            /// Icons need to be 24x24 pixels.
            /// You can add image files to your project resources and access them like this:
            /// return Resources.IconForThisComponent;
            /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("F37A94E1-9369-4861-B503-2B3119A8E523");
    }
}