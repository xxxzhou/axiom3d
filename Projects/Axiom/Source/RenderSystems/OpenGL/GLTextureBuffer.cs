#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Graphics;
using Axiom.Media;
using Tao.OpenGl;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	class GLTextureBuffer : GLHardwarePixelBuffer
	{
		#region Fields and Properties

		// In case this is a texture level
		private int _target;
		private int _faceTarget; // same as _target in case of Gl.GL_TEXTURE_xD, but cubemap face for cubemaps
		private int _textureId;
		private int _face;
		private int _level;
		private bool _softwareMipmap;		// Use GLU for mip mapping

		private List<RenderTexture> _sliceTRT = new List<RenderTexture>();

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLTextureBuffer( string baseName, int target, int id, int face, int level, BufferUsage usage, bool softwareMipmap )
			: base( 0, 0, 0, PixelFormat.Unknown, usage )
		{
			int value;

			_target = target;
			_textureId = id;
			_face = face;
			_level = level;
			_softwareMipmap = softwareMipmap;

			Gl.glBindTexture( _target, _textureId );

			// Get face identifier
			_faceTarget = _target;
			if ( _target == Gl.GL_TEXTURE_CUBE_MAP )
				_faceTarget = Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + _face;

			// Get width
			Gl.glGetTexLevelParameteriv( _faceTarget, _level, Gl.GL_TEXTURE_WIDTH, out value );
			Width = value;

			// Get height
			if ( _target == Gl.GL_TEXTURE_1D )
				value = 1;	// Height always 1 for 1D textures
			else
				Gl.glGetTexLevelParameteriv( _faceTarget, _level, Gl.GL_TEXTURE_HEIGHT, out value );
			Height = value;

			// Get depth
			if ( _target != Gl.GL_TEXTURE_3D )
				value = 1; // Depth always 1 for non-3D textures
			else
				Gl.glGetTexLevelParameteriv( _faceTarget, _level, Gl.GL_TEXTURE_DEPTH, out value );
			Depth = value;

			// Get format
			Gl.glGetTexLevelParameteriv( _faceTarget, _level, Gl.GL_TEXTURE_INTERNAL_FORMAT, out value );
			GLFormat = value;
			Format = GLPixelUtil.GetClosestPixelFormat( value );

			// Default
			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			// Set up pixel box
			this.buffer = new PixelBox( Width, Height, Depth, Format );

			if ( Width == 0 || Height == 0 || Depth == 0 )
				/// We are invalid, do not allocate a buffer
				return;

			// Allocate buffer
			//if(mUsage & HBU_STATIC)
			//	allocateBuffer();

			// Is this a render target?
			if ( ( (TextureUsage)Usage & TextureUsage.RenderTarget ) == 0 )
			{
				// Create render target for each slice
				_sliceTRT.Capacity = Depth;
				for ( int zoffset = 0; zoffset < Depth; ++zoffset )
				{
					String name;
					name = String.Format( "{0}/{1}/{2}/{3}", baseName, face, _level, zoffset );

					GLSurfaceDesc renderTarget;
					renderTarget.Buffer = this;
					renderTarget.ZOffset = zoffset;
					RenderTexture trt = GLRTTManager.Instance.CreateRenderTexture( name, renderTarget );
					_sliceTRT.Add( trt );
					Root.Instance.RenderSystem.AttachRenderTarget( _sliceTRT[ zoffset ] );
				}
			}
		}

		#endregion Construction and Destruction

		#region Methods
		#endregion Methods

		#region GLHardwarePixelBuffer Implementation

		public override void BindToFramebuffer( int attachment, int zOffset )
		{
			base.BindToFramebuffer( attachment, zOffset );
		}

		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			base.BlitFromMemory( src, dstBox );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			base.dispose( disposeManagedResources );
		}

		public override RenderTexture GetRenderTarget()
		{
			return base.GetRenderTarget();
		}

		protected override void download( PixelBox box )
		{
			base.download( box );
		}

		protected override void upload( PixelBox box )
		{
			Gl.glBindTexture( _target, _textureId );
			if ( PixelUtil.IsCompressed( box.Format ) )
			{
				if ( box.Format != Format || !box.Consecutive )
					throw new ArgumentException( "Compressed images must be consecutive, in the source format" );

				int format = GLPixelUtil.GetClosestGLInternalFormat( Format );
				// Data must be consecutive and at beginning of buffer as PixelStorei not allowed
				// for compressed formats
				switch ( _target )
				{
					case Gl.GL_TEXTURE_1D:
						Gl.glCompressedTexSubImage1DARB( Gl.GL_TEXTURE_1D, _level,
							box.Left,
							box.Width,
							format, box.ConsecutiveSize,
							box.Data );
						break;
					case Gl.GL_TEXTURE_2D:
					case Gl.GL_TEXTURE_CUBE_MAP:
						Gl.glCompressedTexSubImage2DARB( _faceTarget, _level,
							box.Left, box.Top,
							box.Width, box.Height,
							format, box.ConsecutiveSize,
							box.Data );
						break;
					case Gl.GL_TEXTURE_3D:
						Gl.glCompressedTexSubImage3DARB( Gl.GL_TEXTURE_3D, _level,
							box.Left, box.Top, box.Front,
							box.Width, box.Height, box.Depth,
							format, box.ConsecutiveSize,
							box.Data );
						break;
				}

			}
			else if ( _softwareMipmap )
			{
				int internalFormat;
				Gl.glGetTexLevelParameteriv( _target, _level, Gl.GL_TEXTURE_INTERNAL_FORMAT, out internalFormat );
				if ( box.Width != box.RowPitch )
					Gl.glPixelStorei( Gl.GL_UNPACK_ROW_LENGTH, box.RowPitch );
				if ( box.Height * box.Width != box.SlicePitch )
					Gl.glPixelStorei( Gl.GL_UNPACK_IMAGE_HEIGHT, ( box.SlicePitch / box.Width ) );
				Gl.glPixelStorei( Gl.GL_UNPACK_ALIGNMENT, 1 );

				switch ( _target )
				{
					case Gl.GL_TEXTURE_1D:
						Glu.gluBuild1DMipmaps(
							Gl.GL_TEXTURE_1D, internalFormat,
							box.Width,
							GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ),
							box.Data );
						break;
					case Gl.GL_TEXTURE_2D:
					case Gl.GL_TEXTURE_CUBE_MAP:
						Glu.gluBuild2DMipmaps(
							_faceTarget,
							internalFormat, box.Width, box.Height,
							GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ),
							box.Data );
						break;
					case Gl.GL_TEXTURE_3D:
						/* Requires GLU 1.3 which is harder to come by than cards doing hardware mipmapping
							Most 3D textures don't need mipmaps?
						Gl.gluBuild3DMipmaps(
							Gl.GL_TEXTURE_3D, internalFormat, 
							box.getWidth(), box.getHeight(), box.getDepth(),
							GLPixelUtil.getGLOriginFormat(box.format), GLPixelUtil.getGLOriginDataType(box.format),
							box.box);
						*/
						Gl.glTexImage3D(
							Gl.GL_TEXTURE_3D, 0, internalFormat,
							box.Width, box.Height, box.Depth, 0,
							GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ),
							box.Data );
						break;
				}
			}
			else
			{
				if ( box.Width != box.RowPitch )
					Gl.glPixelStorei( Gl.GL_UNPACK_ROW_LENGTH, box.RowPitch );
				if ( box.Height * box.Width != box.SlicePitch )
					Gl.glPixelStorei( Gl.GL_UNPACK_IMAGE_HEIGHT, ( box.SlicePitch / box.Width ) );
				if ( ( ( box.Width * PixelUtil.GetNumElemBytes( box.Format ) ) & 3 ) != 0 )
				{
					// Standard alignment of 4 is not right
					Gl.glPixelStorei( Gl.GL_UNPACK_ALIGNMENT, 1 );
				}
				switch ( _target )
				{
					case Gl.GL_TEXTURE_1D:
						Gl.glTexSubImage1D( Gl.GL_TEXTURE_1D, _level,
							box.Left,
							box.Width,
							GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ),
							box.Data );
						break;
					case Gl.GL_TEXTURE_2D:
					case Gl.GL_TEXTURE_CUBE_MAP:
						Gl.glTexSubImage2D( _faceTarget, _level,
							box.Left, box.Top,
							box.Width, box.Height,
							GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ),
							box.Data );
						break;
					case Gl.GL_TEXTURE_3D:
						Gl.glTexSubImage3D(
							Gl.GL_TEXTURE_3D, _level,
							box.Left, box.Top, box.Front,
							box.Width, box.Height, box.Depth,
							GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ),
							box.Data );
						break;
				}
			}
			// Restore defaults
			Gl.glPixelStorei( Gl.GL_UNPACK_ROW_LENGTH, 0 );
			Gl.glPixelStorei( Gl.GL_UNPACK_IMAGE_HEIGHT, 0 );
			Gl.glPixelStorei( Gl.GL_UNPACK_ALIGNMENT, 4 );
		}

		public override void ClearSliceRTT( int zOffset )
		{
			_sliceTRT[ zOffset ] = null;
		}

		#endregion GLHardwarePixelBuffer Implementation
	}
}