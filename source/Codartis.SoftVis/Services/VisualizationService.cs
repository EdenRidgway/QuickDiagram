﻿using System;
using System.Collections.Generic;
using System.Linq;
using Codartis.SoftVis.Diagramming.Definition;
using Codartis.SoftVis.Geometry;
using Codartis.SoftVis.Modeling.Definition;
using Codartis.SoftVis.UI;
using JetBrains.Annotations;

namespace Codartis.SoftVis.Services
{
    /// <summary>
    /// Creates, aggregates and orchestrates model, diagram and UI services.
    /// Manages 1 model and any number of diagrams based on that model.
    /// Diagrams can have plugins that react to model and/or diagram events to perform useful tasks, eg. arrange the diagram.
    /// </summary>
    public sealed class VisualizationService : IVisualizationService
    {
        [NotNull] private readonly IModelService _modelService;
        [NotNull] private readonly Func<IModel, IDiagramService> _diagramServiceFactory;
        [NotNull] private readonly Func<IDiagramEventSource, IDiagramShapeUiFactory> _diagramShapeUiFactoryFactory;
        [NotNull] private readonly Func<IDiagramEventSource, IDiagramShapeUiFactory, IDiagramViewportUi> _diagramViewportUiFactory;
        [NotNull] private readonly Func<IDiagramEventSource, IDiagramViewportUi, IDiagramUi> _diagramUiFactory;
        [NotNull] private readonly Func<IDiagramUi, IDiagramUiService> _diagramUiServiceFactory;
        [NotNull] private readonly IEnumerable<Func<IDiagramService, IDiagramPlugin>> _diagramPluginFactories;

        [NotNull] private readonly Dictionary<DiagramId, IDiagramService> _diagramServices;
        [NotNull] private readonly Dictionary<DiagramId, IDiagramUiService> _diagramUiServices;
        [NotNull] private readonly Dictionary<DiagramId, List<IDiagramPlugin>> _diagramPlugins;

        public VisualizationService(
            [NotNull] IModelService modelService,
            [NotNull] Func<IModel, IDiagramService> diagramServiceFactory,
            [NotNull] Func<IDiagramEventSource, IDiagramShapeUiFactory> diagramShapeUiFactoryFactory,
            [NotNull] Func<IDiagramEventSource, IDiagramShapeUiFactory, IDiagramViewportUi> diagramViewportUiFactory,
            [NotNull] Func<IDiagramEventSource, IDiagramViewportUi, IDiagramUi> diagramUiFactory,
            [NotNull] Func<IDiagramUi, IDiagramUiService> diagramUiServiceFactory,
            [NotNull] IEnumerable<Func<IDiagramService, IDiagramPlugin>> diagramPluginFactories)
        {
            _modelService = modelService;
            _diagramServiceFactory = diagramServiceFactory;
            _diagramShapeUiFactoryFactory = diagramShapeUiFactoryFactory;
            _diagramViewportUiFactory = diagramViewportUiFactory;
            _diagramUiFactory = diagramUiFactory;
            _diagramUiServiceFactory = diagramUiServiceFactory;
            _diagramPluginFactories = diagramPluginFactories;

            _diagramServices = new Dictionary<DiagramId, IDiagramService>();
            _diagramUiServices = new Dictionary<DiagramId, IDiagramUiService>();
            _diagramPlugins = new Dictionary<DiagramId, List<IDiagramPlugin>>();
        }

        public DiagramId CreateDiagram()
        {
            var diagramId = DiagramId.Create();

            var diagramService = _diagramServiceFactory(_modelService.LatestModel);
            _diagramServices.Add(diagramId, diagramService);

            var diagramUiService = CreateDiagramUiService(diagramId, diagramService);
            _diagramUiServices.Add(diagramId, diagramUiService);

            var plugins = _diagramPluginFactories.Select(i => i(diagramService)).ToList();
            _diagramPlugins.Add(diagramId, plugins);

            return diagramId;
        }

        public IModelService GetModelService() => _modelService;
        public IDiagramService GetDiagramService(DiagramId diagramId) => _diagramServices[diagramId];
        public IDiagramUiService GetDiagramUiService(DiagramId diagramId) => _diagramUiServices[diagramId];

        public void DeleteDiagram(DiagramId diagramId)
        {
            _diagramServices.Remove(diagramId);

            var diagramUi = _diagramUiServices[diagramId];
            diagramUi.DiagramNodeHeaderSizeChanged -= PropagateDiagramNodeHeaderSizeChanged(diagramId);
            diagramUi.RemoveDiagramNodeRequested -= PropagateRemoveDiagramNodeRequested(diagramId);
            _diagramUiServices.Remove(diagramId);

            _diagramPlugins[diagramId].ForEach(i => i.Dispose());
            _diagramPlugins.Remove(diagramId);
        }

        [NotNull]
        private IDiagramUiService CreateDiagramUiService(DiagramId diagramId, [NotNull] IDiagramService diagramService)
        {
            var diagramShapeUiFactory = _diagramShapeUiFactoryFactory.Invoke(diagramService);
            var diagramViewportUi = _diagramViewportUiFactory .Invoke(diagramService, diagramShapeUiFactory);
            var diagramUi = _diagramUiFactory.Invoke(diagramService, diagramViewportUi);

            var diagramUiService = _diagramUiServiceFactory(diagramUi);
            diagramUiService.DiagramNodeHeaderSizeChanged += PropagateDiagramNodeHeaderSizeChanged(diagramId);
            diagramUiService.RemoveDiagramNodeRequested += PropagateRemoveDiagramNodeRequested(diagramId);
            return diagramUiService;
        }

        [NotNull]
        private Action<IDiagramNode> PropagateRemoveDiagramNodeRequested(DiagramId diagramId)
        {
            return diagramNode => OnRemoveDiagramNodeRequested(diagramId, diagramNode);
        }

        [NotNull]
        private Action<IDiagramNode, Size2D> PropagateDiagramNodeHeaderSizeChanged(DiagramId diagramId)
        {
            return (diagramNode, size) => OnDiagramNodeHeaderSizeChanged(diagramId, diagramNode, size);
        }

        private void OnDiagramNodeHeaderSizeChanged(DiagramId diagramId, [NotNull] IDiagramNode diagramNode, Size2D newSize)
        {
            GetDiagramService(diagramId).UpdateNodeHeaderSize(diagramNode.Id, newSize);
        }

        private void OnRemoveDiagramNodeRequested(DiagramId diagramId, [NotNull] IDiagramNode diagramNode)
        {
            GetDiagramService(diagramId).RemoveNode(diagramNode.Id);
        }
    }
}