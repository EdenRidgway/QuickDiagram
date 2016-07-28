﻿using System.Diagnostics;
using QuickGraph;

namespace Codartis.SoftVis.Modeling.Implementation
{
    /// <summary>
    /// An implementation of the IModelRelationship interface with a QuickGraph edge.
    /// </summary>
    [DebuggerDisplay("{Source.Name}--{Type}/{Stereotype}-->{Target.Name}")]
    public class ModelRelationship : IModelRelationship, IEdge<IModelEntity>
    {
        private readonly ModelEntity _source;
        private readonly ModelEntity _target;

        public IModelEntity Source => _source;
        public IModelEntity Target => _target;

        public ModelRelationshipType Type { get; }
        public ModelRelationshipStereotype Stereotype { get; }

        public ModelRelationship(ModelEntity source, ModelEntity target, ModelRelationshipType type, ModelRelationshipStereotype stereotype)
        {
            _source = source;
            _target = target;
            Type = type;
            Stereotype = stereotype;
        }

        public bool IsOfType(ModelRelationshipTypeSpecification typeSpecification)
        {
            return Type == typeSpecification.Type && Stereotype == typeSpecification.Stereotype;
        }
    }
}
