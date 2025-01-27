using System;
using System.Collections.Generic;

namespace AgOpenGPS.Core.Models
{
    public class DubinsPathSelector
    {
        private readonly DubinsPathConstraints _constraints;
        public List<DubinsPath> paths = new List<DubinsPath>();

        public DubinsPathSelector(DubinsPathConstraints constraints)
        {
            _constraints = constraints;
            GeoCircle startRightCircle = DubinsPath.ComputeCircle(constraints.StartConstraint, constraints.RadiusConstraint, TurnType.Right);
            GeoCircle startLeftCircle = DubinsPath.ComputeCircle(constraints.StartConstraint, constraints.RadiusConstraint, TurnType.Left);
            GeoCircle goalRightCircle = DubinsPath.ComputeCircle(constraints.GoalConstraint, constraints.RadiusConstraint, TurnType.Right);
            GeoCircle goalLeftCircle = DubinsPath.ComputeCircle(constraints.GoalConstraint, constraints.RadiusConstraint, TurnType.Left);

            // 2 x OuterDubinsPath
            if (RsrDubinsPath.PathIsPossible(startRightCircle, goalRightCircle))
            {
                paths.Add(new RsrDubinsPath(_constraints));
            }
            if (LslDubinsPath.PathIsPossible(startRightCircle, goalRightCircle))
            {
                paths.Add(new LslDubinsPath(_constraints));
            }
            // 2 x CurvedDubinsPath
            if (LrlDubinsPath.PathIsPossible(startLeftCircle, goalLeftCircle))
            {
                paths.Add(new LrlDubinsPath(_constraints));
            }
            if (RlrDubinsPath.PathIsPossible(startRightCircle, goalRightCircle))
            {
                paths.Add(new RlrDubinsPath(_constraints));
            }
            // 2 x InnerDubinsPath
            if (RslDubinsPath.PathIsPossible(startRightCircle, goalLeftCircle))
            {
                paths.Add(new RslDubinsPath(_constraints));
            }
            if (LsrDubinsPath.PathIsPossible(startLeftCircle, goalRightCircle))
            {
                paths.Add(new LsrDubinsPath(_constraints));
            }
            paths.Sort((x, y) => x.TotalLength.CompareTo(y.TotalLength));
        }
    }

}
