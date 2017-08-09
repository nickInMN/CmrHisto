// <copyright file="SurfaceMapDialog.xaml.cs" company="Nick Brett">
//     Copyright (c) 2013 Nick Brett. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CmrHisto.Enums;
using CmrHisto.Triangulator;
using CmrHisto.ViewModels;
using log4net;
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph;
using SharpGL.WPF;

namespace CmrHisto.Views
{
    /// <summary>
    /// Interaction logic for SurfaceMapDialog.xaml
    /// </summary>
    public sealed partial class SurfaceMapDialog : Window
	{
		#region Private Constants
		private const string LabelFontFace = "Arial";
		private const float LabelFontExtrusion = 0.1f;
		private const float LabelFontScaleLarge = 4f;
		private const float LabelFontScaleExtraLarge = 6f;
		private const float LabelFontScaleDefault = 1f;
		private const double DefaultHorizontalRotation = -45;
		private const double DefaultVerticalRotation = 20;
        #endregion

		#region Private Data Members
		////private Vertex mCursorVertex;
		private uint[] _surfaceMapTexture = new uint[1];
		private uint[] _surfaceMapVertexBufferId = new uint[1];
		private uint[] _surfaceMapNormalBufferId = new uint[1];
		private uint[] _surfaceMapIndiciesBufferId = new uint[1];
		private uint[] _surfaceMapTextureBufferId = new uint[1];
		private uint[] _axesThickLinesVertexBufferId = new uint[1];
		private uint[] _axesThickLinesIndiciesBufferId = new uint[1];
		private uint[] _axesThinLinesVertexBufferId = new uint[1];
		private uint[] _axesThinLinesIndiciesBufferId = new uint[1];

        private SurfaceMapDialogViewModel _viewModel;
        private static readonly ILog Log = LogManager.GetLogger(typeof(SurfaceMapDialog));
        #endregion

        #region Private Properties
		/// <summary>
		/// Gets or sets the last position of the mouse.
		/// </summary>
		private System.Windows.Point LastMousePosition { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the left mouse button is down.
		/// </summary>
		private bool IsLeftButtonDown { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="Bitmap"/> used to store the texture used to add color.
		/// </summary>
		private Bitmap GradientImage { get; set; }

		/// <summary>
		/// Gets or sets the size to make the view along the x axis. This represents half of the window's width.
		/// </summary>
		private double XWindow { get; set; }

		/// <summary>
		/// Gets or sets the size to make the view along the y axis. This represents half of the window's height.
		/// </summary>
		private double YWindow { get; set; }

		/// <summary>
		/// Gets or sets the size to make the view along the z axis. This represents half of the window's depth.
		/// </summary>
		private double ZWindow { get; set; }

		/// <summary>
		/// Gets or sets the amount to rotate around the x axis.
		/// </summary>
		private double VerticalRotation { get; set; }

		/// <summary>
		/// Gets or sets the amount to rotate around the y axis.
		/// </summary>
		private double HorizontalRotation { get; set; }

		/// <summary>
		/// Gets or sets the vertex data for the surface map.
		/// </summary>
		private float[] SurfaceMapVertexData { get; set; }

		/// <summary>
		/// Gets or sets the normal data for the surface map.
		/// </summary>
		private float[] SurfaceMapNormalData { get; set; }

		/// <summary>
		/// Gets or sets the texture data for the surface map.
		/// </summary>
		private float[] SurfaceMapTextureData { get; set; }

		/// <summary>
		/// Gets or sets the indices of the surface map.
		/// </summary>
		private uint[] SurfaceMapIndicesData { get; set; }

		/// <summary>
		/// Gets or sets the data that represents all of the vertices for drawing the thick lines on the axes.
		/// </summary>
		private float[] ThickAxesVertexData { get; set; }

		/// <summary>
		/// Gets or sets the indices of the thick lines used to show the data.
		/// </summary>
		private uint[] ThickAxesLinesIndices { get; set; }

		/// <summary>
		/// Gets or sets the data that represents all of the vertices for drawing the thin lines on the axes.
		/// </summary>
		private float[] ThinAxesVertexData { get; set; }

		/// <summary>
		/// Gets or sets the indices of the thin lines used to show the data.
		/// </summary>
		private uint[] ThinAxesLinesIndices { get; set; }
		#endregion

		#region Private Static Methods
		/// <summary>
		/// This is a simple method to get the value to use as the y coordinate of the texture given the value.  It isn't necessary
		/// but I like this better than having math like this in every call that sets the texture coordinate.
		/// </summary>
		/// <param name="value">The y value being rendered.</param>
		/// <returns>The y coordinate of the value in the texture between 0 and 1.</returns>
		private static float CalculateYTextureValue(double value)
		{
			return 1 - (((float)value + 50) / 100);
		}
        #endregion

        #region Private Event Handlers
        /// <summary>
        /// Recalculate the surface map when the user changes the <see cref="Enums.DataType"/>.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">Event parameters.</param>
        private async void DataType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._viewModel.Triangles != null && this._viewModel.Triangles.Count > 0)
            {
                await this.PrepareSurfaceMapDataAsync();
            }
        }

        /// <summary>
        /// Setup the OpenGL environment.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="args">Additional event parameters.</param>
        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
		{
			OpenGL gl = args.OpenGL;

            try
            {
                SurfaceMapDialog.Log.Info($"OpenGL version: {gl.GetString(OpenGL.GL_VERSION)} - Renderer: {gl.GetString(OpenGL.GL_RENDERER)}");
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error getting OpenGL version.", ex);
            }

            gl.ClearColor(1, 1, 1, 1);
			gl.Enable(OpenGL.GL_BLEND);
			gl.ClearDepth(1);
			gl.Enable(OpenGL.GL_DEPTH_TEST | OpenGL.GL_DEPTH_BUFFER);
			gl.DepthFunc(DepthFunction.LessThanOrEqual);

			// If the user has supplied a gradient file use it, otherwise just load it from resources.
			if (File.Exists(this._viewModel.LegendImagePath))
			{
                try
                {
                    this.GradientImage = new Bitmap(this._viewModel.LegendImagePath);
                }
                catch (FileNotFoundException ex)
                {
                    SurfaceMapDialog.Log.Error($"Legend bitmap not found at: {this._viewModel.LegendImagePath}. Using standard image instead.", ex);
                    this.GradientImage = CmrHisto.Properties.Resources.Gradient;
                }
			}
			else
			{
				this.GradientImage = CmrHisto.Properties.Resources.Gradient;
			}

			gl.Enable(OpenGL.GL_TEXTURE_2D);
			gl.GenTextures(1, this._surfaceMapTexture);
			gl.BindTexture(OpenGL.GL_TEXTURE_2D, this._surfaceMapTexture[0]);
			gl.TexImage2D(
				OpenGL.GL_TEXTURE_2D,
				0,
				3,
				this.GradientImage.Width,
				this.GradientImage.Height,
				0,
				OpenGL.GL_BGR,
				OpenGL.GL_UNSIGNED_BYTE,
				this.GradientImage.LockBits(new Rectangle(0, 0, this.GradientImage.Width, this.GradientImage.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, this.GradientImage.PixelFormat).Scan0);

			gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
			gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);

			// Create all the buffers for the axis lines.
			int[] bufferSize = new int[1];
			gl.GenBuffers(1, this._axesThickLinesVertexBufferId);
			gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._axesThickLinesVertexBufferId[0]);

			GCHandle vertexHandle = GCHandle.Alloc(this.ThickAxesVertexData, GCHandleType.Pinned);
			IntPtr vertexPointer = vertexHandle.AddrOfPinnedObject();

			gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.ThickAxesVertexData.Length * sizeof(float), vertexPointer, OpenGL.GL_STATIC_DRAW);

			gl.GetBufferParameter(OpenGL.GL_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
			if (this.ThickAxesVertexData.Length * sizeof(float) != bufferSize[0])
			{
                SurfaceMapDialog.Log.Error("Vertex array not uploaded currectly (thick axes).");
				throw new InvalidOperationException("Vertex array not uploaded correctly");
			}

			gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
			gl.GenBuffers(1, this._axesThickLinesIndiciesBufferId);
			gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._axesThickLinesIndiciesBufferId[0]);

			GCHandle indexHandle = GCHandle.Alloc(this.ThickAxesLinesIndices, GCHandleType.Pinned);
			IntPtr indexPointer = indexHandle.AddrOfPinnedObject();

			gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this.ThickAxesLinesIndices.Length * sizeof(int), indexPointer, OpenGL.GL_STATIC_DRAW);

			gl.GetBufferParameter(OpenGL.GL_ELEMENT_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
			if (this.ThickAxesLinesIndices.Length * sizeof(int) != bufferSize[0])
			{
                SurfaceMapDialog.Log.Error("Element array not uploaded correctly (thick lines).");
                throw new InvalidOperationException("Element array not uploaded correctly");
			}

			gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);

			gl.GenBuffers(1, this._axesThinLinesVertexBufferId);
			gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._axesThinLinesVertexBufferId[0]);

			vertexHandle = GCHandle.Alloc(this.ThinAxesVertexData, GCHandleType.Pinned);
			vertexPointer = vertexHandle.AddrOfPinnedObject();

			gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.ThinAxesVertexData.Length * sizeof(float), vertexPointer, OpenGL.GL_STATIC_DRAW);

			gl.GetBufferParameter(OpenGL.GL_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
			if (this.ThinAxesVertexData.Length * sizeof(float) != bufferSize[0])
			{
                SurfaceMapDialog.Log.Error("Vertex array not uploaded correctly (thin axes).");
                throw new InvalidOperationException("Vertex array not uploaded correctly");
			}

			gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
			gl.GenBuffers(1, this._axesThinLinesIndiciesBufferId);
			gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._axesThinLinesIndiciesBufferId[0]);

			indexHandle = GCHandle.Alloc(this.ThinAxesLinesIndices, GCHandleType.Pinned);
			indexPointer = indexHandle.AddrOfPinnedObject();

			gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this.ThinAxesLinesIndices.Length * sizeof(int), indexPointer, OpenGL.GL_STATIC_DRAW);

			gl.GetBufferParameter(OpenGL.GL_ELEMENT_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
			if (this.ThinAxesLinesIndices.Length * sizeof(int) != bufferSize[0])
			{
                SurfaceMapDialog.Log.Error("Element array not uploaded correctly (thin axes).");
                throw new InvalidOperationException("Element array not uploaded correctly");
			}

			gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
			gl.ShadeModel(OpenGL.GL_SMOOTH);
		}

		/// <summary>
		/// Handles the OpenGLDraw event of the OpenGLControl control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
		private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
		{
            try
            {
                OpenGL gl = args.OpenGL;

                // Doing this will prevent a big black blob on the screen.
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                // The control starts drawing immediately, but if the user hasn't selected a PID we have nothing to draw, so don't.
                if (this._viewModel.Triangles.Count < 1)
                {
                    return;
                }

                // Reset the scene
                gl.MatrixMode(MatrixMode.Projection);
                gl.LoadIdentity();
                gl.Ortho(this.XWindow * -2, this.XWindow * 2, this.YWindow * -2, this.YWindow * 2, -200, 200);

                gl.MatrixMode(MatrixMode.Modelview);
                gl.LoadIdentity();
                gl.LookAt(0, 0, this.ZWindow, 0, 0, 0, 0, 1, 0);
                gl.Rotate(this.VerticalRotation, 1f, 0f, 0f);
                gl.Rotate(this.HorizontalRotation, 0f, 1f, 0f);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in OpenGLControl_OpenGLDraw", ex);
            }

			// Render the map and the axes.
			this.RenderSurfaceMap(args.OpenGL);
			this.RenderAxes(args.OpenGL);

			////gl.LoadIdentity();
			////gl.PointSize(5);
			////gl.Color(1f, 0f, 0f, 0f);
			////gl.Enable(OpenGL.GL_POINT_SMOOTH);
			////gl.Hint(HintTarget.PointSmooth, HintMode.Nicest);
			////gl.Begin(BeginMode.Points);
			////gl.Vertex(this.mCursorVertex.X, this.mCursorVertex.Y, this.mCursorVertex.Z);
			////gl.End();
			////gl.Disable(OpenGL.GL_POINT_SMOOTH);
			////gl.PointSize(1);
		}

		/// <summary>
		/// Handles the user clicking the close button and closes the window.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event arguments.</param>
		private void CloseButtonClicked(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Handles the user changing the selected pid and enables or disables the map button.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event arguments.</param>
		private void PidListboxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
            if (e.AddedItems.Count > 0)
			{
				this._viewModel.ShowMapEnabled = true;
                this._viewModel.SelectedPidName = e.AddedItems[0].ToString();
			}
			else
			{
                this._viewModel.ShowMapEnabled = false;
                this._viewModel.SelectedPidName = string.Empty;
			}
		}

		/// <summary>
		/// Handle the user clicking the map button and start the process of rending the surface map.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event arguments.</param>
		private async void MapButtonClicked(object sender, RoutedEventArgs e)
		{
            await this.PrepareSurfaceMapDataAsync();
        }

		/// <summary>
		/// Handle the user releasing the left or right mouse button and reset and clear the appropriate values.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void OpenGLControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			this.LastMousePosition = new System.Windows.Point();
			if (e.ChangedButton == MouseButton.Left)
			{
				this.IsLeftButtonDown = false;
			}
		}

		/// <summary>
		/// Handles the user clicking the left mouse button and sets the appropriate flags.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void OpenGLControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
            OpenGLControl openGlControl = sender as OpenGLControl;
			this.LastMousePosition = Mouse.GetPosition(openGlControl);
			if (e.ChangedButton == MouseButton.Left)
			{
				this.IsLeftButtonDown = true;
			}
		}

        /// <summary>
        /// Handle the user clicking the center button and rotate the view back to 0,0
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">Additional event parameters</param>
        private void CenterButton_Click(object sender, RoutedEventArgs e)
		{
			this.VerticalRotation = DefaultVerticalRotation;
			this.HorizontalRotation = DefaultHorizontalRotation;
		}

		/// <summary>
		/// Handles the user double clicking a pid to display the surface map.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private async void PidListbox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(this._viewModel.SelectedPidName))
			{
                await this.PrepareSurfaceMapDataAsync();
            }
		}

		/// <summary>
		/// Handle the user moving the mouse within the OpenGL scene.
		/// </summary>
		/// <param name="sender">The object that fired the event.</param>
		/// <param name="e">Additional event parameters.</param>
		private void OpenGLControl_MouseMove(object sender, MouseEventArgs e)
		{
			OpenGLControl openGlControl = sender as OpenGLControl;
			System.Windows.Point currentPosition = Mouse.GetPosition(openGlControl);

			// Left button down for rotating the map.
			if (this.IsLeftButtonDown)
			{
				if (this.LastMousePosition.X > currentPosition.X && this.LastMousePosition.Y == currentPosition.Y)
				{
					// Only left
					this.MoveCamera(CameraMoveDirection.Left);
				}
				else if (this.LastMousePosition.X > currentPosition.X && this.LastMousePosition.Y > currentPosition.Y)
				{
					// Left and up
					this.MoveCamera(CameraMoveDirection.LeftAndUp);
				}
				else if (this.LastMousePosition.X > currentPosition.X && this.LastMousePosition.Y < currentPosition.Y)
				{
					// Left and down
					this.MoveCamera(CameraMoveDirection.LeftAndDown);
				}
				else if (this.LastMousePosition.X < currentPosition.X && this.LastMousePosition.Y == currentPosition.Y)
				{
					// Only right
					this.MoveCamera(CameraMoveDirection.Right);
				}
				else if (this.LastMousePosition.X < currentPosition.X && this.LastMousePosition.Y < currentPosition.Y)
				{
					// Right and down
					this.MoveCamera(CameraMoveDirection.RightAndDown);
				}
				else if (this.LastMousePosition.X < currentPosition.X && this.LastMousePosition.Y > currentPosition.Y)
				{
					// Right and up
					this.MoveCamera(CameraMoveDirection.RightAndUp);
				}
				else if (this.LastMousePosition.X == currentPosition.X && this.LastMousePosition.Y < currentPosition.Y)
				{
					// Down only
					this.MoveCamera(CameraMoveDirection.Down);
				}
				else if (this.LastMousePosition.X == currentPosition.X && this.LastMousePosition.Y > currentPosition.Y)
				{
					// Up only
					this.MoveCamera(CameraMoveDirection.Up);
				}
			}
			else
			{
				if (this._viewModel.MapData != null && this._viewModel.MapData.Data.Count > 0)
				{
					// Map the mouse coordinates to the scene and find the closest point
					OpenGL gl = openGlControl.OpenGL;

					gl.LoadIdentity();

					byte[] pixelData = new byte[4];
					gl.ReadPixels((int)currentPosition.X, (int)currentPosition.Y, 1, 1, OpenGL.GL_DEPTH_COMPONENT, OpenGL.GL_FLOAT, pixelData);
					////float depth = System.BitConverter.ToSingle(pixelData, 0);

					// Screen coords are 0, 0 upper left, OpenGL is 0, 0 lower left
					int[] viewport = new int[4];
					gl.GetInteger(GetTarget.Viewport, viewport);

					////double[] coords = gl.UnProject(currentPosition.X, viewport[3] - currentPosition.Y, depth);

					// Find the closest point in the surface map to the x and z coordinates and set the indicator there and update the label
					////Triangulator.Point closestPoint = null;
					////double closestDistance = double.MaxValue;
					////double distance = double.MaxValue;
					////foreach (Triangulator.Point p in this._viewModel.MapData.Data)
					////{
					////	// We only care about X and Z distances.
					////	distance = Math.Sqrt(Math.Pow(coords[0] - p.X, 2) + Math.Pow(coords[2] - p.Y, 2));
					////	if (distance < closestDistance)
					////	{
					////		closestDistance = distance;
					////		closestPoint = p;
					////	}
					////}

					////this.mCursorVertex = new Vertex((float)closestPoint.X, (float)closestPoint.Value, (float)closestPoint.Y);
					////this.mCursorVertex = new Vertex((float)coords[0], (float)coords[1], (float)coords[2]);
					////this.Title = string.Format("Cursor: {3}, {4}, {5}, X: {0}, Y: {1}, Z: {2}, CURSOR: {6}, {7}, {8}", Math.Round(closestPoint.X, 2), Math.Round(closestPoint.Value, 2), Math.Round(closestPoint.Y, 2), coords[0], coords[1], coords[2], this.mCursorVertex.X, this.mCursorVertex.Y, this.mCursorVertex.Z);
				}
			}

			this.LastMousePosition = currentPosition;
		}
		#endregion

		#region Private Methods
		/// <summary>
		/// Renders the surface map to the screen.
		/// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
		private void RenderSurfaceMap(OpenGL gl)
		{
            try
            {
                if (this._surfaceMapVertexBufferId[0] == 0 || this._surfaceMapIndiciesBufferId[0] == 0)
                {
                    return;
                }

                gl.Enable(OpenGL.GL_TEXTURE2);
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, this._surfaceMapTexture[0]);
                gl.Color(1f, 1f, 1f);

                if (this._surfaceMapTextureBufferId[0] != 0)
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapTextureBufferId[0]);
                    gl.TexCoordPointer(2, OpenGL.GL_FLOAT, sizeof(float) * 2, IntPtr.Zero);
                    gl.EnableClientState(OpenGL.GL_TEXTURE_COORD_ARRAY);
                }

                if (this._viewModel.RenderMode == BeginMode.Points.ToString())
                {
                    gl.PointSize(3);
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapVertexBufferId[0]);
                    gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(float) * 3, IntPtr.Zero);
                    gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);

                    if (!this._viewModel.ShowColors)
                    {
                        gl.Disable(OpenGL.GL_TEXTURE2);
                        gl.Color(0, 0, 0, 0);
                    }

                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._surfaceMapIndiciesBufferId[0]);
                    gl.DrawElements(OpenGL.GL_POINTS, this.SurfaceMapIndicesData.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                    gl.Disable(OpenGL.GL_TEXTURE2);
                    gl.PointSize(1);
                }
                else if (this._viewModel.RenderMode == BeginMode.Lines.ToString())
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapVertexBufferId[0]);
                    gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(float) * 3, IntPtr.Zero);
                    gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);

                    if (!this._viewModel.ShowColors)
                    {
                        gl.Disable(OpenGL.GL_TEXTURE2);
                        gl.Color(0, 0, 0, 0);
                    }

                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._surfaceMapIndiciesBufferId[0]);
                    gl.DrawElements(OpenGL.GL_LINES, this.SurfaceMapIndicesData.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                    gl.Disable(OpenGL.GL_TEXTURE2);
                }
                else
                {
                    gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapVertexBufferId[0]);
                    gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(float) * 3, IntPtr.Zero);
                    gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);

                    gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._surfaceMapIndiciesBufferId[0]);
                    gl.DrawElements(OpenGL.GL_TRIANGLES, this.SurfaceMapIndicesData.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                    gl.Disable(OpenGL.GL_TEXTURE2);

                    // Draw the edges of the triangles.
                    if (this._viewModel.ShowLines)
                    {
                        gl.Begin(BeginMode.Lines);
                        gl.Color(0f, 0f, 0f);

                        foreach (Triangle t in this._viewModel.Triangles)
                        {
                            Triangulator.Point pointOne = this._viewModel.MapData.Data[t.PointOne];
                            Triangulator.Point pointTwo = this._viewModel.MapData.Data[t.PointTwo];
                            Triangulator.Point pointThree = this._viewModel.MapData.Data[t.PointThree];

                            // First leg of the triangle
                            gl.Vertex(pointOne.X, pointOne.Value, pointOne.Y);
                            gl.Vertex(pointTwo.X, pointTwo.Value, pointTwo.Y);

                            // Second leg of the triangle
                            gl.Vertex(pointTwo.X, pointTwo.Value, pointTwo.Y);
                            gl.Vertex(pointThree.X, pointThree.Value, pointThree.Y);

                            // Third leg of the triangle
                            gl.Vertex(pointThree.X, pointThree.Value, pointThree.Y);
                            gl.Vertex(pointOne.X, pointOne.Value, pointOne.Y);
                        }

                        gl.End();
                    }
                }
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in RenderSurfaceMap", ex);
            }
		}

        /// <summary>
        /// Render the axes, grid lines and labels.
        /// </summary
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        private void RenderAxes(OpenGL gl)
		{
            try
            {
                // Render the thick lines
                gl.Color(0, 0, 0);
                gl.LineWidth(3);
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._axesThickLinesVertexBufferId[0]);
                gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(float) * 3, IntPtr.Zero);
                gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
                gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._axesThickLinesIndiciesBufferId[0]);
                gl.DrawElements(OpenGL.GL_LINES, this.ThickAxesLinesIndices.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
                gl.LineWidth(1);

                // Render the thin lines
                gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._axesThinLinesVertexBufferId[0]);
                gl.VertexPointer(3, OpenGL.GL_FLOAT, sizeof(float) * 3, IntPtr.Zero);
                gl.EnableClientState(OpenGL.GL_VERTEX_ARRAY);
                gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._axesThinLinesIndiciesBufferId[0]);
                gl.DrawElements(OpenGL.GL_LINES, this.ThinAxesLinesIndices.Length, OpenGL.GL_UNSIGNED_INT, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in RenderAxes.", ex);
            }

			// X axis values
			double xAxisMidpoint = (this._viewModel.MapData.HighestXAxisValue + this._viewModel.MapData.LowestXAxisValue) / 2;
			this.DrawXAxisValue(gl, this._viewModel.MapData.LowestXAxisValue, -64.5, -50.5, -50.5);
			this.DrawXAxisValue(gl, (xAxisMidpoint + this._viewModel.MapData.LowestXAxisValue) / 2, -64.5, -50.5, -25.25);
			this.DrawXAxisValue(gl, xAxisMidpoint, -64.5, -50.5, 0);
			this.DrawXAxisValue(gl, (this._viewModel.MapData.HighestXAxisValue + xAxisMidpoint) / 2, -64.5, -50.5, 25.25);
			this.DrawXAxisValue(gl, this._viewModel.MapData.HighestXAxisValue, -64.5, -50.5, 50.5);

			this.DrawXAxisValue(gl, this._viewModel.MapData.LowestXAxisValue, 53.5, 50.5, -50.5);
			this.DrawXAxisValue(gl, xAxisMidpoint + this._viewModel.MapData.LowestXAxisValue, 53.5, 50.5, -25.25);
			this.DrawXAxisValue(gl, xAxisMidpoint, 53.5, 50.5, 0);
			this.DrawXAxisValue(gl, (this._viewModel.MapData.HighestXAxisValue + xAxisMidpoint) / 2, 53.5, 50.5, 25.25);
			this.DrawXAxisValue(gl, this._viewModel.MapData.HighestXAxisValue, 53.5, 50.5, 50.5);

			// Y axis values
			double yAxisMidpoint = (this._viewModel.MapData.HighestValue + this._viewModel.MapData.LowestValue) / 2;
			this.DrawYAxisValue(gl, this._viewModel.MapData.LowestValue, 53.5, -50.5, -50.5);
			this.DrawYAxisValue(gl, (yAxisMidpoint + this._viewModel.MapData.LowestValue) / 2, 53.5, -25.25, -50.5);
			this.DrawYAxisValue(gl, yAxisMidpoint, 53.5, 0, -50.5);
			this.DrawYAxisValue(gl, (this._viewModel.MapData.HighestValue + yAxisMidpoint) / 2, 53.5, 25.25, -50.5);
			this.DrawYAxisValue(gl, this._viewModel.MapData.HighestValue, 53.5, 50.5, -50.5);

			this.DrawYAxisValue(gl, this._viewModel.MapData.LowestValue, -64.5, -50.5, 50.5);
			this.DrawYAxisValue(gl, (yAxisMidpoint + this._viewModel.MapData.LowestValue) / 2, -64.5, -25.25, 50.5);
			this.DrawYAxisValue(gl, yAxisMidpoint, -64.5, 0, 50.5);
			this.DrawYAxisValue(gl, (this._viewModel.MapData.HighestValue + yAxisMidpoint) / 2, -64.5, 25.25, 50.5);
			this.DrawYAxisValue(gl, this._viewModel.MapData.HighestValue, -64.5, 50.5, 50.5);

			// Z axis values
			double zAxisMidpoint = (this._viewModel.MapData.HighestZAxisValue + this._viewModel.MapData.LowestZAxisValue) / 2;
			this.DrawZAxisValue(gl, this._viewModel.MapData.HighestZAxisValue, 53.5, -53.5, 50.5);
			this.DrawZAxisValue(gl, (this._viewModel.MapData.HighestZAxisValue + zAxisMidpoint) / 2, 53.5, -53.5, 25.25);
			this.DrawZAxisValue(gl, zAxisMidpoint, 53.5, -53.5, 0);
			this.DrawZAxisValue(gl, (zAxisMidpoint + this._viewModel.MapData.LowestZAxisValue) / 2, 53.5, -53.5, -25.25);
			this.DrawZAxisValue(gl, this._viewModel.MapData.LowestZAxisValue, 53.5, -53.5, -50.5);

			////this.DrawZAxisValue(this._viewModel.MapData.HighestZAxisValue, -64.5, -53.5, 50.5);
			this.DrawZAxisValue(gl, (this._viewModel.MapData.HighestZAxisValue + zAxisMidpoint) / 2, -64.5, 53.5, 25.25);
			this.DrawZAxisValue(gl, zAxisMidpoint, -64.5, 53.5, 0);
			this.DrawZAxisValue(gl, (zAxisMidpoint + this._viewModel.MapData.LowestZAxisValue) / 2, -64.5, 53.5, -25.25);
			this.DrawZAxisValue(gl, this._viewModel.MapData.LowestZAxisValue, -64.5, 53.5, -50.5);

			// Finally draw the labels for the axes
			// X axis - This is always RPM
			this.DrawXAxisLabel(gl, -6, -50.5, 75);
			this.DrawXAxisLabel(gl, -6, 50.5, -75);

			// Y Axis - The PID the user is viewing
			this.DrawYAxisLabel(gl, -15, -70, -50.5);
			this.DrawYAxisLabel(gl, -15, 70, 50.5);

			// Z Axis - Typically PRatio, but the user can select others
			this.DrawZAxisLabel(gl, -25.5, -53.5, 75);
			this.DrawZAxisLabel(gl, -25.5, 53.5, -75);
		}

        /// <summary>
        /// Apply all of the necessary transformations and rotations to display the labels on the X axis grid and
        /// render the label.
        /// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        /// <param name="value">The value to render.</param>
        /// <param name="x">The amount to shift along the X axis.</param>
        /// <param name="y">The amount to shift along the Y axis.</param>
        /// <param name="z">The amount to shift along the Z axis.</param>
        private void DrawXAxisValue(OpenGL gl, double value, double x, double y, double z)
		{
            try
            {
                gl.LoadIdentity();
                gl.Rotate(this.VerticalRotation, 1, 0, 0);
                gl.Rotate(this.HorizontalRotation + 90, 0, 1, 0);
                gl.Translate(x, y, z);
                gl.Scale(LabelFontScaleLarge, LabelFontScaleLarge, LabelFontScaleLarge);
                gl.DrawText3D(LabelFontFace, 12, 0, LabelFontExtrusion, Math.Round(value).ToString(CultureInfo.CurrentCulture));
                //gl.Scale(LabelFontScaleDefault, LabelFontScaleDefault, LabelFontScaleDefault);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in DrawXAxisValue", ex);
            }
		}

        /// <summary>
        /// Apply all of the necessary transformations and rotations to display the labels on the Y axis grid and
        /// render the label.
        /// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        /// <param name="value">The value to render.</param>
        /// <param name="x">The amount to shift along the X axis.</param>
        /// <param name="y">The amount to shift along the Y axis.</param>
        /// <param name="z">The amount to shift along the Z axis.</param>
        private void DrawYAxisValue(OpenGL gl, double value, double x, double y, double z)
        {
            try
            {
                gl.LoadIdentity();
                gl.Rotate(this.VerticalRotation, 1, 0, 0);
                gl.Rotate(this.HorizontalRotation, 0, 1, 0);
                gl.Translate(x, y, z);
                gl.Scale(LabelFontScaleLarge, LabelFontScaleLarge, LabelFontScaleLarge);
                gl.DrawText3D(LabelFontFace, 12, 0, LabelFontExtrusion, value.ToString(CultureInfo.CurrentCulture));
                //gl.Scale(LabelFontScaleDefault, LabelFontScaleDefault, LabelFontScaleDefault);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in DrawYAxisValue", ex);
            }
        }

        /// <summary>
        /// Apply all of the necessary transformations and rotations to display the labels on the Z axis grid and
        /// render the label.
        /// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        /// <param name="value">The value to render.</param>
        /// <param name="x">The amount to shift along the X axis.</param>
        /// <param name="y">The amount to shift along the Y axis.</param>
        /// <param name="z">The amount to shift along the Z axis.</param>
        private void DrawZAxisValue(OpenGL gl, double value, double x, double y, double z)
		{
            try
            {
                gl.LoadIdentity();
                gl.Rotate(this.VerticalRotation, 1, 0, 0);
                gl.Rotate(this.HorizontalRotation, 0, 1, 0);
                gl.Translate(x, y, z);
                gl.Scale(LabelFontScaleLarge, LabelFontScaleLarge, LabelFontScaleLarge);
                gl.DrawText3D(LabelFontFace, 12, 0, LabelFontExtrusion, value.ToString(CultureInfo.CurrentCulture));
                //gl.Scale(LabelFontScaleDefault, LabelFontScaleDefault, LabelFontScaleDefault);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in DrawZAxisValue", ex);
            }
        }

        /// <summary>
        /// Draws the label for the x axis at the given location.
        /// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        /// <param name="x">The x location for the label.</param>
        /// <param name="y">The y location for the label.</param>
        /// <param name="z">The z location for the label.</param>
        private void DrawXAxisLabel(OpenGL gl, double x, double y, double z)
        {
            try
            {
                gl.LoadIdentity();
                gl.Rotate(this.VerticalRotation, 1, 0, 0);
                gl.Rotate(this.HorizontalRotation, 0, 1, 0);
                gl.Translate(x, y, z);
                gl.Scale(LabelFontScaleExtraLarge, LabelFontScaleExtraLarge, LabelFontScaleExtraLarge);
                gl.DrawText3D(LabelFontFace, 12, 0, LabelFontExtrusion, this._viewModel.MapData.XAxisLabel);
                //gl.Scale(LabelFontScaleDefault, LabelFontScaleDefault, LabelFontScaleDefault);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in DrawXAxisLabel", ex);
            }
        }

        /// <summary>
        /// Draws the label for the y axis at the given location.
        /// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        /// <param name="x">The x location for the label.</param>
        /// <param name="y">The y location for the label.</param>
        /// <param name="z">The z location for the label.</param>
        private void DrawYAxisLabel(OpenGL gl, double x, double y, double z)
		{
            try
            {
                gl.LoadIdentity();
                gl.Rotate(this.VerticalRotation, 1, 0, 0);
                gl.Rotate(this.HorizontalRotation, 0, 1, 0);
                gl.Rotate(90, 0, 0, 1);
                gl.Translate(x, y, z);
                gl.Scale(LabelFontScaleExtraLarge, LabelFontScaleExtraLarge, LabelFontScaleExtraLarge);
                gl.DrawText3D(LabelFontFace, 12, 0, LabelFontExtrusion, this._viewModel.MapData.YAxisLabel);
                //gl.Scale(LabelFontScaleDefault, LabelFontScaleDefault, LabelFontScaleDefault);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in DrawYAxisLabel", ex);
            }
        }

        /// <summary>
        /// Draws the label for the z axis at the given location.
        /// </summary>
        /// <param name="gl">The <see cref="OpenGL"/> object doing the rendering.</param>
        /// <param name="x">The x location for the label.</param>
        /// <param name="y">The y location for the label.</param>
        /// <param name="z">The z location for the label.</param>
        private void DrawZAxisLabel(OpenGL gl, double x, double y, double z)
		{
            try
            {
                gl.LoadIdentity();
                gl.Rotate(this.VerticalRotation, 1, 0, 0);
                gl.Rotate(this.HorizontalRotation + 90, 0, 1, 0);
                gl.Translate(x, y, z);
                gl.Scale(LabelFontScaleExtraLarge, LabelFontScaleExtraLarge, LabelFontScaleExtraLarge);
                gl.DrawText3D(SurfaceMapDialog.LabelFontFace, 12, 0, 0.1f, this._viewModel.MapData.ZAxisLabel);
                //gl.Scale(LabelFontScaleDefault, LabelFontScaleDefault, LabelFontScaleDefault);
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("Error in DrawZAxisLabel", ex);
            }
        }

		/// <summary>
		/// Move the camera around the surface map in the provided direction.
		/// </summary>
		/// <param name="direction">The <see cref="CameraMoveDirection"/> to move the camera.</param>
		private void MoveCamera(CameraMoveDirection direction)
		{
			switch (direction)
			{
				case CameraMoveDirection.Up:
					this.VerticalRotation -= 2;
					break;
				case CameraMoveDirection.Down:
					this.VerticalRotation += 2;
					break;
				case CameraMoveDirection.Left:
					this.HorizontalRotation -= 2;
					break;
				case CameraMoveDirection.Right:
					this.HorizontalRotation += 2;
					break;
				case CameraMoveDirection.LeftAndUp:
					this.HorizontalRotation -= 2;
					this.VerticalRotation -= 2;
					break;
				case CameraMoveDirection.LeftAndDown:
					this.HorizontalRotation -= 2;
					this.VerticalRotation += 2;
					break;
				case CameraMoveDirection.RightAndUp:
					this.HorizontalRotation += 2;
					this.VerticalRotation -= 2;
					break;
				case CameraMoveDirection.RightAndDown:
					this.HorizontalRotation += 2;
					this.VerticalRotation += 2;
					break;
			}
		}

        /// <summary>
        /// Setup all of the various buffers needed to get the surface map displayed on the screen.
        /// </summary>
        private void CreateSurfaceMap()
		{
			int[] bufferSize = new int[1];
            OpenGL gl = this._openGLControl.OpenGL;

            if (this.SurfaceMapNormalData != null)
			{
				gl.GenBuffers(1, this._surfaceMapNormalBufferId);
				gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapNormalBufferId[0]);

				GCHandle normalHandle = GCHandle.Alloc(this.SurfaceMapNormalData, GCHandleType.Pinned);
				IntPtr normalPointer = normalHandle.AddrOfPinnedObject();

				gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.SurfaceMapNormalData.Length * sizeof(float), normalPointer, OpenGL.GL_STATIC_DRAW);

				gl.GetBufferParameter(OpenGL.GL_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
				if (this.SurfaceMapNormalData.Length * sizeof(float) != bufferSize[0])
				{
                    SurfaceMapDialog.Log.Error("Normal array not uploaded correctly (surface map normal data).");
                    throw new InvalidOperationException("Normal array not uploaded correctly");
				}

				gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
			}

			if (this.SurfaceMapTextureData != null)
			{
				gl.GenBuffers(1, this._surfaceMapTextureBufferId);
				gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapTextureBufferId[0]);

				GCHandle textureHandle = GCHandle.Alloc(this.SurfaceMapTextureData, GCHandleType.Pinned);
				IntPtr texturePointer = textureHandle.AddrOfPinnedObject();

				gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.SurfaceMapTextureData.Length * sizeof(float), texturePointer, OpenGL.GL_STATIC_DRAW);

				gl.GetBufferParameter(OpenGL.GL_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
				if (this.SurfaceMapTextureData.Length * sizeof(float) != bufferSize[0])
				{
                    SurfaceMapDialog.Log.Error("Texture coordinate array not uploaded correctly.");
                    throw new InvalidOperationException(CmrHisto.Properties.Resources.TextureCoodinatesNotUploaded);
				}

				gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
			}

			gl.GenBuffers(1, this._surfaceMapVertexBufferId);
			gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, this._surfaceMapVertexBufferId[0]);

			GCHandle vertexHandle = GCHandle.Alloc(this.SurfaceMapVertexData, GCHandleType.Pinned);
			IntPtr vertexPointer = vertexHandle.AddrOfPinnedObject();

			gl.BufferData(OpenGL.GL_ARRAY_BUFFER, this.SurfaceMapVertexData.Length * sizeof(float), vertexPointer, OpenGL.GL_STATIC_DRAW);

			gl.GetBufferParameter(OpenGL.GL_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
			if (this.SurfaceMapVertexData.Length * sizeof(float) != bufferSize[0])
			{
                SurfaceMapDialog.Log.Error("Vertex array not uploaded correctly (surface map vertex data).");
                throw new InvalidOperationException("Vertex array not uploaded correctly");
			}

			gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
			gl.GenBuffers(1, this._surfaceMapIndiciesBufferId);
			gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this._surfaceMapIndiciesBufferId[0]);

			GCHandle indexHandle = GCHandle.Alloc(this.SurfaceMapIndicesData, GCHandleType.Pinned);
			IntPtr indexPointer = indexHandle.AddrOfPinnedObject();

			gl.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, this.SurfaceMapIndicesData.Length * sizeof(int), indexPointer, OpenGL.GL_STATIC_DRAW);

			gl.GetBufferParameter(OpenGL.GL_ELEMENT_ARRAY_BUFFER, OpenGL.GL_BUFFER_SIZE, bufferSize);
			if (this.SurfaceMapIndicesData.Length * sizeof(int) != bufferSize[0])
			{
                SurfaceMapDialog.Log.Error("Element array not uploaded correctly (surface map indicies data).");
                throw new InvalidOperationException("Element array not uploaded correctly");
			}

			gl.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, 0);
		}

		/// <summary>
		/// Get the data and organize it so it can be displayed as a surface.
		/// </summary>
		private async Task PrepareSurfaceMapDataAsync()
		{
            this._viewModel.IsBusy = true;
			this._viewModel.ShowMapEnabled = false;
            this._viewModel.BusyText = $"Reading {this._viewModel.SelectedPidName} data.";
            this._viewModel.Title = $"CmrHisto Surface Map - {this._viewModel.SelectedPidName}";

            // Get the data
            this._viewModel.MapData = await ((CmrHistoMain)this.Owner).GetSelectedPidDataAsync(this._viewModel.SelectedPidName, this._viewModel.DataType);

            this._viewModel.BusyText = "Generating surface.";
            Stopwatch stop = new Stopwatch();
            stop.Start();
            await Task.Run(() => { this._viewModel.Triangles = Delauney.Triangulate(this._viewModel.MapData.Data); });
            stop.Stop();
            SurfaceMapDialog.Log.Info($"Generating mesh took {stop.ElapsedMilliseconds}");

			// Setup the legend
			this._viewModel.LegendHighText = this._viewModel.MapData.HighestValue.ToString(CultureInfo.CurrentCulture) + " " + this._viewModel.MapData.YAxisPIDUnit;
			this._viewModel.LegendMidText = Math.Round((this._viewModel.MapData.HighestValue + this._viewModel.MapData.LowestValue) / 2, 3).ToString(CultureInfo.CurrentCulture) + " " + this._viewModel.MapData.YAxisPIDUnit;
            this._viewModel.LegendLowText = this._viewModel.MapData.LowestValue.ToString(CultureInfo.CurrentCulture) + " " + this._viewModel.MapData.YAxisPIDUnit;

			this._viewModel.LegendVisibility = Visibility.Visible;

			// Setup all of the buffer objects.
			this.SurfaceMapVertexData = new float[this._viewModel.MapData.Data.Count * 3];
			this.SurfaceMapTextureData = new float[this._viewModel.MapData.Data.Count * 2];
			this.SurfaceMapNormalData = new float[this._viewModel.MapData.Data.Count * 3];
			int vertexIndex = 0;
			int textureIndex = 0;
			int normalIndex = 0;
			foreach (Triangulator.Point p in this._viewModel.MapData.Data)
			{
				this.SurfaceMapVertexData[vertexIndex] = (float)p.X;
				vertexIndex++;
				this.SurfaceMapVertexData[vertexIndex] = (float)p.Value;
				vertexIndex++;
				this.SurfaceMapVertexData[vertexIndex] = (float)p.Y;
				vertexIndex++;

				this.SurfaceMapTextureData[textureIndex] = 0f;
				textureIndex++;
				this.SurfaceMapTextureData[textureIndex] = SurfaceMapDialog.CalculateYTextureValue(p.Value);
				textureIndex++;

				// Just make all of the normals 0, 0, 1.
				this.SurfaceMapNormalData[normalIndex] = 0f;
				normalIndex++;
				this.SurfaceMapNormalData[normalIndex] = 0f;
				normalIndex++;
				this.SurfaceMapNormalData[normalIndex] = 1f;
				normalIndex++;
			}

			this.SurfaceMapIndicesData = new uint[this._viewModel.Triangles.Count * 3];
			int indexIndex = 0;
			foreach (Triangulator.Triangle t in this._viewModel.Triangles)
			{
				this.SurfaceMapIndicesData[indexIndex] = (uint)t.PointOne;
				indexIndex++;
				this.SurfaceMapIndicesData[indexIndex] = (uint)t.PointTwo;
				indexIndex++;
				this.SurfaceMapIndicesData[indexIndex] = (uint)t.PointThree;
				indexIndex++;
			}

            this._viewModel.BusyText = "Rendering surface map.";
			this.CreateSurfaceMap();

			this.XWindow = 50;
			this.YWindow = 50;
			this.ZWindow = 50;

			// Reset the rotation info.
			this.VerticalRotation = SurfaceMapDialog.DefaultVerticalRotation;
			this.HorizontalRotation = SurfaceMapDialog.DefaultHorizontalRotation;
            this._viewModel.ShowMapEnabled = true;
            this._viewModel.IsBusy = false;
		}
		#endregion

		#region Internal Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="SurfaceMapDialog"/> class.
		/// </summary>
        /// <param name="pidNames">The list of PID names.</param>
		internal SurfaceMapDialog(IEnumerable<string> pidNames)
		{
            string legendPath = Path.Combine(Directory.GetCurrentDirectory(), "gradient.bmp");
            if (!File.Exists(legendPath))
            {
                legendPath = @"..\Resources\gradient.bmp";
            }

            this._viewModel = new SurfaceMapDialogViewModel()
            {
                DataType = Enums.DataType.Average,
                LegendImagePath = legendPath,
                LegendVisibility = Visibility.Hidden,
                PidNames = pidNames,
                RenderMode = "Surface",
                ShowColors = true,
                ShowColorsVisibility = Visibility.Hidden,
                ShowLines = true,
                ShowLinesVisibility = Visibility.Visible,
                Title = "CmrHisto Surface Map"
            };

            this.DataContext = this._viewModel;

            try
            {
                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                SurfaceMapDialog.Log.Error("InitializeComponent failed!", ex);
            }

			this._viewModel.Triangles = new List<Triangle>();

			// Create all the data for drawing the thick and thin lines along the axes.
			this.ThickAxesVertexData = new float[48]
			{
				-50.5f, -50.5f, 50.5f,
				50.5f, -50.5f, 50.5f,
				-50.5f, -50.5f, 0f,
				50.5f, -50.5f, 0f,
				-50.5f, -50.5f, -50.5f,
				50.5f, -50.5f, -50.5f,
				-50.5f, 0f, -50.5f,
				50.5f, 0f, -50.5f,
				-50.5f, 50.5f, -50.5f,
				50.5f, 50.5f, -50.5f,
				0f, -50.5f, -50.5f,
				0f, -50.5f, 50.5f,
				-50.5f, 0f, 50.5f,
				-50.5f, 50.5f, 50.5f,
				0f, 50.5f, -50.5f,
				-50.5f, 50.5f, 0f
			};

			this.ThickAxesLinesIndices = new uint[30]
			{
				0, 1,
				2, 3,
				4, 5,
				6, 7,
				8, 9,
				5, 1,
				10, 11,
				4, 0,
				6, 12,
				8, 13,
				4, 8,
				10, 14,
				5, 9,
				2, 15,
				0, 13
			};

			this.ThinAxesVertexData = new float[54]
			{
				-50.5f, -50.5f, 25.25f,
				50.5f, -50.5f, 25.25f,
				-50.5f, -50.5f, -25.25f,
				50.5f, -50.5f, -25.25f,
				-50.5f, -25.25f, -50.5f,
				50.5f, -25.25f, -50.5f,
				-50.5f, 25.25f, -50.5f,
				50.5f, 25.25f, -50.5f,
				25.25f, -50.5f, -50.5f,
				25.25f, -50.5f, 50.5f,
				-25.25f, -50.5f, -50.5f,
				-25.25f, -50.5f, 50.5f,
				-50.5f, -25.25f, 50.5f,
				-50.5f, 25.25f, 50.5f,
				-25.25f, 50.5f, -50.5f,
				25.25f, 50.5f, -50.5f,
				-50.5f, 50.5f, -25.25f,
				-50.5f, 50.5f, 25.25f
			};

			this.ThinAxesLinesIndices = new uint[24]
			{
				0, 1,
				2, 3,
				4, 5,
				6, 7,
				8, 9,
				10, 11,
				4, 12,
				6, 13,
				10, 14,
				8, 15,
				2, 16,
				0, 17
			};
		}
        #endregion
    }
}