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

using Axiom.Animating;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Graphics.Collections;

#endregion

namespace Axiom.Core
{
    /// <summary>
    ///		Abstract class defining a movable object in a scene.
    /// </summary>
    /// <remarks>
    ///		Instances of this class are discrete, relatively small, movable objects
    ///		which are attached to SceneNode objects to define their position.						  
    /// </remarks>
    public abstract class MovableObject : ShadowCaster, IAnimableObject
    {
        #region Fields

        /// <summary>
        ///		Does this object cast shadows?
        /// </summary>
        protected bool castShadows;

        protected ShadowRenderableList dummyList = new ShadowRenderableList();
		
		protected static long nextUnnamedNodeExtNum = 1;
		
        /// <summary>
        ///    Is this object visible?
        /// </summary>
        protected bool isVisible;

        /// <summary>
        ///    Name of this object.
        /// </summary>
        protected string name;

        /// <summary>
        ///		Flag which indicates whether this objects parent is a <see cref="TagPoint"/>.
        /// </summary>
        protected bool parentIsTagPoint;

        /// <summary>
        ///    Node that this node is attached to.
        /// </summary>
        protected Node parentNode;

        /// <summary>
        ///    The render queue to use when rendering this object.
        /// </summary>
        protected RenderQueueGroupID renderQueueID;

        /// <summary>
        ///    Flags whether the RenderQueue's default should be used.
        /// </summary>
        protected bool renderQueueIDSet = false;

        /// <summary>
        ///    A link back to a GameObject (or subclass thereof) that may be associated with this SceneObject.
        /// </summary>
        protected object userData;

        /// <summary>
        ///    Cached world bounding box of this object.
        /// </summary>
        protected AxisAlignedBox worldAABB;

        /// <summary>
        ///    Cached world bounding spehere.
        /// </summary>
        protected Sphere worldBoundingSphere = new Sphere();

        /// <summary>
        ///		World space AABB of this object's dark cap.
        /// </summary>
        protected AxisAlignedBox worldDarkCapBounds = AxisAlignedBox.Null;

        #region Fields for MovableObjectFactory

        private MovableObjectFactory _creator;
        private SceneManager _manager;
        private string movableType;

        #endregion Fields for MovableObjectFactory

        #endregion Fields

        #region Constructors

        protected MovableObject(string name)
            : this()
        {
            this.name = name;
        }

        /// <summary>
        ///		Default constructor.
        /// </summary>
        protected MovableObject()
            : base()
        {
            this.isVisible = true;
            // set default RenderQueueGroupID for this movable object
            this.renderQueueID = RenderQueueGroupID.Main;
            this.queryFlags = unchecked( 0xffffffff );
            this.worldAABB = AxisAlignedBox.Null;
            this.castShadows = true;
			this.name = "Unnamed_" + nextUnnamedNodeExtNum++;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
        /// </summary>
        public abstract AxisAlignedBox BoundingBox { get; }

        /// <summary>
        ///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
        /// </summary>
        public abstract float BoundingRadius { get; }

        /// <summary>
        ///     Get/Sets a link back to a GameObject (or subclass thereof, such as Entity) that may be associated with this SceneObject.
        /// </summary>
        public object UserData
        {
            get
            {
                return this.userData;
            }
            set
            {
                this.userData = value;
            }
        }

        /// <summary>
        ///		Gets the parent node that this object is attached to.
        /// </summary>
        public Node ParentNode
        {
            get
            {
                return this.parentNode;
            }
        }

        public SceneNode ParentSceneNode
        {
            get
            {
                if ( this.parentIsTagPoint )
                {
                    TagPoint tp = (TagPoint) this.parentNode;
                    return tp.ParentEntity.ParentSceneNode;
                }
                else
                {
                    return (SceneNode) this.parentNode;
                }
            }
        }

        /// <summary>
        ///		See if this object is attached to another node.
        /// </summary>
        [Obsolete("This property has been superceded by the IsInScene property")]
        public bool IsAttached
        {
            get
            {
                return ( this.parentNode != null );
            }
        }

        /// <summary>
        ///		States whether or not this object should be visible.
        /// </summary>
        public virtual bool IsVisible
        {
            get
            {
                return this.isVisible;
            }
            set
            {
                this.isVisible = value;
            }
        }

        /// <summary>
        ///		Name of this SceneObject.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                name = value;
            }
        }

        /// <summary>
        ///		Gets the full transformation of the parent SceneNode or TagPoint.
        /// </summary>
        public virtual Matrix4 ParentNodeFullTransform
        {
            get
            {
                if ( this.parentNode != null )
                {
                    // object is attached to a node, so return the nodes transform
                    return this.parentNode.FullTransform;
                }

                // fallback
                return Matrix4.Identity;
            }
        }

        /// <summary>
        /// Get the 'type flags' for this MovableObject.
        /// </summary>
        /// <remarks>
        /// A type flag identifies the type of the MovableObject as a bitpattern. 
        /// This is used for categorical inclusion / exclusion in SceneQuery
        /// objects. By default, this method returns all ones for objects not 
        /// created by a MovableObjectFactory (hence always including them); 
        /// otherwise it returns the value assigned to the MovableObjectFactory.
        /// Custom objects which don't use MovableObjectFactory will need to 
        /// override this if they want to be included in queries.
        /// </remarks>
        public virtual ulong TypeFlags
        {
            get
            {
                if ( this.Creator != null )
                {
                    return this.Creator.TypeFlag;
                }
                else
                {
                    return 0xFFFFFFFF;
                }
            }
        }

        /// <summary>
        ///    Allows showing the bounding box of an invidual SceneObject.
        /// </summary>
        /// <remarks>
        ///    This shows the bounding box of the SceneNode that the SceneObject is currently attached to.
        /// </remarks>
        public bool ShowBoundingBox
        {
            get
            {
                return ( (SceneNode) this.parentNode ).ShowBoundingBox;
            }
            set
            {
                ( (SceneNode) this.parentNode ).ShowBoundingBox = value;
            }
        }

        /// <summary>
        ///		Gets/Sets the render queue group this entity will be rendered through.
        /// </summary>
        /// <remarks>
        ///		Render queues are grouped to allow you to more tightly control the ordering
        ///		of rendered objects. If you do not call this method, all Entity objects default
        ///		to <see cref="RenderQueueGroupID.Main"/> which is fine for most objects. You may want to alter this
        ///		if you want this entity to always appear in front of other objects, e.g. for
        ///		a 3D menu system or such.
        /// </remarks>
        public RenderQueueGroupID RenderQueueGroup
        {
            get
            {
                return this.renderQueueID;
            }
            set
            {
                this.renderQueueID = value;
                this.renderQueueIDSet = true;
            }
        }

        /// <summary>
        /// Returns true if this object is attached to a SceneNode or TagPoint, 
        /// and this SceneNode / TagPoint is currently in an active part of the
        /// scene graph.
        /// </summary>
        public virtual bool IsInScene
        {
            get
            {
                if ( this.parentNode != null )
                {
                    if ( this.parentIsTagPoint )
                    {
                        TagPoint tp = (TagPoint)this.ParentNode;
                        return tp.ParentEntity.IsInScene;
                    }
                    else
                    {
                        SceneNode sn = (SceneNode)this.ParentNode;
                        return sn.IsInSceneGraph;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///    Retrieves the axis-aligned bounding box for this object in world coordinates.
        /// </summary>
        /// <returns></returns>
        public override AxisAlignedBox GetWorldBoundingBox( bool derive )
        {
            if ( derive )
            {
                this.worldAABB = this.BoundingBox;
                this.worldAABB.Transform( this.ParentNodeFullTransform );
            }

            return this.worldAABB;
        }

        /// <summary>
        ///    Overloaded method.  Calls the overload with a default of not deriving the transform.
        /// </summary>
        /// <returns></returns>
        public Sphere GetWorldBoundingSphere()
        {
            return this.GetWorldBoundingSphere( false );
        }

        /// <summary>
        ///    Retrieves the worldspace bounding sphere for this object.
        /// </summary>
        /// <param name="derive">Whether or not to derive from parent transforms.</param>
        /// <returns></returns>
        public virtual Sphere GetWorldBoundingSphere( bool derive )
        {
            if ( derive )
            {
                this.worldBoundingSphere.Radius = this.BoundingRadius;
                this.worldBoundingSphere.Center = this.parentNode.DerivedPosition;
            }

            return this.worldBoundingSphere;
        }

        #endregion Methods

        #region QueryFlags

        /// <summary>
        ///    Flags determining whether this object is included/excluded from scene queries.
        /// </summary>
        protected ulong queryFlags;

        /// <summary>
        /// default query flags for all future MovableObject instances.
        /// </summary>
        protected ulong defaultQueryFlags;

        /// <summary>
        /// Gets/Sets default query flags for all future MovableObject instances.
        /// </summary>
        public ulong DefaultQueryFlags
        {
            get 
            { 
                return this.defaultQueryFlags; 
            }
            set 
            { 
                this.defaultQueryFlags = value; 
            }
        }

        /// <summary>
        ///		Gets/Sets the query flags for this object.
        /// </summary>
        /// <remarks>
        ///		When performing a scene query, this object will be included or excluded according
        ///		to flags on the object and flags on the query. This is a bitwise value, so only when
        ///		a bit on these flags is set, will it be included in a query asking for that flag. The
        ///		meaning of the bits is application-specific.
        /// </remarks>
        public ulong QueryFlags
        {
            get
            {
                return this.queryFlags;
            }
            set
            {
                this.queryFlags = value;
            }
        }

        /// <summary>
        ///		Appends the specified flags to the current flags for this object.
        /// </summary>
        /// <param name="flags"></param>
        public void AddQueryFlags( ulong flags )
        {
            this.queryFlags |= flags;
        }

        /// <summary>
        ///		Removes the specified flags from the current flags for this object.
        /// </summary>
        /// <param name="flags"></param>
        public void RemoveQueryFlags( ulong flags )
        {
            this.queryFlags ^= flags;
        }

        #endregion QueryFlags

        #region VisibilityFlags

        /// <summary>
        /// Flags determining whether this object is visible (compared to SceneManager mask)
        /// </summary>
        protected ulong visibilityFlags;

        /// <summary>
        /// default visibility flags for all future MovableObject instances.
        /// </summary>
        protected static ulong defaultVisibilityFlags;

        /// <summary>
        /// Gets/Sets default visibility flags for all future MovableObject instances.
        /// </summary>
        public static ulong DefaultVisibilityFlags
        {
            get
            {
                return defaultVisibilityFlags;
            }
            set
            {
                defaultVisibilityFlags = value;
            }
        }

        /// <summary>
        ///	Gets/Sets the visibility flags for this object.
        /// </summary>
        /// <remarks>
        ///	As well as a simple true/false value for visibility (as seen in setVisible),
        ///	you can also set visiblity flags which when 'and'ed with the SceneManager's
        ///	visibility mask can also make an object invisible.
        /// </remarks>
        public ulong VisibilityFlags
        {
            get
            {
                return this.visibilityFlags;
            }
            set
            {
                this.visibilityFlags = value;
            }
        }

        /// <summary>
        ///		Appends the specified flags to the current flags for this object.
        /// </summary>
        /// <param name="flags"></param>
        public void AddVisibilityFlags( ulong flags )
        {
            this.visibilityFlags |= flags;
        }

        /// <summary>
        ///		Removes the specified flags from the current flags for this object.
        /// </summary>
        /// <param name="flags"></param>
        public void RemoveVisibilityFlags( ulong flags )
        {
            this.visibilityFlags ^= flags;
        }

        #endregion VisibilityFlags

        /// <summary>
        ///		Overridden.
        /// </summary>
        public override bool CastShadows
        {
            get
            {
                return this.castShadows;
            }
            set
            {
                this.castShadows = value;
            }
        }

        public override AxisAlignedBox GetLightCapBounds()
        {
            // same as original bounds
            return this.GetWorldBoundingBox();
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="light"></param>
        /// <param name="extrusionDistance"></param>
        /// <returns></returns>
        public override AxisAlignedBox GetDarkCapBounds( Light light, float extrusionDistance )
        {
            // Extrude own light cap bounds
            // need a clone to avoid modifying the original bounding box
            this.worldDarkCapBounds = (AxisAlignedBox) this.GetLightCapBounds().Clone();

            this.ExtrudeBounds( this.worldDarkCapBounds, light.GetAs4DVector(), extrusionDistance );

            return this.worldDarkCapBounds;
        }

        /// <summary>
        ///		Overridden.  Returns null by default.
        /// </summary>
        public override EdgeData GetEdgeList( int lodIndex )
        {
            return null;
        }

        public override IEnumerator GetShadowVolumeRenderableEnumerator( ShadowTechnique technique,
                                                                         Light light,
                                                                         HardwareIndexBuffer indexBuffer,
                                                                         bool extrudeVertices,
                                                                         float extrusionDistance,
                                                                         int flags )
        {
            return this.dummyList.GetEnumerator();
        }

        public override IEnumerator GetLastShadowVolumeRenderableEnumerator()
        {
            return this.dummyList.GetEnumerator();
        }

        /// <summary>
        ///		Get the distance to extrude for a point/spot light
        /// </summary>
        /// <param name="light"></param>
        /// <returns></returns>
        public override float GetPointExtrusionDistance( Light light )
        {
            if ( this.parentNode != null )
            {
                return this.GetExtrusionDistance( this.parentNode.DerivedPosition, light );
            }
            else
            {
                return 0;
            }
        }

        #region IAnimable methods

        /// <summary>
        ///     Part of the IAnimableObject interface.
        ///		The implementation of this property just returns null; descendents
        ///     are free to override this.
        /// </summary>
        public virtual string[] AnimableValueNames
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        ///     Part of the IAnimableObject interface.
        ///		Create an AnimableValue for the attribute with the given name, or 
        ///     throws an exception if this object doesn't support creating them.
        /// </summary>
        public virtual AnimableValue CreateAnimableValue( string valueName )
        {
            throw new Exception( "This object has no AnimableValue attributes" );
        }

        #endregion IAnimable methods

        #region Internal engine methods


        /// <summary>
        ///		Internal method called to notify the object that it has been attached to a node.
        /// </summary>
        /// <param name="node">Scene node to notify.</param>
        internal virtual void NotifyAttached( Node node )
        {
            this.NotifyAttached( node, false );
        }

        /// <summary>
        ///		Internal method called to notify the object that it has been attached to a node.
        /// </summary>
        /// <param name="node">Scene node to notify.</param>
        internal virtual void NotifyAttached( Node node, bool isTagPoint )
        {
            this.parentNode = node;
            this.parentIsTagPoint = isTagPoint;
        }

        /// <summary>
        ///		Internal method to notify the object of the camera to be used for the next rendering operation.
        /// </summary>
        /// <remarks>
        ///		Certain objects may want to do specific processing based on the camera position. This method notifies
        ///		them incase they wish to do this.
        /// </remarks>
        /// <param name="camera">Reference to the Camera being used for the current rendering operation.</param>
        public abstract void NotifyCurrentCamera( Camera camera );

        /// <summary>
        ///		An abstract method that causes the specified RenderQueue to update itself.  
        /// </summary>
        /// <remarks>This is an internal method used by the engine assembly only.</remarks>
        /// <param name="queue">The render queue that this object should be updated in.</param>
        public abstract void UpdateRenderQueue( RenderQueue queue );

        #endregion Internal engine methods

        #region Factory methods and propertys

        /// <summary>
        ///     Notify the object of it's creator (internal use only).
        /// </summary>

        public virtual MovableObjectFactory Creator
        {
            get
            {
                return this._creator;
            }
            protected internal set
            {
                this._creator = value;
            }
        }

        /// <summary>
        ///     Notify the object of it's manager (internal use only).
        /// </summary>

        public virtual SceneManager Manager
        {
            get
            {
                return this._manager;
            }
            protected internal set
            {
                this._manager = value;
            }
        }

        /// <summary>
        ///     Gets the MovableType for this MovableObject.
        /// </summary>
        public virtual string MovableType
        {
            get{ return movableType;}
            set { movableType = value; }
        }

        #endregion Factory methods and propertys
    }

    #region MovableObjectFactory

    public abstract class MovableObjectFactory : AbstractFactory<MovableObject>
    {
        public const string TypeName = "MovableObject";
        private string _type;
        private ulong _typeFlag;

        protected MovableObjectFactory()
        {
            this._typeFlag = 0xFFFFFFFF;
            this._type = MovableObjectFactory.TypeName;
        }

        /// <summary>
        ///     Gets/Sets the type flag for this factory.
        /// </summary>
        /// <remarks>
        ///     A type flag is like a query flag, except that it applies to all instances
        ///	    of a certain type of object.
        ///	    This should normally only be called by Root in response to
        ///	    a 'true' result from RequestTypeFlags. However, you can actually use
        ///	    it yourself if you're careful; for example to assign the same mask
        ///	    to a number of different types of object, should you always wish them
        ///	    to be treated the same in queries.
        /// </remarks>
        public virtual ulong TypeFlag
        {
            get
            {
                return this._typeFlag;
            }
            set
            {
                this._typeFlag = value;
            }
        }


        /// <summary>
        ///     Does this factory require the allocation of a 'type flag', used to 
        ///	    selectively include / exclude this type from scene queries?
        /// </summary>
        /// <remarks>
        ///     The default implementation here is to return 'false', ie not to 
        ///     request a unique type mask from Root. For objects that
        ///     never need to be excluded in SceneQuery results, that's fine, since
        ///     the default implementation of MovableObject::getTypeFlags is to return
        ///     all ones, hence matching any query type mask. However, if you want the
        ///     objects created by this factory to be filterable by queries using a 
        ///     broad type, you have to give them a (preferably unique) type mask - 
        ///     and given that you don't know what other MovableObject types are 
        ///     registered, Root will allocate you one. 
        /// </remarks>
        /// 
        public virtual bool RequestTypeFlags
        {
            get
            {
                return false;
            }
        }

        #region AbstractFactory<MovableObject> Members

        public MovableObject CreateInstance( string name )
        {
            return this.CreateInstance( name, null );
        }

        /// <summary>
        ///     Destroy an instance of the object
        /// </summary>
        /// <param name="obj">
        ///     The MovableObject to destroy.
        /// </param>
        public abstract void DestroyInstance( MovableObject obj );

        public string Type
        {
            get
            {
                return this._type;
            }
            protected set
            {
                this._type = value;
            }
        }

        #endregion

        protected abstract MovableObject _createInstance( string name, NamedParameterList param );

        /// <summary>
        ///     Create a new instance of the object.
        /// </summary>
        /// <param name="name">
        ///     The name of the new object
        /// </param>
        /// <param name="manager">
        ///     The SceneManager instance that will be holding the instance once created.
        /// </param>
        /// <param name="param">
        ///     Name/value pair list of additional parameters required to construct the object 
        ///     (defined per subtype).
        /// </param>
        /// <returns>A new MovableObject</returns>
        public MovableObject CreateInstance( string name, SceneManager manager, NamedParameterList param )
        {
            MovableObject m = this._createInstance( name, param );
            m.Creator = this;
            m.Manager = manager;
            m.MovableType = this.Type;
            return m;
        }

        /// <summary>
        ///     Create a new instance of the object.
        /// </summary>
        /// <param name="name">
        ///     The name of the new object
        /// </param>
        /// <param name="manager">
        ///     The SceneManager instance that will be holding the instance once created.
        /// </param>
        /// <returns>A new MovableObject</returns>
        public MovableObject CreateInstance( string name, SceneManager manager )
        {
            return this.CreateInstance( name, manager, null );
        }

    }

    #endregion MovableObjectFactory
}