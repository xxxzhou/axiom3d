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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.FileSystem;
using Axiom.Scripting;
using Axiom.Graphics;

using Real = System.Single;

#endregion Namespace Declarations

#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="OgreOverlayManager.h"   revision="1.23.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
///     <file name="OgreOverlayManager.cpp" revision="1.39.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion

namespace Axiom.Overlays
{
	/// <summary>
	///    Manages Overlay objects, parsing them from Ogre .overlay files and
	///    storing a lookup library of them. Also manages the creation of 
	///    OverlayContainers and OverlayElements, used for non-interactive 2D 
	///	   elements such as HUDs.
	/// </summary>
	public sealed class OverlayManager : Singleton<OverlayManager>, IScriptLoader
	{
		#region Fields and Properties

		private List<string> _loadedScripts = new List<string>();
		private Dictionary<string, IOverlayElementFactory> _elementFactories = new Dictionary<string, IOverlayElementFactory>();

		#region Overlays Property

		private Dictionary<string, Overlay> _overlays = new Dictionary<string, Overlay>();
		/// <summary>
		/// returns all existing overlays
		/// </summary>
		public IEnumerator<Overlay> Overlays
		{
			get
			{
				return _overlays.Values.GetEnumerator();
			}
		}

		#endregion Overlays Property

		#region ElementInstances Property

		private Dictionary<string, OverlayElement> _elementInstances = new Dictionary<string, OverlayElement>();
		/// <summary>
		/// returns all elemnt instances
		/// </summary>
		public IEnumerator<OverlayElement> ElementInstances
		{
			get
			{
				return _elementInstances.Values.GetEnumerator();
			}
		}

		#endregion ElementInstances Property

		#region ElementTemplates Property

		private Dictionary<string, OverlayElement> _elementTemplates = new Dictionary<string, OverlayElement>();
		/// <summary>
		/// returns all element templates
		/// </summary>
		public IEnumerator<OverlayElement> ElementTemplates
		{
			get
			{
				return _elementTemplates.Values.GetEnumerator();
			}
		}

		#endregion ElementTemplates Property

		#region HasViewportChanged Property

		private bool _viewportDimensionsChanged;
		/// <summary>
		///		Gets if the viewport has changed dimensions. 
		/// </summary>
		/// <remarks>
		///		This is used by pixel-based GuiControls to work out if they need to reclaculate their sizes.
		///	</remarks>																				  
		public bool HasViewportChanged
		{
			get
			{
				return _viewportDimensionsChanged;
			}
		}

		#endregion HasViewportChanged Property

		#region ViewportHeight Property

		private int _lastViewportHeight;
		/// <summary>
		///		Gets the height of the destination viewport in pixels.
		/// </summary>
		public int ViewportHeight
		{
			get
			{
				return _lastViewportHeight;
			}
		}

		#endregion ViewportHeight Property

		#region ViewportWidth Property

		private int _lastViewportWidth;
		/// <summary>
		///		Gets the width of the destination viewport in pixels.
		/// </summary>
		public int ViewportWidth
		{
			get
			{
				return _lastViewportWidth;
			}
		}

		#endregion ViewportWidth Property

		#region ViewportAspectRation Property

		public Real ViewportAspectRatio
		{
			get
			{
				return (Real)_lastViewportHeight / (Real)_lastViewportWidth;
			}
		}

		#endregion ViewportAspectRation Property

		#endregion Fields and Properties

		#region Construction and Destruction

		private OverlayManager()
		{
			// Scripting is supported by this manager
			ScriptPatterns.Add( "*.overlay" );
			ResourceGroupManager.Instance.RegisterScriptLoader( this );
		}

		#endregion Construction and Destruction

		#region Methods

		#region Overlay Management

		/// <summary>
		///		Creates and return a new overlay.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Overlay Create( string name )
		{
			if ( _overlays.ContainsKey( name ) )
			{
				throw new Exception( "Overlay with the name '" + name + "' already exists." );
			}

			Overlay overlay = new Overlay( name );
			if ( overlay == null )
			{
				throw new Exception( "Overlay '" + name + "' could not be created." );
			}

			_overlays.Add( name, overlay );
			return overlay;
		}

		/// <summary>
		/// Retrieve an Overlay by name
		/// </summary>
		/// <param name="name">Name of Overlay to retrieve</param>
		/// <returns>The overlay or null if not found.</returns>
		public Overlay GetByName( string name )
		{
			return _overlays.ContainsKey( name ) ? _overlays[ name ] : null;
		}

		#region Destroy*

		/// <summary>
		/// Destroys an existing overlay by name
		/// </summary>
		/// <param name="name"></param>
		public void Destroy( string name )
		{
			if ( !_overlays.ContainsKey( name ) )
			{
				LogManager.Instance.Write( "No overlay with the name '" + name + "' found to destroy." );
				return;
			}

			_overlays[ name ].Dispose();
			_overlays.Remove( name );
		}

		/// <summary>
		/// Destroys an existing overlay
		/// </summary>
		/// <param name="overlay"></param>
		public void Destroy( Overlay overlay )
		{
			if ( !_overlays.ContainsValue( overlay ) )
			{
				LogManager.Instance.Write( "Overlay '" + overlay.Name + "' not found to destroy." );
				overlay.Dispose();
				overlay = null;
				return;
			}

			_overlays.Remove( overlay.Name );
			_overlays[ overlay.Name ].Dispose();
		}

		/// <summary>
		/// Destroys all existing overlays
		/// </summary>
		public void DestroyAll()
		{
			foreach ( KeyValuePair<string, Overlay> entry in _overlays )
			{
				entry.Value.Dispose();
			}
			_overlays.Clear();
		}

		#endregion Destroy*

		#endregion Overlay Management

		#region OverlayElement Management

		#region Create*

		/// <summary>
		/// Creates an overlay element
		/// </summary>
		/// <param name="type">type to create</param>
		/// <param name="name">name of the element</param>
		/// <returns></returns>
		public OverlayElement CreateOverlayElement( string type, string name )
		{
			return CreateOverlayElement( type, name, false );
		}

		/// <summary>
		/// Creates an overlay element
		/// </summary>
		/// <param name="type">type to create</param>
		/// <param name="name">name of the element</param>
		/// <param name="isTemplate">create a template?</param>
		/// <returns></returns>
		public OverlayElement CreateOverlayElement( string type, string name, bool isTemplate )
		{
			Dictionary<string, OverlayElement> elements = isTemplate ? _elementTemplates : _elementInstances;

			if ( elements.ContainsKey( name ) )
			{
				throw new Exception( "An OverlayElement with the name '" + name + "' already exists." );
			}

			OverlayElement element = CreateOverlayElementFromFactory( type, name );
			elements.Add( name, element );
			return element;
		}

		/// <summary>
		/// Creates an overlay element from a factory
		/// </summary>
		/// <param name="type">type to create</param>
		/// <param name="name">name of the element</param>
		/// <returns></returns>
		public OverlayElement CreateOverlayElementFromFactory( string type, string name )
		{
			// Look up factory
			if ( !_elementFactories.ContainsKey( type ) )
			{
				throw new Exception( "Cannot locate factory for element type " + type );
			}

			// create
			return _elementFactories[type].Create( name );
		}

		/// <summary>
		/// Creates an overlay element from a template
		/// </summary>
		/// <param name="template">template to clone</param>
		/// <param name="type">type to create</param>
		/// <param name="name">name of the new element</param>
		/// <returns></returns>
		public OverlayElement CreateOverlayElementFromTemplate( string template, string type, string name )
		{
			return CreateOverlayElementFromTemplate( template, type, name, false );
		}

		/// <summary>
		/// Creates an overlay element from a template
		/// </summary>
		/// <param name="type">type to create</param>
		/// <param name="name">name of the new element</param>
		/// <param name="isTemplate">create a template?</param>
		/// <returns></returns>
		public OverlayElement CreateOverlayElementFromTemplate( string template, string type, string name, bool isTemplate )
		{
			OverlayElement element = null;

			if ( template == String.Empty )
			{
				return CreateOverlayElement( type, name, isTemplate );
			}
			else
			{
				// no template
				OverlayElement templateElement = GetOverlayElement( template, true );

				string typeToCreate = type == string.Empty ? templateElement.GetType().Name : type;

				element = CreateOverlayElement( typeToCreate, name, isTemplate );

				( (OverlayElementContainer)element ).CopyFromTemplate( element );

			}

			return element;
		}

		/// <summary>
		/// Clones an overlay element from a template
		/// </summary>
		/// <param name="template">template to clone</param>
		/// <param name="name">name of the new element</param>
		/// <returns></returns>
		public OverlayElement CloneOverlayElementFromTemplate( string template, string name )
		{
			OverlayElement element = GetOverlayElement( template, true );
			return element.Clone( name );
		}

		#endregion Create*

		#region GetOverlayElement

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public OverlayElement GetOverlayElement( string name )
		{
			return GetOverlayElement( name, false );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="isTemplate"></param>
		/// <returns></returns>
		public OverlayElement GetOverlayElement( string name, bool isTemplate )
		{
			Dictionary<string, OverlayElement> elements = isTemplate ? _elementTemplates : _elementInstances;
			if ( !elements.ContainsKey( name ) )
			{
				throw new Exception( "OverlayElement with the name '" + name + "' not found." );
			}

			return elements[ name ];
		}

		#endregion GetOverlayElement

		#region Destroy*OverlayElement

		/// <summary>
		/// Destroys the specified OverlayElement
		/// </summary>
		/// <param name="name"></param>
		public void DestroyOverlayElement( string name )
		{
			DestroyOverlayElement( name, false );
		}

		/// <summary>
		/// Destroys the specified OverlayElement
		/// </summary>
		/// <param name="name"></param>
		/// <param name="isTemplate"></param>
		public void DestroyOverlayElement( string name, bool isTemplate )
		{
			Dictionary<string, OverlayElement> elements = isTemplate ? _elementTemplates : _elementInstances;
			if ( !elements.ContainsKey( name ) )
			{
				throw new Exception( "OverlayElement with the name '" + name + "' not found to destroy." );
			}

			elements.Remove( name );
		}

		/// <summary>
		/// Destroys the supplied OvelayElement
		/// </summary>
		/// <param name="element"></param>
		public void DestroyOverlayElement( OverlayElement element )
		{
			DestroyOverlayElement( element, false );
		}

		/// <summary>
		/// Destroys the supplied OvelayElement
		/// </summary>
		/// <param name="element"></param>
		/// <param name="isTemplate"></param>
		public void DestroyOverlayElement( OverlayElement element, bool isTemplate )
		{
			Dictionary<string, OverlayElement> elements = isTemplate ? _elementTemplates : _elementInstances;
			if ( !elements.ContainsValue( element ) )
			{
				throw new Exception( "OverlayElement with the name '" + element.Name + "' not found to destroy." );
			}

			elements.Remove( element.Name );
		}

		/// <summary>
		/// destroys all OverlayElements
		/// </summary>
		public void DestroyAllOverlayElements()
		{
			DestroyAllOverlayElements( false );
		}

		/// <summary>
		/// destroys all OverlayElements
		/// </summary>
		public void DestroyAllOverlayElements( bool isTemplate )
		{
			( isTemplate ? _elementTemplates : _elementInstances ).Clear();
		}

		#endregion Destroy*OverlayElement

		public void AddOverlayElementFactory( IOverlayElementFactory factory )
		{
			if ( !_elementFactories.ContainsKey( factory.Type ) )
			{
				// Add
				_elementFactories.Add( factory.Type, factory );
			}
			else
			{
				// Replace
				_elementFactories[ factory.Type ] = factory;
			}
		}

		#endregion OverlayElement Management

		/// <summary>
		///		Internal method for queueing the visible overlays for rendering.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="queue"></param>
		/// <param name="viewport"></param>
		internal void QueueOverlaysForRendering( Camera camera, RenderQueue queue, Viewport viewport )
		{
			// Flag for update pixel-based OverlayElements if viewport has changed dimensions
			if ( _lastViewportWidth != viewport.ActualWidth ||
				_lastViewportHeight != viewport.ActualHeight )
			{

				_viewportDimensionsChanged = true;
				_lastViewportWidth = viewport.ActualWidth;
				_lastViewportHeight = viewport.ActualHeight;
			}
			else
			{
				_viewportDimensionsChanged = false;
			}

			foreach ( Overlay overlay in _overlays.Values )
			{
				overlay.FindVisibleObjects( camera, queue );
			}
		}

		#region Script Parsing
/*
		/// <summary>
		///    Load a specific overlay file by name.
		/// </summary>
		/// <remarks>
		///    This is required from allowing .overlay scripts to include other overlay files.  It
		///    is not guaranteed what order the files will be loaded in, so this can be used to ensure
		///    depencies in a script are loaded prior to the script itself being loaded.
		/// </remarks>
		/// <param name="fileName"></param>
		public void LoadAndParseOverlayFile( string fileName )
		{
			if ( _loadedScripts.Contains( fileName ) )
			{
				LogManager.Instance.Write( "Skipping load of overlay include: {0}, as it is already loaded.", fileName );
				return;
			}

			// file has not been loaded, so load it now

			// look in local resource data
			Stream data = this.FindResourceData( fileName );

			if ( data == null )
			{
				// wasnt found, so look in common resource data.
				data = ResourceManager.FindCommonResourceData( fileName );

				if ( data == null )
				{
					throw new Exception( string.Format( "Unable to find overlay file '{0}'", fileName ) );
				}
			}

			// parse the overlay script
			ParseOverlayScript( data );
		}

		/// <summary>
		///    Parses all overlay files in resource folders and archives.
		/// </summary>
		public void ParseAllSources()
		{
			string extension = ".overlay";

			// search archives
			for ( int i = 0; i < archives.Count; i++ )
			{
				Archive archive = (Archive)archives[ i ];
				string[] files = archive.GetFileNamesLike( "", extension );

				for ( int j = 0; j < files.Length; j++ )
				{
					Stream data = archive.ReadFile( files[ j ] );

					// parse the materials
					ParseOverlayScript( data );
				}
			}

			// search common archives
			for ( int i = 0; i < commonArchives.Count; i++ )
			{
				Archive archive = (Archive)commonArchives[ i ];
				string[] files = archive.GetFileNamesLike( "", extension );

				for ( int j = 0; j < files.Length; j++ )
				{

					Stream data = archive.ReadFile( files[ j ] );

					// parse the materials
					ParseOverlayScript( data );
				}
			}
		}
*/
		/// <summary>
		///    Parses an attribute belonging to an Overlay.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="overlay"></param>
		private void ParseAttrib( string line, Overlay overlay )
		{
			string[] parms = line.Split( ' ' );

			if ( parms[ 0 ].ToLower() == "zorder" )
			{
				overlay.ZOrder = int.Parse( parms[ 1 ] );
			}
			else
			{
				ParseHelper.LogParserError( parms[ 0 ], overlay.Name, "Invalid overlay attribute." );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="script"></param>
		/// <param name="line"></param>
		/// <param name="overlay"></param>
		/// <param name="isTemplate"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		private bool ParseChildren( TextReader script, string line, Overlay overlay, bool isTemplate, OverlayElementContainer parent )
		{
			bool ret = false;
			int skipParam = 0;

			string[] parms = line.Split( ' ', '(', ')' );

			// split on lines with a ) will have an extra blank array element, so lets get rid of it
			if ( parms[ parms.Length - 1 ].Length == 0 )
			{
				string[] tmp = new string[ parms.Length - 1 ];
				Array.Copy( parms, 0, tmp, 0, parms.Length - 1 );
				parms = tmp;
			}

			if ( isTemplate )
			{
				// the first param = 'template' on a new child element
				if ( parms[ 0 ] == "template" )
				{
					skipParam++;
				}
			}

			// top level component cannot be an element, it must be a container unless it is a template
			if ( parms[ 0 + skipParam ] == "container" || ( parms[ 0 + skipParam ] == "element" && ( isTemplate || parent != null ) ) )
			{
				string templateName = "";
				ret = true;

				// nested container/element
				if ( parms.Length > 3 + skipParam )
				{
					if ( parms.Length != 5 + skipParam )
					{
						LogManager.Instance.Write( "Bad element/container line: {0} in {1} - {2}, expecting ':' templateName", line, parent.GetType().Name, parent.Name );
						ParseHelper.SkipToNextCloseBrace( script );
						return ret;
					}
					if ( parms[ 3 + skipParam ] != ":" )
					{
						LogManager.Instance.Write( "Bad element/container line: {0} in {1} - {2}, expecting ':' for element inheritance.", line, parent.GetType().Name, parent.Name );
						ParseHelper.SkipToNextCloseBrace( script );
						return ret;
					}

					// get the template name
					templateName = parms[ 4 + skipParam ];
				}
				else if ( parms.Length != 3 + skipParam )
				{
					LogManager.Instance.Write( "Bad element/container line: {0} in {1} - {2}, expecting 'element type(name)'.", line, parent.GetType().Name, parent.Name );
					ParseHelper.SkipToNextCloseBrace( script );
					return ret;
				}

				ParseHelper.SkipToNextOpenBrace( script );
				bool isContainer = ( parms[ 0 + skipParam ] == "container" );
				ParseNewElement( script, parms[ 1 + skipParam ], parms[ 2 + skipParam ], isContainer, overlay, isTemplate, templateName, parent );
			}

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		/// <param name="overlay"></param>
		/// <param name="element"></param>
		private void ParseElementAttrib( string line, Overlay overlay, OverlayElement element )
		{
			string[] parms = line.Split( ' ' );

			// get a string containing only the params
			string paramLine = line.Substring( line.IndexOf( ' ', 0 ) + 1 );

			// set the param, and hopefully it exists
			if ( !element.SetParam( parms[ 0 ].ToLower(), paramLine ) )
			{
				LogManager.Instance.Write( "Bad element attribute line: {0} for element '{1}'", line, element.Name );
			}
		}

		/// <summary>
		///    Overloaded.  Calls overload with default of empty template name and null for the parent container.
		/// </summary>
		/// <param name="script"></param>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="isContainer"></param>
		/// <param name="overlay"></param>
		/// <param name="isTemplate"></param>
		private void ParseNewElement( TextReader script, string type, string name, bool isContainer, Overlay overlay, bool isTemplate )
		{
			ParseNewElement( script, type, name, isContainer, overlay, isTemplate, "", null );
		}

		/// <summary>
		///    Parses a new element
		/// </summary>
		/// <param name="script"></param>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="isContainer"></param>
		/// <param name="overlay"></param>
		/// <param name="isTemplate"></param>
		/// <param name="templateName"></param>
		/// <param name="parent"></param>
		private void ParseNewElement( TextReader script, string type, string name, bool isContainer, Overlay overlay, bool isTemplate,
			string templateName, OverlayElementContainer parent )
		{

			string line;
			OverlayElement element = OverlayElementManager.Instance.CreateElementFromTemplate( templateName, type, name, isTemplate );

			if ( parent != null )
			{
				// add this element to the parent container
				parent.AddChild( element );
			}
			else if ( overlay != null )
			{
				overlay.AddElement( (OverlayElementContainer)element );
			}

			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				// inore blank lines and comments
				if ( line.Length > 0 && !line.StartsWith( "//" ) )
				{
					if ( line == "}" )
					{
						// finished element
						break;
					}
					else
					{
						OverlayElementContainer container = null;
						if ( element is OverlayElementContainer )
						{
							container = (OverlayElementContainer)element;
						}
						//if ( isContainer && ParseChildren( script, line, overlay, isTemplate, (OverlayElementContainer)element ) )
						if ( isContainer && ParseChildren( script, line, overlay, isTemplate, container ) )
						{
							// nested children, so don't reparse it
						}
						else
						{
							// element attribute
							ParseElementAttrib( line, overlay, element );
						}
					}
				}
			}
		}

		/// <summary>
		///    Parses a 3D mesh which will be used in the overlay.
		/// </summary>
		/// <param name="script"></param>
		/// <param name="meshName"></param>
		/// <param name="entityName"></param>
		/// <param name="overlay"></param>
		public void ParseNewMesh( TextReader script, string meshName, string entityName, Overlay overlay )
		{
		}

		/// <summary>
		///    Parses an individual .overlay file.
		/// </summary>
		/// <param name="data"></param>
		public void ParseOverlayScript( Stream data )
		{
		}

		#endregion Script Parsing

		#endregion Methods

		#region Singleton<OverlayManager> Implementation

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !isDisposed )
			{
				if ( disposeManagedResources )
				{
					DestroyAllOverlayElements( false );
					DestroyAllOverlayElements( true );
					DestroyAll();

					// Unregister with resource group manager
					ResourceGroupManager.Instance.UnregisterScriptLoader( this );
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			isDisposed = true;

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Singleton<OverlayManager> Implementation

		#region IScriptLoader Members

		private List<string> _scriptPatterns = new List<string>();
		public List<string> ScriptPatterns
		{
			get
			{
				return _scriptPatterns;
			}
		}

		public void ParseScript( Stream stream, string groupName, string fileName )
		{
			string line = "";
			Overlay overlay = null;
			bool skipLine;

			if ( _loadedScripts.Contains( fileName ) )
			{
				LogManager.Instance.Write( "Skipping load of overlay include: {0}, as it is already loaded.", fileName );
				return;
			}

			// parse the overlay script
			StreamReader script = new StreamReader( stream, System.Text.Encoding.ASCII );

			// keep reading the file until we hit the end
			while ( ( line = ParseHelper.ReadLine( script ) ) != null )
			{
				bool isTemplate = false;
				skipLine = false;

				// ignore comments and blank lines
				if ( line.Length > 0 && !line.StartsWith( "//" ) )
				{
					// does another overlay have to be included
					if ( line.StartsWith( "#include" ) )
					{

						string[] parms = line.Split( ' ', '(', ')', '<', '>' );
						// split on lines with a ) will have an extra blank array element, so lets get rid of it
						if ( parms[ parms.Length - 1 ].Length == 0 )
						{
							string[] tmp = new string[ parms.Length - 1 ];
							Array.Copy( parms, 0, tmp, 0, parms.Length - 1 );
							parms = tmp;
						}
						string includeFile = parms[ 2 ];

						Stream data = ResourceGroupManager.Instance.OpenResource( includeFile );
						ParseScript( data, groupName, includeFile );
						data.Close();

						continue;
					}

					if ( overlay == null )
					{
						// no current overlay
						// check to see if there is a template
						if ( line.StartsWith( "template" ) )
						{
							isTemplate = true;
						}
						else
						{
							// the line in this case should be the name of the overlay
							overlay = Create( line );
							//this is just telling the file name of the overlay
							overlay.Origin = fileName;
							// cause the next line (open brace) to be skipped
							ParseHelper.SkipToNextOpenBrace( script );
							skipLine = true;
						}
					}

					if ( ( overlay != null && !skipLine ) || isTemplate )
					{
						// already in overlay
						string[] parms = line.Split( ' ', '(', ')' );

						// split on lines with a ) will have an extra blank array element, so lets get rid of it
						if ( parms[ parms.Length - 1 ].Length == 0 )
						{
							string[] tmp = new string[ parms.Length - 1 ];
							Array.Copy( parms, 0, tmp, 0, parms.Length - 1 );
							parms = tmp;
						}

						if ( line == "}" )
						{
							// finished overlay
							overlay = null;
							isTemplate = false;
						}
						else if ( ParseChildren( script, line, overlay, isTemplate, null ) )
						{
						}
						else if ( parms[ 0 ] == "entity" ) // Multiverse Extension?
						{
							// 3D element
							if ( parms.Length != 3 )
							{
								LogManager.Instance.Write( string.Format( "Bad entity line: {0} in {1}, expected format - entity meshName(entityName)'", line, overlay.Name ) );
							} // if parms...
							else
							{
								ParseHelper.SkipToNextOpenBrace( script );
								ParseNewMesh( script, parms[ 1 ], parms[ 2 ], overlay );
							}
						}
						else
						{
							// must be an attribute
							if ( !isTemplate )
							{
								ParseAttrib( line, overlay );
							}
						}
					}
				}
			}
		}

		public float LoadingOrder
		{
			get
			{
				// Load Late
				return 1100.0f;
			}
		}

		#endregion

	}
}