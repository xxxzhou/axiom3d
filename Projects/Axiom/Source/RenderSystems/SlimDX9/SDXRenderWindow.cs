#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: D3DWindow.cs 1445 2008-12-02 19:25:22Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using SWF = System.Windows.Forms;

using Axiom.Core;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Media;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.SlimDX9
{
    /// <summary>
    /// The Direct3D implementation of the RenderWindow class.
    /// </summary>
    public class SDXRenderWindow : RenderWindow
    {
        #region Fields

        private SWF.Control _window;			// Win32 Window handle
		private bool _isExternal;			// window not created by Ogre
		private bool _sizing;
		private bool _isSwapChain;			// Is this a secondary window?
		
		// -------------------------------------------------------
		// DirectX-specific
		// -------------------------------------------------------

		// Pointer to swap chain, only valid if IsSwapChain
		private D3D.SwapChain _swapChain;
		
		private D3D.PresentParameters _d3dpp;
		public D3D.PresentParameters PresentationParameters
		{
			get{return _d3dpp;}
		}
		
        /// <summary>Used to provide support for multiple RenderWindows per device.</summary>
        private D3D.Surface _renderZBuffer;
        private D3D.MultisampleType _fsaaType;
        private int _fsaaQuality;
		private int _displayFrequency;
		private bool _vSync;
		private bool _useNVPerfHUD;
		
        
        private Driver _driver;
		/// <summary>
		/// Get the current Driver
		/// </summary>
		public Driver Driver
		{
			get{return _driver;}
		}

		/// <summary>
		/// Gets the active DirectX Device
		/// </summary>
		public D3D.Device D3DDevice
		{
			get{return _driver.D3DDevice;}
		}
		
		private bool _isClosed;
        public override bool IsClosed
        {
            get
            {
                return _isClosed;
            }
        }
        
        private D3D.Surface _renderSurface;
		public D3D.Surface RenderSurface
		{
			get{return _renderSurface;}
		}

		public bool IsVisible
		{
			get
			{
				if ( _window != null )
				{
					if ( _isExternal )
					{
						if ( _window is SWF.Form )
						{
							if ( ( (SWF.Form)_window ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
						else if ( _window is SWF.PictureBox )
						{
							SWF.Control parent = _window.Parent;
							while ( !( parent is SWF.Form ) )
								parent = parent.Parent;

							if ( ( (SWF.Form)parent ).WindowState == SWF.FormWindowState.Minimized )
							{
								return false;
							}
						}
					}
					else
					{
						if ( ( (SWF.Form)_window ).WindowState == SWF.FormWindowState.Minimized )
						{
							return false;
						}
					}
				}
				else
					return false;

				return true;
			}
		}
		
        #endregion Fields

        internal D3D.Direct3D manager;

        #region Constructor

        /// <summary>
		/// 
		/// </summary>
		/// <param name="driver">The root driver</param>
		public SDXRenderWindow( Driver driver )
		{
			_driver = driver;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="driver">The root driver</param>
		/// <param name="deviceIfSwapChain">The existing D3D device to create an additional swap chain from, if this is not	the first window.</param>
		public SDXRenderWindow(D3D.Direct3D manager, Driver driver, D3D.Device deviceIfSwapChain )
			: this( driver )
		{
			this.manager = manager;
			_isSwapChain = ( deviceIfSwapChain != null );
		}
		

        #endregion
        
		private bool _checkMultiSampleQuality( D3D.MultisampleType type, out int outQuality, D3D.Format format, int adapterNum, D3D.DeviceType deviceType, bool fullScreen )
		{
			DX.Result result;

			manager.CheckDeviceMultisampleType( adapterNum, deviceType, format, fullScreen, type, out outQuality, out result );

			if ( result.IsSuccess )
				return true;
			return false;
		}
		
        
        #region RenderWindow implementation
        
        /// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="colorDepth"></param>
		/// <param name="isFullScreen"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="depthBuffer"></param>height
		/// <param name="miscParams"></param>
        public override void Create(string name, int width, int height, bool fullScreen, Axiom.Collections.NamedParameterList miscParams)
        {
        	SWF.Control parentHWnd = null;
			SWF.Control externalHWnd = null;
			String title = name;
			int colourDepth = 32;
			int left = -1; // Defaults to screen center
			int top = -1; // Defaults to screen center
			bool depthBuffer = true;
			string border = "";
			bool outerSize = false;

			_useNVPerfHUD = false;
			_fsaaType = D3D.MultisampleType.None;
			_fsaaQuality = 0;
			_vSync = false;
			
			if ( miscParams != null )
			{
				// left (x)
				if ( miscParams.ContainsKey( "left" ) )
				{
					left = Int32.Parse( miscParams[ "left" ].ToString() );
				}

				// top (y)
				if ( miscParams.ContainsKey( "top" ) )
				{
					top = Int32.Parse( miscParams[ "top" ].ToString() );
				}

				// Window title
				if ( miscParams.ContainsKey( "title" ) )
				{
					title = (string)miscParams[ "title" ];
				}

				// parentWindowHandle		-> parentHWnd
				if ( miscParams.ContainsKey( "parentWindowHandle" ) )
				{
					object handle = miscParams[ "parentWindowHandle" ];
					IntPtr ptr = IntPtr.Zero;
					if ( handle.GetType() == typeof( IntPtr ) )
					{
						ptr = (IntPtr)handle;
					}
					else if ( handle.GetType() == typeof( System.Int32 ) )
					{
						ptr = new IntPtr( (int)handle );
					}
					parentHWnd = SWF.Control.FromHandle( ptr );
					//parentHWnd = (SWF.Control)miscParams[ "parentWindowHandle" ];
				}

				// externalWindowHandle		-> externalHWnd
				if ( miscParams.ContainsKey( "externalWindowHandle" ) )
				{
					object handle = miscParams[ "externalWindowHandle" ];
					IntPtr ptr = IntPtr.Zero;
					if ( handle.GetType() == typeof( IntPtr ) )
					{
						ptr = (IntPtr)handle;
					}
					else if ( handle.GetType() == typeof( System.Int32 ) )
					{
						ptr = new IntPtr( (int)handle );
					}
					externalHWnd = SWF.Control.FromHandle( ptr );
				}

				// vsync	[parseBool]
				if ( miscParams.ContainsKey( "vsync" ) )
				{
					_vSync = bool.Parse( miscParams[ "vsync" ].ToString() );
				}

				// displayFrequency
				if ( miscParams.ContainsKey( "displayFrequency" ) )
				{
					_displayFrequency = Int32.Parse( miscParams[ "displayFrequency" ].ToString() );
				}

				// colourDepth
				if ( miscParams.ContainsKey( "colorDepth" ) )
				{
					this.ColorDepth = Int32.Parse( miscParams[ "colorDepth" ].ToString() );
				}

				// depthBuffer [parseBool]
				if ( miscParams.ContainsKey( "depthBuffer" ) )
				{
					depthBuffer = bool.Parse( miscParams[ "depthBuffer" ].ToString() );
				}

				// FSAA type
				if ( miscParams.ContainsKey( "FSAA" ) )
				{
					_fsaaType = (D3D.MultisampleType)miscParams[ "FSAA" ];
				}

				// FSAA quality
				if ( miscParams.ContainsKey( "FSAAQuality" ) )
				{
					_fsaaQuality = Int32.Parse( miscParams[ "FSAAQuality" ].ToString() );
				}

				// window border style
				if ( miscParams.ContainsKey( "border" ) )
				{
					border = ((string)miscParams[ "border" ]).ToLower();
				}

				// set outer dimensions?
				if ( miscParams.ContainsKey( "outerDimensions" ) )
				{
					outerSize = bool.Parse( miscParams[ "outerDimensions" ].ToString() );
				}

				// NV perf HUD?
				if ( miscParams.ContainsKey( "useNVPerfHUD" ) )
				{
					_useNVPerfHUD = bool.Parse( miscParams[ "useNVPerfHUD" ].ToString() );
				}

			}
			
			if ( _window != null )
				Dispose();
			
			if ( externalHWnd == null )
			{
				Width = width;
				Height = height;
				top = top;
				left = left;

				_isExternal = false;
				DefaultForm newWin = new DefaultForm();
				newWin.Text = title;

				/* If we're in fullscreen, we can use the device's back and stencil buffers.
				 * If we're in windowed mode, we'll want our own.
				 * get references to the render target and depth stencil surface
				 */
				if ( !isFullScreen )
				{
					newWin.StartPosition = SWF.FormStartPosition.CenterScreen;
					if ( parentHWnd != null )
					{
						newWin.Parent = parentHWnd;
					}
					else
					{
                        if( border == "none")
                        {
                            newWin.FormBorderStyle = SWF.FormBorderStyle.None;
                        }
                        else if ( border == "fixed" )
                        {
                            newWin.FormBorderStyle = SWF.FormBorderStyle.FixedSingle;
                            newWin.MaximizeBox = false;
                        }
					}

					if ( !outerSize )
					{
						newWin.ClientSize = new System.Drawing.Size( Width, Height );
					}
					else
					{
						newWin.Width = Width;
						newWin.Height = Height;
					}

					if ( top < 0 )
						top = ( SWF.Screen.PrimaryScreen.Bounds.Height - Height ) / 2;
					if ( left < 0 )
						left = ( SWF.Screen.PrimaryScreen.Bounds.Width - Width ) / 2;


				}
				else
				{
					//dwStyle |= WS_POPUP;
					top = left = 0;
				}

				// Create our main window
				newWin.Top = top;
				newWin.Left = left;

				newWin.RenderWindow = this;
				_window = newWin;

				WindowEventMonitor.Instance.RegisterWindow( this );
			}
			else
			{
				_window = externalHWnd;
				_isExternal = true;
			}

			
			// set the params of the window
			this.Name = name;
			this.ColorDepth = ColorDepth;
			this.Width = width;
			this.Height = height;
			this.IsFullScreen = isFullScreen;
			this.isDepthBuffered = depthBuffer;
			this.top = top;
			this.left = left;

			LogManager.Instance.Write( "SlimDX : Created SlimDX Rendering Window '{0}' : {1}x{2}, {3}bpp", Name, Width, Height, ColorDepth );
			
			CreateD3DResources();

            _window.Show();

			IsActive = true;
			_isClosed = false;
        }
        
        //FIXME
        public void CreateD3DResources()
        {
            D3D.Device device = _driver.D3DDevice;

            if ( _isSwapChain && device == null )
            {
                throw new Exception( "Secondary window has not been given the device from the primary!" );
            }

            if ( _renderSurface != null )
            {
                _renderSurface.Dispose();
                _renderSurface = null;
            }

            if ( _driver.Description.ToLower().Contains( "nvperfhud" ) )
            {
                _useNVPerfHUD = true;
            }

            D3D.DeviceType devType = D3D.DeviceType.Hardware;

            _d3dpp = new D3D.PresentParameters();

            _d3dpp.Windowed = !IsFullScreen;
            _d3dpp.SwapEffect = D3D.SwapEffect.Discard;
            _d3dpp.BackBufferCount = _vSync ? 2 : 1;
            _d3dpp.EnableAutoDepthStencil = isDepthBuffered;
            _d3dpp.DeviceWindowHandle = _window.Handle;
            _d3dpp.BackBufferHeight = Height;
            _d3dpp.BackBufferWidth = Width;
            _d3dpp.FullScreenRefreshRateInHertz = IsFullScreen ? _displayFrequency : 0;

            if ( _vSync )
            {
                _d3dpp.PresentationInterval = D3D.PresentInterval.One;
            }
            else
            {
                // NB not using vsync in windowed mode in D3D9 can cause jerking at low 
                // frame rates no matter what buffering modes are used (odd - perhaps a
                // timer issue in D3D9 since GL doesn't suffer from this) 
                // low is < 200fps in this context
                if ( !IsFullScreen )
                {
                    LogManager.Instance.Write( "SlimDX : WARNING - disabling VSync in windowed mode can cause timing issues at lower frame rates, turn VSync on if you observe this problem." );
                }
                _d3dpp.PresentationInterval = D3D.PresentInterval.Immediate;
            }

            _d3dpp.BackBufferFormat = D3D.Format.R5G6B5;
            if ( ColorDepth > 16 )
            {
                _d3dpp.BackBufferFormat = D3D.Format.X8R8G8B8;
            }
            if ( ColorDepth > 16 )
            {
                // Try to create a 32-bit depth, 8-bit stencil

                if ( manager.CheckDeviceFormat( _driver.AdapterNumber, devType, _d3dpp.BackBufferFormat, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, D3D.Format.D24S8 ) == false )
                {
                    // Bugger, no 8-bit hardware stencil, just try 32-bit zbuffer 
                    if ( manager.CheckDeviceFormat( _driver.AdapterNumber, devType, _d3dpp.BackBufferFormat, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, D3D.Format.D32 ) == false )
                    {
                        // Jeez, what a naff card. Fall back on 16-bit depth buffering
                        _d3dpp.AutoDepthStencilFormat = D3D.Format.D16;
                    }
                    else
                    {
                        _d3dpp.AutoDepthStencilFormat = D3D.Format.D32;
                    }
                }
                else
                {
                    // Woohoo!
                    if ( manager.CheckDepthStencilMatch( _driver.AdapterNumber, devType, _d3dpp.BackBufferFormat, _d3dpp.BackBufferFormat, D3D.Format.D24S8 ) == true )
                    {
                        _d3dpp.AutoDepthStencilFormat = D3D.Format.D24S8;
                    }
                    else
                    {
                        _d3dpp.AutoDepthStencilFormat = D3D.Format.D24X8;
                    }
                }
            }
            else
            {
                // 16-bit depth, software stencil
                _d3dpp.AutoDepthStencilFormat = D3D.Format.D16;
            }

            _d3dpp.Multisample = _fsaaType;
            _d3dpp.MultisampleQuality = _fsaaQuality;

            if ( _isSwapChain )
            {
                // Create swap chain	
                try
                {
                    _swapChain = new D3D.SwapChain( device, _d3dpp );
                }
                catch ( Exception )
                {
                    // Try a second time, may fail the first time due to back buffer count,
                    // which will be corrected by the runtime
                    try
                    {
                        _swapChain = new D3D.SwapChain( device, _d3dpp );
                    }
                    catch ( Exception ex )
                    {
                        throw new Exception( "Unable to create an additional swap chain", ex );
                    }
                }

                // Store references to buffers for convenience
                _renderSurface = _swapChain.GetBackBuffer( 0 );

                // Additional swap chains need their own depth buffer
                // to support resizing them
                if ( isDepthBuffered )
                {
                    bool discard = ( _d3dpp.PresentFlags & D3D.PresentFlags.DiscardDepthStencil ) == 0;

                    try
                    {
                        _renderZBuffer = D3D.Surface.CreateDepthStencil( device, Width, Height, _d3dpp.AutoDepthStencilFormat, _d3dpp.Multisample, _d3dpp.MultisampleQuality, discard );
                    }
                    catch ( Exception )
                    {
                        throw new Exception( "Unable to create a depth buffer for the swap chain" );
                    }
                }
            }
            else
            {
                if ( device == null ) // We haven't created the device yet, this must be the first time
                {
                    // Turn off default event handlers, since Managed DirectX seems confused.

                    //D3D.Device.IsUsingEventHandlers = true;

                    // Do we want to preserve the FPU mode? Might be useful for scientific apps
                    D3D.CreateFlags extraFlags = 0;
                    ConfigOptionCollection configOptions = Root.Instance.RenderSystem.ConfigOptions;
                    ConfigOption FPUMode = configOptions[ "Floating-point mode" ];
                    if ( FPUMode.Value == "Consistent" )
                        extraFlags |= D3D.CreateFlags.FpuPreserve;

                    // Set default settings (use the one Ogre discovered as a default)
                    int adapterToUse = Driver.AdapterNumber;

                    if ( this._useNVPerfHUD )
                    {
                        // Look for 'NVIDIA NVPerfHUD' adapter
                        // If it is present, override default settings
                        foreach ( D3D.AdapterInformation adapter in manager.Adapters )
                        {
                            LogManager.Instance.Write( "D3D : NVIDIA PerfHUD requested, checking adapter {0}:{1}", adapter.Adapter, adapter.Details.Description );
                            if ( adapter.Details.Description.ToLower().Contains( "perfhud" ) )
                            {
                                LogManager.Instance.Write( "D3D : NVIDIA PerfHUD requested, using adapter {0}:{1}", adapter.Adapter, adapter.Details.Description );
                                adapterToUse = adapter.Adapter;
                                devType = D3D.DeviceType.Reference;
                                break;
                            }
                        }
                    }

                    // create the D3D Device, trying for the best vertex support first, and settling for less if necessary
                    try
                    {
                        // hardware vertex processing
                        device = new D3D.Device( manager, adapterToUse, devType, _window.Handle, D3D.CreateFlags.HardwareVertexProcessing | extraFlags, _d3dpp );
                    }
                    catch ( Exception )
                    {
                        try
                        {
                            // Try a second time, may fail the first time due to back buffer count,
                            // which will be corrected down to 1 by the runtime
                            device = new D3D.Device( manager, adapterToUse, devType, _window.Handle, D3D.CreateFlags.HardwareVertexProcessing | extraFlags, _d3dpp );
                        }
                        catch ( Exception )
                        {
                            try
                            {
                                // doh, how bout mixed vertex processing
                                device = new D3D.Device( manager, adapterToUse, devType, _window.Handle, D3D.CreateFlags.MixedVertexProcessing | extraFlags, _d3dpp );
                            }
                            catch ( Exception )
                            {
                                try
                                {
                                    // what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
                                    // anything at all since they obviously don't have a video card installed
                                    device = new D3D.Device( manager, adapterToUse, devType, _window.Handle, D3D.CreateFlags.SoftwareVertexProcessing | extraFlags, _d3dpp );
                                }
                                catch ( Exception ex )
                                {
                                    throw new Exception( "Failed to create Direct3D9 Device", ex );
                                }
                            }
                        }
                    }

                    //device.DeviceResizing += new System.ComponentModel.CancelEventHandler( OnDeviceResizing );
                }
                // update device in driver
                Driver.D3DDevice = device;
                // Store references to buffers for convenience
                _renderSurface = device.GetRenderTarget( 0 );
                _renderZBuffer = device.DepthStencilSurface;
                // release immediately so we don't hog them
                //_renderZBuffer.ReleaseDC( _renderZBuffer.GetDC() );

                //device.DeviceReset += new EventHandler( OnResetDevice );

            }
        }

        public override object this[ string attribute ]
		{
        	get
			{
				switch ( attribute.ToUpper() )
				{
						case "D3DDEVICE":
							return _driver.D3DDevice;
							
						case "WINDOW":
							return this._window.Handle;
						
						case "ISTEXTURE":
							return false;
						case "D3DZBUFFER":
							return _renderZBuffer;
							
						case "D3DBACKBUFFER":
	                        D3D.Surface[] surface = new D3D.Surface[ 1 ];
	                        surface[ 0 ] = _renderSurface;
							return surface;

						case "D3DFRONTBUFFER":
							return _renderSurface;
	
				}
				
				return new NotSupportedException( "There is no SlimDX RenderWindow custom attribute named " + attribute );
        	}
        }

        public void DisposeD3DResources()
		{
			// Dispose D3D Resources
			if ( _isSwapChain )
			{
				_renderZBuffer.Dispose();
				_renderZBuffer = null;
				_swapChain.Dispose();
				_swapChain = null;
			}
			else
			{
				_renderZBuffer = null;
			}
			_renderSurface.Dispose();
		}
        
        /// <summary>
        /// 
        /// </summary>
		protected override void dispose(bool disposeManagedResources)
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					DisposeD3DResources();

					// Dispose Other resources
					if ( _window != null && !_isExternal )
					{
						WindowEventMonitor.Instance.UnregisterWindow( this );
						( (SWF.Form)_window ).Dispose();
					}
				}

				// make sure this window is no longer active
				_window = null;
				IsActive = false;
				_isClosed = true;

			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );

			isDisposed = true;
		}
		

        public override void Reposition( int left, int right )
        {
            if ( _window != null && !IsFullScreen )
			{
				_window.Location = new System.Drawing.Point( left, right );
			}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public override void Resize( int width, int height )
        {
            // CMH 4/24/2004 - Start
			width = width < 10 ? 10 : width;
			height = height < 10 ? 10 : height;
			this.Height = height;
			this.Width = width;

        }

        public override void WindowMovedOrResized()
		{
			if ( GetForm( _window ) == null || GetForm( _window ).WindowState == SWF.FormWindowState.Minimized )
				return;

        }
        
        private SWF.Form GetForm( SWF.Control windowHandle )
		{
			SWF.Control tmp = windowHandle;

			if ( windowHandle == null )
				return null;
			if ( tmp is SWF.Form )
				return (SWF.Form)tmp;
			do
			{
				tmp = tmp.Parent;
			} while ( !( tmp is SWF.Form ) );

			return (SWF.Form)tmp;
		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="waitForVSync"></param>
        public override void SwapBuffers( bool waitForVSync )
        {
        	D3D.Device device = _driver.D3DDevice;
            try
            {
            	
	        	if ( device != null )
				{
	        		// tests coop level to make sure we are ok to render
					DX.Result result = device.TestCooperativeLevel();
					if(result.IsSuccess)
					{
						if ( _isSwapChain )
						{
							_swapChain.Present(D3D.Present.None);
						}
						else
						{
							device.Present();
						}
					}
	        	}
            }
            catch ( SlimDX.SlimDXException dlx ) //catch ( D3D.DeviceLostException dlx )
            {
                if (dlx.ResultCode == SlimDX.Direct3D9.ResultCode.DeviceLost)
                {
                	_renderSurface.ReleaseDC(_renderSurface.GetDC());
                	( (SDXRenderSystem)( Root.Instance.RenderSystem ) ).notifyDeviceLost();
                    Console.WriteLine(dlx.ToString());
                }

                else if (dlx.ResultCode == SlimDX.Direct3D9.ResultCode.DeviceNotReset)
                {
                    Console.WriteLine(dlx.ToString());
                    device.Reset( _swapChain.PresentParameters );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IsFullScreen
        {
            get
            {
                return base.IsFullScreen;
            }
        }

        public override void CopyContentsToMemory( PixelBox dst, FrameBuffer buffer )
        {
        	if ( ( dst.Left < 0 ) || ( dst.Right > Width ) ||
				( dst.Top < 0 ) || ( dst.Bottom > Height ) ||
				( dst.Front != 0 ) || ( dst.Back != 1 ) )
			{
				throw new Exception( "Invalid box." );
			}

			D3D.Device device = Driver.D3DDevice;
			D3D.Surface surface, tmpSurface = null;
			DX.DataRectangle stream;
			int pitch;
			D3D.SurfaceDescription desc;
			DX.DataBox lockedBox;
			
			if ( buffer == RenderTarget.FrameBuffer.Auto )
			{
				buffer = RenderTarget.FrameBuffer.Front;
			}
			
			if ( buffer == RenderTarget.FrameBuffer.Front )
			{
				//FIXME
				D3D.DisplayMode mode = device.GetDisplayMode(0);
				
				desc = new D3D.SurfaceDescription();
				desc.Width = mode.Width;
				desc.Height = mode.Height;
				desc.Format = D3D.Format.A8R8G8B8;
				
				// create a temp surface which will hold the screen image
				surface = D3D.Surface.CreateOffscreenPlain( device, mode.Width, mode.Height, D3D.Format.A8R8G8B8, D3D.Pool.SystemMemory );
				
				// get the entire front buffer.  This is SLOW!!
            	device.GetFrontBufferData( 0, surface );
            	
            	if ( IsFullScreen )
				{
					if ( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
					{
						stream = surface.LockRectangle( D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
					}
					else
					{
						Rectangle rect = new Rectangle();
						rect.Left = dst.Left;
						rect.Right = dst.Right;
						rect.Top= dst.Top;
						rect.Bottom = dst.Bottom;

						stream = surface.LockRectangle( SDXHelper.ToRectangle( rect ), D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock);
					}
				}
            	else
				{
					Rectangle srcRect = new Rectangle();
					srcRect.Left = dst.Left;
					srcRect.Right = dst.Right;
					srcRect.Top = dst.Top;
					srcRect.Bottom = dst.Bottom;
					// Adjust Rectangle for Window Menu and Chrome
					SWF.Control control = (SWF.Control)_window;
					System.Drawing.Point point = new System.Drawing.Point();
					point.X = (int)srcRect.Left;
					point.Y = (int)srcRect.Top;
					point = control.PointToScreen( point );
					srcRect.Top = point.Y;
					srcRect.Left = point.X;
					srcRect.Bottom += point.Y;
					srcRect.Right += point.X;

					desc.Width = (int)(srcRect.Right - srcRect.Left);
					desc.Height = (int)(srcRect.Bottom - srcRect.Top);

					stream = surface.LockRectangle( SDXHelper.ToRectangle(srcRect), D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
				}
			}
			else
			{
				surface = device.GetBackBuffer( 0, 0 );
				desc = surface.Description;
				
				tmpSurface = D3D.Surface.CreateOffscreenPlain( device, desc.Width, desc.Height, desc.Format, D3D.Pool.SystemMemory );
				if ( desc.MultisampleType == D3D.MultisampleType.None )
				{
					device.GetRenderTargetData( surface, tmpSurface );
				}
				else
				{
					D3D.Surface stretchSurface;
					Rectangle rect = new Rectangle();
					stretchSurface = D3D.Surface.CreateRenderTarget( device, desc.Width, desc.Height, desc.Format, D3D.MultisampleType.None, 0, false );
					device.StretchRectangle( tmpSurface, SDXHelper.ToRectangle( rect ), stretchSurface, SDXHelper.ToRectangle( rect ), D3D.TextureFilter.None );
					device.GetRenderTargetData( stretchSurface, tmpSurface );
					stretchSurface.Dispose();
				}
				
				if ( ( dst.Left == 0 ) && ( dst.Right == Width ) && ( dst.Top == 0 ) && ( dst.Bottom == Height ) )
				{
					stream = tmpSurface.LockRectangle( D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock);
				}
				else
				{
					Rectangle rect = new Rectangle();
					rect.Left = dst.Left;
					rect.Right = dst.Right;
					rect.Top = dst.Top;
					rect.Bottom = dst.Bottom;

					stream = tmpSurface.LockRectangle( SDXHelper.ToRectangle(rect), D3D.LockFlags.ReadOnly | D3D.LockFlags.NoSystemLock );
				}
			}
			
			PixelFormat format = SDXHelper.ConvertEnum( desc.Format );

			if ( format == PixelFormat.Unknown )
			{
				if ( tmpSurface != null )
					tmpSurface.Dispose();
				throw new Exception( "Unsupported format" );
			}

			PixelBox src = new PixelBox( dst.Width, dst.Height, 1, format, stream.Data.DataPointer );
			src.RowPitch = stream.Pitch / PixelUtil.GetNumElemBytes( format );
			src.SlicePitch = desc.Height * src.RowPitch;
				
			PixelConverter.BulkPixelConversion( src, dst );
			surface.Dispose();
			if ( tmpSurface != null )
				tmpSurface.Dispose();
        }
        
        
        private void OnResetDevice( object sender, EventArgs e )
        {
            D3D.Device resetDevice = (D3D.Device)sender;

            // Turn off culling, so we see the front and back of the triangle
            //resetDevice.RenderState.CullMode = D3D.Cull.None;
            resetDevice.SetRenderState(SlimDX.Direct3D9.RenderState.CullMode, D3D.Cull.None);
            // Turn on the ZBuffer
            //resetDevice.RenderState.ZBufferEnable = true;
            resetDevice.SetRenderState(SlimDX.Direct3D9.RenderState.ZWriteEnable, true);
            //resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
            resetDevice.SetRenderState(SlimDX.Direct3D9.RenderState.Lighting, true);

        }

        private void OnDeviceResizing( object sender, System.ComponentModel.CancelEventArgs e )
		{
        	
        }
        public override void Update( bool swapBuffers )
        {
            SDXRenderSystem rs = (SDXRenderSystem)Root.Instance.RenderSystem;

            // access device through driver
            D3D.Device device = _driver.D3DDevice;

            if ( rs.IsDeviceLost )
            {
                try
                {
                    device.TestCooperativeLevel();
                }
                catch ( SlimDX.SlimDXException dlx )
                {
                    if ( dlx.ResultCode == D3D.ResultCode.DeviceLost )
                    {
                        // device lost, and we can't reset
                        // can't do anything about it here, wait until we get 
                        // D3DERR_DEVICENOTRESET; rendering calls will silently fail until 
                        // then (except Present, but we ignore device lost there too)
                        _renderSurface.ReleaseDC( _renderSurface.GetDC() );
                        // need to release if swap chain
                        if ( !_isSwapChain )
                            _renderZBuffer = null;
                        else
                            _renderZBuffer.ReleaseDC( _renderZBuffer.GetDC() );
                        System.Threading.Thread.Sleep( 50 );
                        return;
                    }
                    else
                    {
                        // device lost, and we can reset
                        rs.RestoreLostDevice();

                        // Still lost?
                        if ( rs.IsDeviceLost )
                        {
                            // Wait a while
                            System.Threading.Thread.Sleep( 50 );
                            return;
                        }

                        if ( !_isSwapChain )
                        {
                            // re-qeuery buffers
                            _renderSurface = device.GetRenderTarget( 0 );
                            _renderZBuffer = device.DepthStencilSurface;
                            // release immediately so we don't hog them
                            _renderZBuffer.ReleaseDC( _renderZBuffer.GetDC() );
                        }
                        else
                        {
                            // Update dimensions incase changed
                            foreach ( Viewport entry in this.viewportList )
                            {
                                entry.UpdateDimensions();
                            }
                        }
                    }
                }
            }

            base.Update( swapBuffers );
        }

        #endregion
    }
}