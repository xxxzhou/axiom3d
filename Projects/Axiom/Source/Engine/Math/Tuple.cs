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
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Math
{
    /// <summary>
    ///	Represents two related values
    /// </summary>
    public class Tuple< A, B >
    {
        public Tuple( A first, B second )
        {
            this.first = first;
            this.second = second;
        }
        /// <summary></summary>
        public A first;
        /// <summary></summary>
        public B second;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <typeparam name="C"></typeparam>
    public class Tuple<A, B, C>
    {
        /// <summary></summary>
        public A first;
        /// <summary></summary>
        public B second;
        /// <summary></summary>
        public C thrid;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <typeparam name="C"></typeparam>
    /// <typeparam name="D"></typeparam>
    public class Tuple<A, B, C, D>
    {
        /// <summary></summary>
        public A first;
        /// <summary></summary>
        public B second;
        /// <summary></summary>
        public C thrid;
        /// <summary></summary>
        public D fourth;
    }
}