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

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;

using Axiom;

#endregion Namespace Declarations
			
namespace Axiom.Platforms.SDL
{
    /// <summary>
    ///		Platform management specialization for Microsoft Windows (r) platform.
    /// </summary>
    // TODO Disposal of object create here.
    [PluginMetadata(IsSingleton = true, Name = "PlatformManager",
    Description = "Axiom SDL Platform")]
    public class SdlPlatformManager : IPlatformManager, ISingletonPlugin
    {
        #region Fields

        /// <summary>
        ///		Reference to the current input reader.
        /// </summary>
        private InputReader inputReader;
        /// <summary>
        ///		Reference to the current active timer.
        /// </summary>
        private ITimer timer;

        #endregion Fields

        #region IPlatformManager Members

        public void Dispose()
        {
        }

        /// <summary>
        ///		Creates an InputReader implemented using Microsoft DirectInput (tm).
        /// </summary>
        /// <returns></returns>
        public InputReader CreateInputReader()
        {
            inputReader = new SdlInputReader();
            return inputReader;
        }

        /// <summary>
        ///		Creates a high precision Windows timer.
        /// </summary>
        /// <returns></returns>
        public ITimer CreateTimer()
        {
            timer = new SdlTimer();
            return timer;
        }

        /// <summary>
        ///		Implements the Microsoft Windows (r) message pump for allowing the OS to process
        ///		pending events.
        /// </summary>
        public void DoEvents()
        {
            // not required
        }

        #endregion

        public object GetSubsystemImplementation()
        {
            return this;
        }

        public void Start()
        {
            LogManager.Instance.Write("SDL Platform Manager started");
            _isStarted = true;
        }

        public void Stop()
        {
            LogManager.Instance.Write("SDL Platform Manager stopped");
        }

        private bool _isStarted = false;

        public bool IsStarted
        {
            get { return _isStarted; }
        }

    }
}
