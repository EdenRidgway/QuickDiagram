﻿using System;
using System.Collections.Generic;
using Codartis.SoftVis.Diagramming.Layout.Incremental.Absolute;
using Codartis.SoftVis.Diagramming.Layout.Incremental.Relative;
using Codartis.SoftVis.Diagramming.Layout.Incremental.Relative.Logic;
using Codartis.SoftVis.Geometry;
using Codartis.SoftVis.Util;

namespace Codartis.SoftVis.Diagramming.Layout.Incremental
{
    /// <summary>
    /// A stateful layout calculator that gets diagram shape actions and calculates layout actions.
    /// </summary>
    /// <remarks>
    /// Layout rules:
    /// <para>Adding a new node adds it to the first layer in node name order.</para>
    /// <para>Adding a new inheritance connection moves the source node under the target node.
    /// The source node brings all its children with it.</para>
    /// <para>If the source node has siblings then it is placed among them based on node name order.</para>
    /// <para>If the source node has no siblings then it is placed between the children of the parent's preceding and following nodes.
    /// It ensures that there won't be any inheritance edge crossings.</para>
    /// </remarks>
    internal sealed class IncrementalLayoutEngine : IIncrementalLayoutEngine
    {
        private readonly Map<IDiagramNode, DiagramNodeLayoutVertex> _diagramNodeToLayoutVertexMap;
        private readonly Map<IDiagramConnector, LayoutPath> _diagramConnectorToLayoutPathMap;
        private readonly Map<LayoutPath, Route> _layoutPathToPreviousRouteMap;
        private LayoutVertexToPointMap _previousVertexCenters;
        private readonly RelativeLayoutCalculator _relativeLayoutCalculator;

        private const double HorizontalGap = DiagramDefaults.HorizontalGap;
        private const double VerticalGap = DiagramDefaults.VerticalGap;

        public IncrementalLayoutEngine()
        {
            _diagramNodeToLayoutVertexMap = new Map<IDiagramNode, DiagramNodeLayoutVertex>();
            _diagramConnectorToLayoutPathMap = new Map<IDiagramConnector, LayoutPath>();
            _layoutPathToPreviousRouteMap = new Map<LayoutPath, Route>();
            _previousVertexCenters = new LayoutVertexToPointMap();
            _relativeLayoutCalculator = new RelativeLayoutCalculator();
        }

        private IReadOnlyRelativeLayout RelativeLayout => _relativeLayoutCalculator.RelativeLayout;

        public void Clear()
        {
            _relativeLayoutCalculator.OnDiagramCleared();

            _layoutPathToPreviousRouteMap.Clear();
            _diagramConnectorToLayoutPathMap.Clear();
            _diagramNodeToLayoutVertexMap.Clear();
            _previousVertexCenters.Clear();
        }

        public IEnumerable<ILayoutAction> CalculateLayoutActions(IEnumerable<DiagramAction> diagramActions)
        {
            foreach (var diagramShapeAction in diagramActions)
                ApplyDiagramActionToRelativeLayout(diagramShapeAction);

            return CalculateAbsoluteLayout();
        }

        private void ApplyDiagramActionToRelativeLayout(DiagramAction diagramAction)
        {
            var diagramNodeAction = diagramAction as DiagramNodeAction;
            if (diagramNodeAction != null)
            {
                switch (diagramNodeAction.ActionType)
                {
                    case ShapeActionType.Add:
                        OnDiagramNodeAdded(diagramNodeAction.DiagramNode);
                        break;
                    case ShapeActionType.Remove:
                        OnDiagramNodeRemoved(diagramNodeAction.DiagramNode);
                        break;
                    case ShapeActionType.Resize:
                        OnDiagramNodeResized(diagramNodeAction.DiagramNode);
                        break;
                }
            }

            var diagramConnectorAction = diagramAction as DiagramConnectorAction;
            if (diagramConnectorAction != null)
            {
                switch (diagramConnectorAction.ActionType)
                {
                    case ShapeActionType.Add:
                        OnDiagramConnectorAdded(diagramConnectorAction.DiagramConnector);
                        break;
                    case ShapeActionType.Remove:
                        OnDiagramConnectorRemoved(diagramConnectorAction.DiagramConnector);
                        break;
                }
            }
        }

        private void OnDiagramNodeAdded(IDiagramNode diagramNode)
        {
            if (_diagramNodeToLayoutVertexMap.Contains(diagramNode))
                throw new InvalidOperationException($"Diagram node {diagramNode} already added.");

            var diagramNodeLayoutVertex = new DiagramNodeLayoutVertex(diagramNode);
            _diagramNodeToLayoutVertexMap.Set(diagramNode, diagramNodeLayoutVertex);

            _relativeLayoutCalculator.OnDiagramNodeAdded(diagramNodeLayoutVertex);
        }

        private void OnDiagramNodeRemoved(IDiagramNode diagramNode)
        {
            if (!_diagramNodeToLayoutVertexMap.Contains(diagramNode))
                throw new InvalidOperationException($"Diagram node {diagramNode} not found.");

            var diagramNodeLayoutVertex = _diagramNodeToLayoutVertexMap.Get(diagramNode);
            _diagramNodeToLayoutVertexMap.Remove(diagramNode);

            _relativeLayoutCalculator.OnDiagramNodeRemoved(diagramNodeLayoutVertex);
        }

        private void OnDiagramNodeResized(IDiagramNode diagramNode)
        {
            if (!_diagramNodeToLayoutVertexMap.Contains(diagramNode))
                throw new InvalidOperationException($"Diagram node {diagramNode} not found.");

            var diagramNodeLayoutVertex = _diagramNodeToLayoutVertexMap.Get(diagramNode);
            diagramNodeLayoutVertex.Resize(diagramNode.Size);
        }

        private void OnDiagramConnectorAdded(IDiagramConnector diagramConnector)
        {
            if (_diagramConnectorToLayoutPathMap.Contains(diagramConnector))
                throw new InvalidOperationException($"Diagram connector {diagramConnector} already added.");

            var layoutPath = CreateLayoutPath(diagramConnector);
            _diagramConnectorToLayoutPathMap.Set(diagramConnector, layoutPath);

            _relativeLayoutCalculator.OnDiagramConnectorAdded(layoutPath);
        }

        private void OnDiagramConnectorRemoved(IDiagramConnector diagramConnector)
        {
            if (!_diagramConnectorToLayoutPathMap.Contains(diagramConnector))
                throw new InvalidOperationException($"Diagram connector {diagramConnector} not found.");

            var layoutPath = _diagramConnectorToLayoutPathMap.Get(diagramConnector);
            _diagramConnectorToLayoutPathMap.Remove(diagramConnector);

            _relativeLayoutCalculator.OnDiagramConnectorRemoved(layoutPath);
        }

        private LayoutPath CreateLayoutPath(IDiagramConnector diagramConnector)
        {
            var sourceVertex = _diagramNodeToLayoutVertexMap.Get(diagramConnector.Source);
            var targetVertex = _diagramNodeToLayoutVertexMap.Get(diagramConnector.Target);
            return new LayoutPath(sourceVertex, targetVertex, diagramConnector);
        }

        private IEnumerable<ILayoutAction> CalculateAbsoluteLayout()
        {
            var newVertexCenters = AbsolutePositionCalculator.GetVertexCenters(
                _relativeLayoutCalculator.RelativeLayout, HorizontalGap, VerticalGap);

            var layoutActions = CreateLayoutActions(newVertexCenters);

            SaveCurrentPositions(newVertexCenters);
            return layoutActions;
        }

        private void SaveCurrentPositions(LayoutVertexToPointMap newVertexCenters)
        {
            _previousVertexCenters = newVertexCenters;
            SaveCurrentLayoutPaths(newVertexCenters);
        }

        private void SaveCurrentLayoutPaths(LayoutVertexToPointMap newVertexCenters)
        {
            _layoutPathToPreviousRouteMap.Clear();

            foreach (var layoutPath in RelativeLayout.LayeredLayoutGraph.Edges)
            {
                var currentRoute = layoutPath.GetRoute(newVertexCenters);
                _layoutPathToPreviousRouteMap.Set(layoutPath, currentRoute);
            }
        }

        private List<ILayoutAction> CreateLayoutActions(LayoutVertexToPointMap newVertexCenters)
        {
            var layoutActions = new List<ILayoutAction>();
            layoutActions.AddRange(CreateLayoutActionsForMovedDiagramNodes(newVertexCenters));
            layoutActions.AddRange(CreateLayoutActionsForRerouteAllPaths(newVertexCenters));
            return layoutActions;
        }

        private IEnumerable<ILayoutAction> CreateLayoutActionsForMovedDiagramNodes(LayoutVertexToPointMap newVertexCenters)
        {
            foreach (var diagramNodeLayoutVertex in RelativeLayout.LayeredLayoutGraph.Vertices)
            {
                var oldCenter = GetVertexCenterOrNull(_previousVertexCenters, diagramNodeLayoutVertex);
                var newCenter = GetVertexCenterOrNull(newVertexCenters, diagramNodeLayoutVertex);
                if (oldCenter != newCenter && newCenter != null)
                    yield return new MoveDiagramNodeLayoutAction(diagramNodeLayoutVertex, Point2D.Undefined, newCenter.Value);
            }
        }

        private IEnumerable<ILayoutAction> CreateLayoutActionsForRerouteAllPaths(LayoutVertexToPointMap newVertexCenters)
        {
            foreach (var layoutPath in RelativeLayout.LayeredLayoutGraph.Edges)
            {
                var previousRoute = _layoutPathToPreviousRouteMap.Get(layoutPath);
                var newRoute = layoutPath.GetRoute(newVertexCenters);

                if (previousRoute != newRoute && newRoute != null)
                    yield return new ReroutePathLayoutAction(layoutPath, previousRoute, newRoute);
            }
        }

        private static Point2D? GetVertexCenterOrNull(LayoutVertexToPointMap vertexCenters, LayoutVertexBase vertex)
        {
            return vertexCenters.Contains(vertex)
                ? vertexCenters.Get(vertex)
                : (Point2D?)null;
        }
    }
}
