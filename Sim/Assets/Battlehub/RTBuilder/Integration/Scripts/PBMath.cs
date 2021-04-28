
using UnityEngine;

namespace Battlehub.ProBuilderIntegration
{
    public static class PBMath 
    {
        public static Vector2 DivideBy(this Vector2 v, Vector2 o)
        {
            return new Vector2(v.x / o.x, v.y / o.y);
        }

        /// <summary>
        /// Returns a new point by rotating the Vector2 around an origin point.
        /// </summary>
        /// <param name="v">Vector2 original point.</param>
        /// <param name="origin">The pivot to rotate around.</param>
        /// <param name="theta">How far to rotate in degrees.</param>
        /// <returns></returns>
        public static Vector2 RotateAroundPoint(this Vector2 v, Vector2 origin, float theta)
        {
            float cx = origin.x, cy = origin.y; // origin
            float px = v.x, py = v.y;           // point

            float s = Mathf.Sin(theta * Mathf.Deg2Rad);
            float c = Mathf.Cos(theta * Mathf.Deg2Rad);

            // translate point back to origin:
            px -= cx;
            py -= cy;

            // rotate point
            float xnew = px * c + py * s;
            float ynew = -px * s + py * c;

            // translate point back:
            px = xnew + cx;
            py = ynew + cy;

            return new Vector2(px, py);
        }
    }

}

