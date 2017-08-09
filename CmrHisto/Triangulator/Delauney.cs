// <copyright file="Delauney.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

// Credit to Paul Bourke (pbourke@swin.edu.au) for the original Fortran 77 Program :))
// Converted to a standalone C# 2.0 library by Morten Nielsen (www.iter.dk)
// Check out: http://astronomy.swin.edu.au/~pbourke/terrain/triangulate/

using System;
using System.Collections.Generic;

namespace CmrHisto.Triangulator
{
	/// <summary>
	/// A class that performs the Delauney triangulation on a set of vertices.
	/// </summary>
	/// <remarks>
	/// Based on Paul Bourke's "An Algorithm for Interpolating Irregularly-Spaced Data
	/// with Applications in Terrain Modelling"
	/// http://astronomy.swin.edu.au/~pbourke/modelling/triangulate/
	/// </remarks>
	internal sealed class Delauney
	{
		#region Private Static Methods
		/// <summary>
		/// Returns true if the point (p) lies inside the circumcircle made up by points (pointOne, pointTwo, pointThree).
		/// </summary>
		/// <remarks>
		/// NOTE: A point on the edge is inside the circumcircle.
		/// </remarks>
		/// <param name="pointToCheck">Point to check.</param>
		/// <param name="pointOne">First point on circle.</param>
		/// <param name="pointTwo">Second point on circle.</param>
		/// <param name="pointThree">Third point on circle.</param>
		/// <returns>true if p is inside circle.</returns>
		private static bool InCircle(Point pointToCheck, Point pointOne, Point pointTwo, Point pointThree)
		{
			// Return TRUE if the point (xp, yp) lies inside the circumcircle
			// made up by points (x1, y1) (x2, y2) (x3, y3)
			// NOTE: A point on the edge is inside the circumcircle
			if (System.Math.Abs(pointOne.Y - pointTwo.Y) < double.Epsilon && System.Math.Abs(pointTwo.Y - pointThree.Y) < double.Epsilon)
			{
				// INCIRCUM - F - Points are coincident !!
				return false;
			}

			double m1, m2;
			double mx1, mx2;
			double my1, my2;
			double xc, yc;

			if (System.Math.Abs(pointTwo.Y - pointOne.Y) < double.Epsilon)
			{
				m2 = -(pointThree.X - pointTwo.X) / (pointThree.Y - pointTwo.Y);
				mx2 = (pointTwo.X + pointThree.X) * 0.5;
				my2 = (pointTwo.Y + pointThree.Y) * 0.5;

				// Calculate CircumCircle center (xc, yc)
				xc = (pointTwo.X + pointOne.X) * 0.5;
				yc = (m2 * (xc - mx2)) + my2;
			}
			else if (System.Math.Abs(pointThree.Y - pointTwo.Y) < double.Epsilon)
			{
				m1 = -(pointTwo.X - pointOne.X) / (pointTwo.Y - pointOne.Y);
				mx1 = (pointOne.X + pointTwo.X) * 0.5;
				my1 = (pointOne.Y + pointTwo.Y) * 0.5;

				// Calculate CircumCircle center (xc, yc)
				xc = (pointThree.X + pointTwo.X) * 0.5;
				yc = (m1 * (xc - mx1)) + my1;
			}
			else
			{
				m1 = -(pointTwo.X - pointOne.X) / (pointTwo.Y - pointOne.Y);
				m2 = -(pointThree.X - pointTwo.X) / (pointThree.Y - pointTwo.Y);
				mx1 = (pointOne.X + pointTwo.X) * 0.5;
				mx2 = (pointTwo.X + pointThree.X) * 0.5;
				my1 = (pointOne.Y + pointTwo.Y) * 0.5;
				my2 = (pointTwo.Y + pointThree.Y) * 0.5;

				// Calculate CircumCircle center (xc, yc)
				xc = ((m1 * mx1) - (m2 * mx2) + my2 - my1) / (m1 - m2);
				yc = (m1 * (xc - mx1)) + my1;
			}

			double dx = pointTwo.X - xc;
			double dy = pointTwo.Y - yc;
			double rsqr = (dx * dx) + (dy * dy);
			dx = pointToCheck.X - xc;
			dy = pointToCheck.Y - yc;
			double drsqr = (dx * dx) + (dy * dy);

			return drsqr <= rsqr;
		}
		#endregion

		#region Private Constructor
		/// <summary>
		/// Prevents a default instance of the <see cref="Delauney"/> class from being created.
		/// </summary>
		private Delauney()
		{
		}
		#endregion

		#region Internal Static Methods
		/// <summary>
		/// Performs Delauney triangulation on a set of points.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The triangulation doesn't support multiple points with the same planar location.
		/// Vertex-lists with duplicate points may result in strange triangulation with intersecting edges.
		/// To avoid adding multiple points to your vertex-list you can use the following anonymous predicate
		/// method:
		/// <code>
		/// if(!Vertices.Exists(delegate(Triangulator.Geometry.Point p) { return pNew.Equals2D(p); }))
		///		Vertices.Add(pNew);
		/// </code>
		/// </para>
		/// <para>The triangulation algorithm may be described in pseudo-code as follows:
		/// <code>
		/// subroutine Triangulate
		/// input : vertex list
		/// output : triangle list
		///    initialize the triangle list
		///    determine the supertriangle
		///    add supertriangle vertices to the end of the vertex list
		///    add the supertriangle to the triangle list
		///    for each sample point in the vertex list
		///       initialize the edge buffer
		///       for each triangle currently in the triangle list
		///          calculate the triangle circumcircle center and radius
		///          if the point lies in the triangle circumcircle then
		///             add the three triangle edges to the edge buffer
		///             remove the triangle from the triangle list
		///          endif
		///       endfor
		///       delete all doubly specified edges from the edge buffer
		///          this leaves the edges of the enclosing polygon only
		///       add to the triangle list all triangles formed between the point 
		///          and the edges of the enclosing polygon
		///    endfor
		///    remove any triangles from the triangle list that use the supertriangle vertices
		///    remove the supertriangle vertices from the vertex list
		/// end
		/// </code>
		/// </para>
		/// </remarks>
		/// <param name="verticies">List of vertices to triangulate.</param>
		/// <returns>Triangles referencing vertex indices arranged in clockwise order</returns>
		internal static List<Triangle> Triangulate(List<Point> verticies)
		{
			int vertexCount = verticies.Count;
			if (vertexCount < 3)
			{
				throw new ArgumentException("Need at least three vertices for triangulation");
			}

			int triangleMax = 4 * vertexCount;

			// Find the maximum and minimum vertex bounds.
			// This is to allow calculation of the bounding supertriangle
			double xMin = verticies[0].X;
			double xMax = xMin;
			double yMin = verticies[0].Y;
			double yMax = yMin;

			for (int i = 1; i < vertexCount; i++)
			{
				if (verticies[i].X < xMin)
				{
					xMin = verticies[i].X;
				}

				if (verticies[i].X > xMax)
				{
					xMax = verticies[i].X;
				}

				if (verticies[i].Y < yMin)
				{
					yMin = verticies[i].Y;
				}

				if (verticies[i].Y > yMax)
				{
					yMax = verticies[i].Y;
				}
			}

			double xDelta = xMax - xMin;
			double yDelta = yMax - yMin;
			double maxDelta = xDelta > yDelta ? xDelta : yDelta;

			double xMidpoint = (xMax + xMin) * 0.5;
			double yMidpoint = (yMax + yMin) * 0.5;

			// Set up the supertriangle
			// This is a triangle which encompasses all the sample points.
			// The supertriangle coordinates are added to the end of the
			// vertex list. The supertriangle is the first triangle in
			// the triangle list.
			verticies.Add(new Point(xMidpoint - (2 * maxDelta), yMidpoint - maxDelta));
			verticies.Add(new Point(xMidpoint, yMidpoint + (2 * maxDelta)));
			verticies.Add(new Point(xMidpoint + (2 * maxDelta), yMidpoint - maxDelta));
			List<Triangle> triangles = new List<Triangle>();

			// SuperTriangle placed at index 0
			triangles.Add(new Triangle(vertexCount, vertexCount + 1, vertexCount + 2));

			// Include each point one at a time into the existing mesh
			for (int i = 0; i < vertexCount; i++)
			{
				List<Edge> edges = new List<Edge>();

				// Set up the edge buffer.
				// If the point (Vertex(i).x,Vertex(i).y) lies inside the circumcircle then the
				// three edges of that triangle are added to the edge buffer and the triangle is removed from list.
				for (int j = 0; j < triangles.Count; j++)
				{
					if (Delauney.InCircle(verticies[i], verticies[triangles[j].PointOne], verticies[triangles[j].PointTwo], verticies[triangles[j].PointThree]))
					{
						edges.Add(new Edge(triangles[j].PointOne, triangles[j].PointTwo));
						edges.Add(new Edge(triangles[j].PointTwo, triangles[j].PointThree));
						edges.Add(new Edge(triangles[j].PointThree, triangles[j].PointOne));
						triangles.RemoveAt(j);
						j--;
					}
				}

				// In case we the last duplicate point we removed was the last in the array
				if (i >= vertexCount)
				{
					continue;
				}

				// Remove duplicate edges
				// Note: if all triangles are specified anticlockwise then all
				// interior edges are opposite pointing in direction.
				for (int j = edges.Count - 2; j >= 0; j--)
				{
					for (int k = edges.Count - 1; k >= j + 1; k--)
					{
						if (edges[j].Equals(edges[k]))
						{
							edges.RemoveAt(k);
							edges.RemoveAt(j);
							k--;
							continue;
						}
					}
				}

				// Form new triangles for the current point
				// Skipping over any tagged edges.
				// All edges are arranged in clockwise order.
				for (int j = 0; j < edges.Count; j++)
				{
					if (triangles.Count >= triangleMax)
					{
						throw new ApplicationException("Exceeded maximum edges");
					}

					triangles.Add(new Triangle(edges[j].StartingPoint, edges[j].EndingPoint, i));
				}

				edges.Clear();
				edges = null;
			}

			// Remove triangles with supertriangle vertices
			// These are triangles which have a vertex number greater than vertexCount
			for (int i = triangles.Count - 1; i >= 0; i--)
			{
				if (triangles[i].PointOne >= vertexCount || triangles[i].PointTwo >= vertexCount || triangles[i].PointThree >= vertexCount)
				{
					triangles.RemoveAt(i);
				}
			}

			// Remove SuperTriangle vertices
			verticies.RemoveAt(verticies.Count - 1);
			verticies.RemoveAt(verticies.Count - 1);
			verticies.RemoveAt(verticies.Count - 1);
			triangles.TrimExcess();

			return triangles;
		}
		#endregion
	}
}
