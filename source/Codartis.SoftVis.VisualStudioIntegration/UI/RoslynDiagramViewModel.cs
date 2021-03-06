﻿using Codartis.SoftVis.Diagramming.Definition;
using Codartis.SoftVis.Modeling.Definition;
using Codartis.SoftVis.UI;
using Codartis.SoftVis.UI.Wpf.ViewModel;
using JetBrains.Annotations;

namespace Codartis.SoftVis.VisualStudioIntegration.UI
{
    /// <summary>
    /// The view model of a QuickDiagram tool window.
    /// </summary>
    internal sealed class RoslynDiagramViewModel : DiagramViewModel
    {
        [NotNull] private readonly RoslynDiagramViewportViewModel _roslynDiagramViewportViewModel;

        public RoslynDiagramViewModel(
            [NotNull] IModelEventSource modelEventSource,
            [NotNull] IDiagramEventSource diagramEventSource,
            [NotNull] IDiagramViewportUi diagramViewportUi)
            : base(modelEventSource, diagramEventSource, diagramViewportUi)
        {
            _roslynDiagramViewportViewModel = (RoslynDiagramViewportViewModel)diagramViewportUi;
        }

        public void ExpandAllNodes() => _roslynDiagramViewportViewModel.ExpandAllNodes();
        public void CollapseAllNodes() => _roslynDiagramViewportViewModel.CollapseAllNodes();
    }
}