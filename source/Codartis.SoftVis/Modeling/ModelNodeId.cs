﻿using System;

namespace Codartis.SoftVis.Modeling
{
    /// <summary>
    /// Identifies a model node through its lifetime.
    /// </summary>
    [Immutable]
    public struct ModelNodeId : IEquatable<ModelNodeId>, IComparable<ModelNodeId>
    {
        private readonly Guid _id;

        public ModelNodeId(Guid id)
        {
            _id = id;
        }

        public static ModelNodeId Create() => new ModelNodeId(Guid.NewGuid());

        public override string ToString() => $"{GetType().Name}({_id})";

        public bool Equals(ModelNodeId other)
        {
            return _id.Equals(other._id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ModelNodeId && Equals((ModelNodeId)obj);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode();
        }

        public static bool operator ==(ModelNodeId left, ModelNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ModelNodeId left, ModelNodeId right)
        {
            return !left.Equals(right);
        }

        public int CompareTo(ModelNodeId other)
        {
            return _id.CompareTo(other._id);
        }
    }
}
