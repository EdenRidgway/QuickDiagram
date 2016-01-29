﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Codartis.SoftVis.Diagramming.Graph.Layout;
using Codartis.SoftVis.Diagramming.Graph.Layout.Incremental.Logic;
using Codartis.SoftVis.Geometry;
using Codartis.SoftVis.Graphs;
using Codartis.SoftVis.Graphs.Layout;
using Codartis.SoftVis.Modeling;

namespace Codartis.SoftVis.Diagramming.Graph
{
    /// <summary>
    /// A diagram is a partial, graphical representation of a model. 
    /// A diagram shows a subset of the model and there can be many diagrams depicting different areas/aspects of the same model.
    /// A diagram consists of shapes that represent model elements.
    /// The shapes form a directed graph: some shapes are nodes in the graph and others are connectors between nodes.
    /// A diagram has a layout engine that calculates how to arrange nodes and connectors.
    /// The layout (relative positions and size) also conveys meaning.
    /// </summary>
    [DebuggerDisplay("VertexCount={_graph.VertexCount}, EdgeCount={_graph.EdgeCount}")]
    public abstract class Diagram : IArrangeableDiagram
    {
        public IConnectorTypeResolver ConnectorTypeResolver { get; }

        private readonly DiagramGraph _graph;
        private readonly IIncrementalLayoutEngine _incrementalLayoutEngine;
        private readonly LayoutActionExecutorVisitor _layoutActionExecutor;
        private readonly List<DiagramShapeAction> _diagramShapeActionBuffer;

        public event EventHandler<DiagramShape> ShapeAdded;
        public event EventHandler<DiagramShape> ShapeMoved;
        public event EventHandler<DiagramShape> ShapeRemoved;
        public event EventHandler<DiagramShape> ShapeSelected;
        public event EventHandler<DiagramShape> ShapeActivated;
        public event EventHandler Cleared;

        protected Diagram(IConnectorTypeResolver connectorTypeResolver)
        {
            if (connectorTypeResolver == null)
                throw new ArgumentNullException(nameof(connectorTypeResolver));

            ConnectorTypeResolver = connectorTypeResolver;

            _graph = new DiagramGraph();
            _incrementalLayoutEngine = new IncrementalLayoutEngine();
            _layoutActionExecutor = new LayoutActionExecutorVisitor(this);
            _diagramShapeActionBuffer = new List<DiagramShapeAction>();
        }

        public IEnumerable<DiagramNode> Nodes => _graph.Vertices;
        public IEnumerable<DiagramConnector> Connectors => _graph.Edges;
        public IEnumerable<DiagramShape> Shapes => Nodes.OfType<DiagramShape>().Union(Connectors);
        public Rect2D ContentRect => Shapes.Select(i => i.Rect).Union();

        /// <summary>
        /// Clear the diagram (that is, hide all nodes and connectors).
        /// </summary>
        public virtual void Clear()
        {
            _graph.Clear();
            _incrementalLayoutEngine.Clear();
            _diagramShapeActionBuffer.Clear();
            OnCleared();
        }

        public void ShowItem(IModelItem modelItem) => ShowItems(new[] { modelItem });
        public void HideItem(IModelItem modelItem) => HideItems(new[] { modelItem });

        public void ShowItems(IEnumerable<IModelItem> modelItems)
        {
            foreach (var modelItem in modelItems)
            {
                if (modelItem is IModelEntity)
                    ShowEntityCore((IModelEntity)modelItem);

                if (modelItem is IModelRelationship)
                    ShowRelationshipCore((IModelRelationship)modelItem);
            }

            Layout();
        }

        public void HideItems(IEnumerable<IModelItem> modelItems)
        {
            foreach (var modelItem in modelItems)
            {
                if (modelItem is IModelEntity)
                    HideEntityCore((IModelEntity)modelItem);

                if (modelItem is IModelRelationship)
                    HideRelationshipCore((IModelRelationship)modelItem);
            }

            Layout();
        }

        public void SelectShape(DiagramShape diagramShape)
        {
            OnShapeSelected(diagramShape);
        }

        public void ActivateShape(DiagramShape diagramShape)
        {
            OnShapeActivated(diagramShape);
        }

        public void RemoveShape(DiagramShape diagramShape)
        {
            HideItems(new[] { diagramShape.ModelItem });
        }

        public IEnumerable<DiagramNode> GetRelatedNodes(DiagramNode diagramNode, RelationshipSpecification specification)
        {
            var typeSpecification = specification.TypeSpecification;

            return specification.Direction == ModelRelationshipDirection.Incoming
                ? _graph.InEdges(diagramNode).Where(i => i.IsOfType(typeSpecification)).Select(i => i.Source)
                : _graph.OutEdges(diagramNode).Where(i => i.IsOfType(typeSpecification)).Select(i => i.Target);
        }

        /// <summary>
        /// Show a node on the diagram that represents the given model element.
        /// </summary>
        /// <param name="modelEntity">A type or package model element.</param>
        protected virtual void ShowEntityCore(IModelEntity modelEntity)
        {
            if (NodeExists(modelEntity))
                return;

            var diagramNode = CreateDiagramNode(modelEntity);
            AddDiagramNode(diagramNode);
            ShowRelationshipsIfBothEndsAreVisible(modelEntity);
        }

        /// <summary>
        /// Show a connector on the diagram that represents the given model element.
        /// </summary>
        /// <param name="modelRelationship">A relationship model item.</param>
        protected virtual void ShowRelationshipCore(IModelRelationship modelRelationship)
        {
            if (ConnectorExists(modelRelationship) ||
                !NodeExists(modelRelationship.Source) ||
                !NodeExists(modelRelationship.Target))
                return;

            var diagramConnector = CreateDiagramConnector(modelRelationship);
            AddDiagramConnector(diagramConnector);
            HideRedundantDirectEdges();
        }

        /// <summary>
        /// Hide a node from the diagram that represents the given model element.
        /// </summary>
        /// <param name="modelEntity">A type or package model element.</param>
        protected virtual void HideEntityCore(IModelEntity modelEntity)
        {
            if (!NodeExists(modelEntity))
                return;

            var diagramNode = FindNode(modelEntity);

            foreach (var edge in _graph.GetAllEdges(diagramNode).ToArray())
                HideRelationshipCore(edge.ModelRelationship);

            RemoveDiagramNode(diagramNode);
        }

        /// <summary>
        /// Hides a connector from the diagram that represents the given model element.
        /// </summary>
        /// <param name="modelRelationship">A modelRelationship model item.</param>
        protected virtual void HideRelationshipCore(IModelRelationship modelRelationship)
        {
            if (!ConnectorExists(modelRelationship))
                return;

            var diagramConnector = FindConnector(modelRelationship);
            RemoveDiagramConnector(diagramConnector);
        }

        private void Layout() => Layout(LayoutType.Incremental);

        /// <summary>
        /// Calculates the layout of the diagram and applies the new shape positions and edge routes.
        /// </summary>
        private void Layout(LayoutType layoutType, ILayoutParameters layoutParameters = null)
        {
            switch (layoutType)
            {
                case LayoutType.Incremental:
                    ApplyIncrementalLayoutChanges();
                    break;
                default:
                    throw new ArgumentException($"Unexpected layout type: {layoutType}");
            }
        }

        public void MoveNode(DiagramNode diagramNode, Point2D newCenter)
        {
            var isFirstPosition = diagramNode.Center == Point2D.Empty;
            diagramNode.Center = newCenter;

            if (isFirstPosition)
                OnShapeAdded(diagramNode);
            else
                OnShapeMoved(diagramNode);
        }

        public void RerouteConnector(DiagramConnector diagramConnector, Route newRoute)
        {
            var isFirstRoute = diagramConnector.RoutePoints == null;
            diagramConnector.RoutePoints = newRoute;

            if (isFirstRoute)
                OnShapeAdded(diagramConnector);
            else
                OnShapeMoved(diagramConnector);
        }

        protected abstract DiagramNode CreateDiagramNode(IModelEntity modelEntity);
        protected abstract DiagramConnector CreateDiagramConnector(IModelRelationship relationship);

        private void HideRedundantDirectEdges()
        {
            // TODO: should only hide same-type connectors!!!

            foreach (var connector in Connectors.ToList())
            {
                var paths = _graph.GetShortestPaths(connector.Source, connector.Target, 2).ToList();
                if (paths.Count > 1)
                {
                    var pathToHide = paths.FirstOrDefault(i => i.Length == 1);
                    if (pathToHide != null)
                        HideRelationshipCore(pathToHide[0].ModelRelationship);
                }
            }
        }

        private void ShowRelationshipsIfBothEndsAreVisible(IModelEntity modelEntity)
        {
            foreach (var modelRelationship in modelEntity.AllRelationships)
            {
                if (NodeExists(modelRelationship.Source) &&
                    NodeExists(modelRelationship.Target))
                {
                    ShowRelationshipCore(modelRelationship);
                }
            }
        }

        private void ApplyIncrementalLayoutChanges()
        {
            var layoutActions = _incrementalLayoutEngine.GetLayoutActions(_diagramShapeActionBuffer);
            var netLayoutActions = CombineLayoutAction(layoutActions);

            foreach (var layoutAction in netLayoutActions)
                layoutAction.AcceptVisitor(_layoutActionExecutor);

            _diagramShapeActionBuffer.Clear();
        }

        private static IEnumerable<ILayoutAction> CombineLayoutAction(IEnumerable<ILayoutAction> layoutActions)
        {
            return layoutActions.GroupBy(i => i.DiagramShape).Select(j => j.Last());
        }

        private void AddDiagramNode(DiagramNode diagramNode)
        {
            _graph.AddVertex(diagramNode);
            _diagramShapeActionBuffer.Add(new DiagramNodeAction(diagramNode, ShapeActionType.Add));
        }

        private void RemoveDiagramNode(DiagramNode diagramNode)
        {
            _graph.RemoveVertex(diagramNode);
            _diagramShapeActionBuffer.Add(new DiagramNodeAction(diagramNode, ShapeActionType.Remove));
            OnShapeRemoved(diagramNode);
        }

        private void AddDiagramConnector(DiagramConnector diagramConnector)
        {
            _graph.AddEdge(diagramConnector);
            _diagramShapeActionBuffer.Add(new DiagramConnectorAction(diagramConnector, ShapeActionType.Add));
        }

        private void RemoveDiagramConnector(DiagramConnector diagramConnector)
        {
            _graph.RemoveEdge(diagramConnector);
            _diagramShapeActionBuffer.Add(new DiagramConnectorAction(diagramConnector, ShapeActionType.Remove));
            OnShapeRemoved(diagramConnector);
        }

        protected DiagramNode FindNode(IModelEntity modelEntity)
        {
            return Nodes.FirstOrDefault(i => Equals(i.ModelEntity, modelEntity));
        }

        protected bool NodeExists(IModelEntity modelEntity)
        {
            return Nodes.Any(i => Equals(i.ModelEntity, modelEntity));
        }

        private DiagramConnector FindConnector(IModelRelationship modelRelationship)
        {
            return Connectors.FirstOrDefault(i => Equals(i.ModelRelationship, modelRelationship));
        }

        private bool ConnectorExists(IModelRelationship modelRelationship)
        {
            return Connectors.Any(i => Equals(i.ModelRelationship, modelRelationship));
        }

        private void OnShapeAdded(DiagramShape diagramShape)
        {
            ShapeAdded?.Invoke(this, diagramShape);
        }

        private void OnShapeMoved(DiagramShape diagramShape)
        {
            ShapeMoved?.Invoke(this, diagramShape);
        }

        private void OnShapeRemoved(DiagramShape diagramShape)
        {
            ShapeRemoved?.Invoke(this, diagramShape);
        }

        private void OnCleared()
        {
            Cleared?.Invoke(this, EventArgs.Empty);
        }

        private void OnShapeSelected(DiagramShape diagramShape)
        {
            ShapeSelected?.Invoke(this, diagramShape);
        }

        private void OnShapeActivated(DiagramShape diagramShape)
        {
            ShapeActivated?.Invoke(this, diagramShape);
        }
    }
}
