﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Codartis.SoftVis.Diagramming;
using Codartis.SoftVis.Diagramming.Implementation;
using Codartis.SoftVis.Modeling;
using Codartis.SoftVis.Util;
using Codartis.SoftVis.VisualStudioIntegration.Modeling;

namespace Codartis.SoftVis.VisualStudioIntegration.Diagramming
{
    /// <summary>
    /// Specializes the diagram class for the VS integrated usage.
    /// </summary>
    internal class RoslynBasedDiagram : AutoArrangingDiagram, IDiagramServices
    {
        private readonly IModelServices _modelServices;

        public RoslynBasedDiagram(IModelServices modelServices)
            : base(modelServices.Model)
        {
            _modelServices = modelServices;
        }

        public override IEnumerable<EntityRelationType> GetEntityRelationTypes()
        {
            foreach (var entityRelationType in base.GetEntityRelationTypes())
                yield return entityRelationType;

            yield return RoslynEntityRelationTypes.ImplementedInterface;
            yield return RoslynEntityRelationTypes.ImplementerType;
        }

        public override ConnectorType GetConnectorType(ModelRelationshipType type)
        {
            return type.Stereotype == ModelRelationshipStereotypes.Implementation
                ? RoslynBasedConnectorTypes.Implementation
                : ConnectorTypes.Generalization;
        }

        public IDiagramNode ShowEntity(IModelEntity modelEntity)
        {
            return ShowItem(modelEntity) as IDiagramNode;
        }

        public List<IDiagramNode> ShowEntities(IEnumerable<IModelEntity> modelEntities, CancellationToken cancellationToken, IIncrementalProgress progress)
        {
            return ShowItems(modelEntities, cancellationToken, progress).OfType<IDiagramNode>().ToList();
        }

        public List<IDiagramNode> ShowEntityWithHierarchy(IModelEntity modelEntity, CancellationToken cancellationToken, IIncrementalProgress progress)
        {
            var baseTypes = Model.GetRelatedEntities(modelEntity, EntityRelationTypes.BaseType, recursive: true);
            var subtypes = Model.GetRelatedEntities(modelEntity, EntityRelationTypes.Subtype, recursive: true);
            var entities = new[] { modelEntity }.Union(baseTypes).Union(subtypes);

             return ShowItems(entities, cancellationToken, progress).OfType<IDiagramNode>().ToList();
        }

        public void UpdateFromSource(CancellationToken cancellationToken, IIncrementalProgress progress)
        {
            foreach (var diagramNode in Nodes.ToArray())
            {
                _modelServices.ExtendModelWithRelatedEntities(diagramNode.ModelEntity, cancellationToken: cancellationToken);
                progress?.Report(1);
            }
        }
    }
}
