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

using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    /// <summary>
    ///		Any class that wants to entend the functionality of the engine can implement this
    ///		interface.  Classes implementing this interface will automatically be loaded and
    ///		started by the engine during the initialization phase.  Examples of plugins would be
    ///		RenderSystems, SceneManagers, etc, which can register themself using the 
    ///		singleton instance of the Engine class.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Initializes the plugin
        /// </summary>
        void Start();

        /// <summary>
        /// Shuts down the plugin (really?)
        /// </summary>
        void Stop();

        /// <summary>
        /// Checks whether this plugin is already started
        /// </summary>
        bool IsStarted { get; }
    }
}
